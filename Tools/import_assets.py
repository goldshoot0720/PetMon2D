import json, pathlib, shutil
from PIL import Image, ImageOps, ImageDraw, PngImagePlugin
root=pathlib.Path(__file__).resolve().parents[1]
dest=root/'Assets/Resources/Art'
dest.mkdir(parents=True,exist_ok=True)
prompts=json.loads((root/'Tools/asset-prompts.json').read_text(encoding='utf-8'))
report=[]
thumbs=[]
for folder in sorted((root/'MeowaOutputs').iterdir()):
    manifests=list(folder.rglob('final_outputs.json'))
    if not manifests: continue
    manifest=json.loads(manifests[0].read_text(encoding='utf-8'))
    outputs=manifest.get('outputs',[])
    for item in outputs[:1]:
        src=pathlib.Path(item['path']);
        if not src.is_absolute(): src=(root/src)
        target=dest/(folder.name+src.suffix)
        if src.suffix.lower()=='.png':
            img=Image.open(src).convert('RGBA')
            info=PngImagePlugin.PngInfo();info.add_text('Source','Meowa '+manifest.get('job_id',''));info.add_text('Prompt',prompts.get(folder.name,''))
            img.save(target,pnginfo=info)
            alpha=img.getchannel('A').getextrema()
            report.append({'name':folder.name,'size':img.size,'alpha':alpha,'job':manifest.get('job_id')})
            preview=Image.new('RGB',(320,270),'#d7e0dd'); small=ImageOps.contain(img,(300,235));preview.paste(small,((320-small.width)//2,0),small)
            ImageDraw.Draw(preview).text((10,247),folder.name,fill='#17392d');thumbs.append(preview)
        else: shutil.copy2(src,target)
sheet=Image.new('RGB',(1280,270*((len(thumbs)+3)//4)),'#d7e0dd')
for i,im in enumerate(thumbs): sheet.paste(im,((i%4)*320,(i//4)*270))
(root/'Documentation').mkdir(exist_ok=True)
sheet.save(root/'Documentation/assets-preview.jpg')
(root/'Documentation/asset-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
