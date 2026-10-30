from pathlib import Path
import cv2, json, hashlib, numpy as np
from PIL import Image,ImageDraw
p=Path('D:/CloriumImages2/2026년10월/27일/test/Superfocus/20261028_123912_5045285d')
fs=sorted(p.glob('*.JPG'))
if (p/'Superfocus.png').exists(): fs += [p/'Superfocus.png']
records=[]
# Fixed image regions: patterned near vase and distant fan; no registration for this diagnostic.
rois={'near_vase':(1100,1800,2100,3100),'far_blind':(4650,1350,5720,2400)}
for f in fs:
 im=Image.open(f); im.load()
 a=cv2.imdecode(np.fromfile(str(f),dtype=np.uint8),cv2.IMREAD_GRAYSCALE)
 scores={k:round(float(cv2.Laplacian(a[y1:y2,x1:x2],cv2.CV_64F).var()),3) for k,(x1,y1,x2,y2) in rois.items()}
 records.append(dict(file=f.name,dimensions=im.size,bytes=f.stat().st_size,sha256=hashlib.sha256(f.read_bytes()).hexdigest(),sharpness=scores))
canvas=Image.new('RGB',(1200,460),'white')
preview=[fs[0],fs[-2] if fs[-1].suffix=='.png' else fs[-1],fs[-1]]
for i,f in enumerate(preview):
 im=Image.open(f); im.thumbnail((400,420));canvas.paste(im,(400*i,30));ImageDraw.Draw(canvas).text((400*i+8,8),f.name[:12],fill='black')
canvas.save('evidence/superfocus-box-quality-preview.jpg')
Path('evidence/superfocus-box-files.json').write_text(json.dumps({'folder':str(p),'rois':rois,'files':records},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records[::max(1,len(records)-1)],ensure_ascii=False))


