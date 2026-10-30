"""Clorium Stack 0.1: independent, CPU focus stacking reference implementation.

Recovered ModeStack evidence: Laplacian pyramid, grayscale squared-detail
energy, Gaussian energy smoothing, colour coefficient selection, reconstruction.
Our alignment, floating-point arithmetic, tie policy and validation are new.
No ModeVid executable, token or proprietary DLL is used.
"""
from pathlib import Path
import argparse
import json
import math
import threading
import time

import cv2
import numpy as np
from PIL import Image, ImageOps


class StackError(ValueError):
    pass


class Cancelled(StackError):
    pass


def check_cancel(cancel):
    if cancel is not None and cancel.is_set():
        raise Cancelled('사용자가 합성을 중지했습니다.')


def read_image(path):
    with Image.open(path) as source:
        if source.mode not in ('RGB', 'L'):
            raise StackError(f'8-bit RGB/JPEG/PNG/TIFF만 지원합니다: {path} ({source.mode})')
        profile = source.info.get('icc_profile')
        oriented = ImageOps.exif_transpose(source)
        return np.array(oriented.convert('RGB')), profile


def pyramid(image, levels):
    result = []
    current = image.astype(np.float32)
    for _ in range(levels):
        down = cv2.pyrDown(current)
        result.append(current - cv2.pyrUp(down, dstsize=(current.shape[1], current.shape[0])))
        current = down
    result.append(current)
    return result


def reconstruct(layers):
    result = layers[-1]
    for layer in reversed(layers[:-1]):
        result = cv2.pyrUp(result, dstsize=(layer.shape[1], layer.shape[0])) + layer
    return np.rint(np.clip(result, 0, 255)).astype(np.uint8)


def preview_gray(image, max_side=1600):
    h, w = image.shape[:2]
    scale = min(1., max_side / max(h, w))
    size = (max(1, round(w*scale)), max(1, round(h*scale)))
    gray = cv2.cvtColor(image, cv2.COLOR_RGB2GRAY)
    return cv2.resize(gray, size, interpolation=cv2.INTER_AREA), np.array([size[0]/w, size[1]/h])


def align_image(image, reference):
    """ORB+Hamming+RANSAC similarity transform, intentionally not ModeStack exact."""
    if np.array_equal(image, reference):
        return image, np.ones(image.shape[:2], np.uint8), {'identity': True}
    a, sa = preview_gray(image)
    b, sb = preview_gray(reference)
    orb = cv2.ORB_create(nfeatures=6000)
    ka, da = orb.detectAndCompute(a, None)
    kb, db = orb.detectAndCompute(b, None)
    if da is None or db is None:
        raise StackError('정렬 실패: 특징점 부족. 고정 촬영임을 확인한 경우에만 자동 정렬을 끄세요.')
    pairs = cv2.BFMatcher(cv2.NORM_HAMMING).knnMatch(da, db, k=2)
    good = [p[0] for p in pairs if len(p)==2 and p[0].distance < .72*p[1].distance]
    if len(good) < 16:
        raise StackError(f'정렬 실패: 신뢰할 수 있는 대응점 {len(good)}개 (최소 16개).')
    src = np.float32([ka[m.queryIdx].pt for m in good])
    dst = np.float32([kb[m.trainIdx].pt for m in good])
    matrix, inliers = cv2.estimateAffinePartial2D(src, dst, method=cv2.RANSAC,
                                               ransacReprojThreshold=2., maxIters=4000, confidence=.995)
    if matrix is None or inliers is None:
        raise StackError('정렬 변환을 계산할 수 없습니다.')
    mask = inliers.ravel().astype(bool)
    projected = src @ matrix[:,:2].T + matrix[:,2]
    residual = float(np.median(np.linalg.norm(projected[mask]-dst[mask],axis=1)))
    scale = float(np.sqrt(np.linalg.det(matrix[:,:2])))
    if mask.sum()<12 or mask.mean()<.35 or residual>1.5 or not .85<=scale<=1.18:
        raise StackError('정렬 품질이 기준에 미달합니다. 흔들림/피사체 이동/반복 무늬를 확인하세요.')
    # Reject tiny clusters of matches that do not constrain the image-wide transform.
    area = cv2.contourArea(cv2.convexHull(src[mask])) / (a.shape[0]*a.shape[1])
    if area < .025:
        raise StackError('정렬 대응점이 너무 좁은 영역에 모여 있습니다.')
    full = np.diag([1/sb[0],1/sb[1],1]) @ np.vstack([matrix,[0,0,1]]) @ np.diag([sa[0],sa[1],1])
    h,w=reference.shape[:2]
    valid=cv2.warpAffine(np.ones(image.shape[:2],np.uint8),full[:2],(w,h),flags=cv2.INTER_NEAREST)
    if float(valid.mean()) < .85:
        raise StackError('정렬 후 이미지의 겹치는 영역이 85% 미만입니다.')
    warped=cv2.warpAffine(image,full[:2],(w,h),flags=cv2.INTER_LINEAR,borderMode=cv2.BORDER_REFLECT_101)
    return warped, valid, {'inliers':int(mask.sum()),'median_preview_error':residual,'scale':scale,
                           'overlap':float(valid.mean()),'matrix':full[:2].tolist()}


def energy(layer, window):
    gray=cv2.cvtColor(layer,cv2.COLOR_RGB2GRAY)
    return cv2.GaussianBlur(gray*gray,(window,window),0)


def unique_save(image, requested, profile, cancel):
    path=Path(requested)
    if path.suffix.lower()!='.png':
        raise StackError('출력 확장자는 .png로 지정하세요.')
    path.parent.mkdir(parents=True,exist_ok=True)
    index=0
    while True:
        check_cancel(cancel)
        candidate=path if index==0 else path.with_name(f'{path.stem} ({index}){path.suffix}')
        try:
            stream=candidate.open('xb')
            break
        except FileExistsError:
            index+=1
    try:
        with stream:
            Image.fromarray(image).save(stream,format='PNG',compress_level=6,icc_profile=profile)
        check_cancel(cancel)
    except BaseException:
        candidate.unlink(missing_ok=True)
        raise
    return candidate


def stack(paths, output, *, align=True, levels=5, window=55, memory_mb=6144,
          cancel=None, progress=None):
    """Stream frames into a winning-coefficient pyramid; inputs remain untouched."""
    started=time.perf_counter()
    paths=[Path(p).resolve() for p in paths]
    if not 2<=len(paths)<=100:
        raise StackError('서로 다른 초점의 사진 2~100장을 선택하세요.')
    if len(set(paths))!=len(paths):
        raise StackError('동일한 파일이 중복 선택되었습니다.')
    if not 1<=levels<=8 or not 3<=window<=101 or window%2==0:
        raise StackError('피라미드 1~8단계, 에너지 창은 3~101 사이 홀수여야 합니다.')
    if Path(output).suffix.lower()!='.png':
        raise StackError('출력 확장자는 .png로 지정하세요.')
    check_cancel(cancel)
    # Check dimensions and memory estimate before full image decoding/allocation.
    with Image.open(paths[len(paths)//2]) as meta:
        pixels=meta.width*meta.height
    if pixels*160 > memory_mb*1024**2:
        raise StackError(f'작업 메모리 예상치가 {memory_mb} MiB 제한을 넘습니다. 큰 메모리 설정 또는 작은 입력이 필요합니다.')
    ref_index=len(paths)//2
    reference,profile=read_image(paths[ref_index])
    h,w=reference.shape[:2]
    if min(h,w)<32:
        raise StackError('입력 이미지의 가로와 세로는 각각 32픽셀 이상이어야 합니다.')
    levels=min(levels,int(math.log2(min(h,w)))-2)
    best=pyramid(reference,levels)
    scores=[energy(p,window) for p in best[:-1]]
    # Preserve the reference low-frequency layer to avoid exposure pumping.
    # This policy is our choice; the recovered original coarsest-layer rule is unknown.
    order=[ref_index]+[i for i in range(len(paths)) if i!=ref_index]
    records=[]
    def report(done,message):
        if progress: progress(done,message)
    report(0,'기준 이미지 준비')
    for count,index in enumerate(order):
        check_cancel(cancel)
        if index==ref_index:
            records.append({'file':str(paths[index]),'reference':True})
            continue
        report(count/len(paths),f'{count+1}/{len(paths)} 정렬·합성: {paths[index].name}')
        image,icc=read_image(paths[index])
        if image.shape!=reference.shape:
            raise StackError('회전 방향을 적용한 입력 사진들의 크기가 서로 다릅니다.')
        if icc!=profile:
            raise StackError('입력의 색상 프로필이 서로 다릅니다. 같은 프로필로 내보내 주세요.')
        if align:
            image,valid,alignment=align_image(image,reference)
        else:
            valid=np.ones((h,w),np.uint8)
            alignment={'disabled':True}
        check_cancel(cancel)
        layers=pyramid(image,levels)
        mask=valid.astype(np.float32)
        for level in range(levels):
            check_cancel(cancel)
            # Conservatively exclude warp edges and energy-window neighbourhoods.
            reliable=cv2.erode((mask>.999).astype(np.uint8),np.ones((window,window),np.uint8),
                               borderType=cv2.BORDER_CONSTANT,borderValue=1)>0
            score=energy(layers[level],window)
            choose=(score>scores[level]) & reliable
            best[level][choose]=layers[level][choose]
            scores[level][choose]=score[choose]
            mask=cv2.pyrDown(mask)
        records.append({'file':str(paths[index]),'alignment':alignment})
        del layers,image
    check_cancel(cancel)
    report(.9,'피라미드 복원·PNG 저장')
    merged=reconstruct(best)
    actual=unique_save(merged,output,profile,cancel)
    result={'output':str(actual),'width':w,'height':h,'seconds':round(time.perf_counter()-started,3),
            'engine':'Clorium Stack 0.1','device':'CPU','levels':levels,'window':window,
            'reference':str(paths[ref_index]),'inputs':records}
    report(1.,f'완료: {actual.name}')
    return result


def main():
    parser=argparse.ArgumentParser(description='Clorium Stack — independent focus stacking engine')
    parser.add_argument('images',nargs='+')
    parser.add_argument('-o','--output',required=True)
    parser.add_argument('--no-align',action='store_true')
    parser.add_argument('--levels',type=int,default=5)
    parser.add_argument('--window',type=int,default=55)
    parser.add_argument('--memory-mb',type=int,default=6144)
    parser.add_argument('--cancel-file',type=Path)
    args=parser.parse_args()
    class FileCancellation:
        def is_set(self): return bool(args.cancel_file and args.cancel_file.exists())
    try:
        result=stack(args.images,args.output,align=not args.no_align,levels=args.levels,
                     window=args.window,memory_mb=args.memory_mb,cancel=FileCancellation(),
                     progress=lambda p,m:print(json.dumps({'progress':p,'message':m},ensure_ascii=False),flush=True))
        print(json.dumps(result,ensure_ascii=False),flush=True)
        return 0
    except Cancelled as exc:
        print(str(exc),flush=True)
        return 130
    except (StackError,OSError,cv2.error) as exc:
        print('ERROR: '+str(exc),flush=True)
        return 1


if __name__=='__main__':
    raise SystemExit(main())
