import concurrent.futures, json, pathlib, subprocess, sys, re

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = 'C:/Users/chbon/.agents/skills/game-assets/meowart_api.py'
refs = sorted(ROOT.glob('ChatGPT*.png')) + list(ROOT.glob('Screenshot*.png'))
names = ['baibai','bubu','fengxiong','yamei','xiaotu','fengbang','xiaoying','yumei','fengshi','fengzong','tudong','gugu']
jobs = []
for name, ref in zip(names, refs):
    prompt = 'Create one full body chibi JRPG character sprite based on this reference. Preserve the distinctive face, hair or fur, clothing and accessories. Warm storybook anime illustration, clean painted shading, three-quarter front view, friendly battle-ready standing pose, centered with generous transparent margin, no scenery, no text, no extra characters. Match a cozy cat dream adventure game.'
    jobs.append((name, ['image-2.5-run','--prompt',prompt,'--reference-image',str(ref),'--remove-bg-method','standard'], prompt))
for name, prompt in [
    ('world','A top-down illustrated JRPG dream island map, wide landscape. Soft mint meadows, cream winding paths connecting a cozy red roof village on the left to a blue pond in the middle and a small moonlit castle on the right. Trees and flower beds around the perimeter, open walkable center, warm storybook anime style. No characters, no text, no UI. Landscape fills entire frame.'),
    ('battle','A cozy storybook anime JRPG battle background, wide landscape. Sunlit mint meadow with wildflowers and distant blue mountains, dreamy luminous sky. Two empty grassy battle clearings at lower left and middle right. No characters, no text, no UI.'),
    ('title','A beautiful cozy storybook anime dream landscape at dusk, wide landscape. A tiny cottage among wildflowers on the far left, a winding cream path leading into distant blue mountains, huge luminous crescent moon upper right. Warm peach and sage green with deep blue sky. Open quiet central composition for a game title. No characters, no text, no UI.'),
    ('ui','A single ivory parchment panel for a cozy storybook cat RPG user interface. Subtle warm paper texture, elegant thin brown outline, gently rounded corners, empty cream interior, front view, fills square canvas with small transparent margin. No text, no symbols, no other objects.'),
    ('spark','One magical golden starburst impact effect for a cozy anime JRPG, soft yellow sparks and small mint stars around one bright central flash, isolated on transparent background, no text, no scenery.')]:
    args=['image-2.5-run','--prompt',prompt]
    args += ['--remove-bg-method','standard'] if name in ('ui','spark') else ['--aspect-ratio','16:9']
    jobs.append((name,args,prompt))

def run(job):
    name,args,prompt=job
    out=ROOT/'MeowaOutputs'/name
    out.mkdir(parents=True,exist_ok=True)
    if list(out.rglob('final_outputs.json')):
        return name+' already completed'
    log=out/'runner.log'
    if log.exists():
        ids=re.findall(r'api_job_id=(job_[a-z0-9]+)',log.read_text(encoding='utf-8'))
        if ids: args=['image-2.5-poll','--job-id',ids[-1]]
    with log.open('w',encoding='utf-8') as f:
        result=subprocess.run([sys.executable,'-X','utf8',RUNNER,*args,'--output-dir',str(out)],cwd=ROOT,stdout=f,stderr=subprocess.STDOUT)
    return name+': '+str(result.returncode)+' '+log.read_text(encoding='utf-8')[-700:]

if __name__=='__main__':
    (ROOT/'Tools'/'asset-prompts.json').write_text(json.dumps({n:p for n,a,p in jobs},ensure_ascii=False,indent=2),encoding='utf-8')
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for result in pool.map(run,jobs): print(result,flush=True)
