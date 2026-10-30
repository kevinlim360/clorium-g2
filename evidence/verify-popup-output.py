from pathlib import Path
from PIL import Image
import hashlib,json
root=Path('D:/CloriumImages2/2026년10월/27일/test/Superfocus')
folders=sorted(root.glob('20261028_17*'),key=lambda p:p.stat().st_mtime,reverse=True)[:2]
records=[]
for folder in folders:
    files=[]
    for f in sorted(folder.glob('*')):
        if f.suffix.lower() not in ('.jpg','.png'): continue
        with Image.open(f) as im:
            im.load()
            files.append(dict(file=f.name,dimensions=im.size,bytes=f.stat().st_size,sha256=hashlib.sha256(f.read_bytes()).hexdigest()))
    records.append(dict(folder=str(folder),files=files))
Path('evidence/superfocus-popup-files.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records,ensure_ascii=False))
