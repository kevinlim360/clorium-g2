# Magick.NET / ImageMagick — G2 색보정 엔진

G2 색보정(`CameraControl/Classes/G2ImageAdjust.cs`, `windows/G2ColorAdjustWnd.xaml`)은 기존 제품에 이미 포함된 아래 구성요소를 그대로 사용한다. 새 바이너리를 추가하지 않았다.

| 구성요소 | 파일 | 확인한 버전 | 라이선스 |
|---|---|---|---|
| Magick.NET (Q8, x64) | `dependencies/Managed/Magick.NET-Q8-x64.dll`, `Magick.NET.Core.dll` | 14.12.0 (파일 버전 리소스, netstandard2.0 빌드) | Apache License 2.0 — https://github.com/dlemstra/Magick.NET/blob/main/License.txt |
| ImageMagick 네이티브 | 런타임의 `Magick.Native-Q8-x64.dll` | Magick.NET 14.12.0 동봉판 | ImageMagick License — https://imagemagick.org/script/license.php |

- 저장소: https://github.com/dlemstra/Magick.NET (2013년부터 유지, GitHub 약 3.2k stars)
- 버전은 DLL의 FileVersion/ProductVersion(`14.12.0+158db3ad…`)으로 확인했다. 고객 재배포 전 THIRD-PARTY-NOTICES의 원칙대로 정확한 판본의 라이선스 원문과 고지 의무(Apache-2.0 NOTICE 포함)를 확인해 동봉해야 한다.
- 선택 근거: 이미 제품 의존성이며 JPEG/PNG/TIFF 디코딩·ICC/EXIF 보존·방향 처리·CLUT/색행렬 연산을 한 엔진에서 제공. ImageSharp(Six Labors Split License, 일정 매출 이상 상용 라이선스 필요), SkiaSharp(보정 연산 부족), OpenCvSharp(대형 네이티브 추가, ICC 처리 별도)는 제외.
