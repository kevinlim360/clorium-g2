from pathlib import Path
import cv2, json
from PIL import Image,ImageOps,ImageDraw
p=Path('D:/CloriumImages2/2026년10월/27일/test/Superfocus/20261028_112740_9d158c76')
fs=sorted(p.glob('*.JPG'))
canvas=Image.new('RGB',(1200,460),'white')
for i,f in enumerate([fs[0],fs[len(fs)//2],fs[-1]]):
 im=Image.open(f); im.load(); im.thumbnail((400,420)); canvas.paste(im,(400*i,30)); ImageDraw.Draw(canvas).text((400*i+8,8),f.name[:4],fill='black')
canvas.save('evidence/superfocus-ab-originals-preview.jpg')
print(json.dumps([{'name':f.name,'size':Image.open(f).size} for f in fs]))
