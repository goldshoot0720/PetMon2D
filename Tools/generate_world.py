"""Generate the Meowa overworld assets: terrain textures, dual-grid tilesets and map props.

Run in stages so tileset jobs always start from validated 64x64 textures:
    python Tools/generate_world.py textures
    python Tools/generate_world.py props
    python Tools/generate_world.py tilesets
Each job writes into MeowaOutputs/<name>/ and is skipped once final_outputs.json exists,
so an interrupted run resumes instead of resubmitting a paid task.
"""
import concurrent.futures, json, pathlib, subprocess, sys

ROOT = pathlib.Path(__file__).resolve().parents[1]
RUNNER = 'C:/Users/chbon/.agents/skills/game-assets/meowart_api.py'
OUT = ROOT / 'MeowaOutputs'

STYLE = 'Warm storybook anime JRPG map art for a cozy cat dream island, soft painted shading, gentle daylight.'

TEXTURES = {
    'tex_grass': 'Soft mint green meadow grass, short even blades, tiny pale highlights, flat overhead material with no shadows and no objects.',
    'tex_path': 'Pale cream sandy dirt path with fine gravel specks, warm light tone, flat overhead material with no shadows and no objects.',
    'tex_water': 'Calm shallow pond water, soft cornflower blue with gentle ripples, flat overhead material with no shadows and no objects.',
    'tex_tallgrass': 'Lush deep green tall meadow grass, dense upright blades seen from above, flat overhead material with no shadows and no objects.',
    'tex_floor': 'Warm honey wood plank floor for a cozy cottage interior, flat overhead material with no shadows and no objects.',
}

PROPS = {
    'prop_house_red': 'One small cottage with a round red tile roof, cream walls, a wooden door and two warm lit windows.',
    'prop_house_blue': 'One small cottage with a soft blue slate roof, cream walls, a wooden door and one round window.',
    'prop_house_hall': 'One larger cream hall with a wide teal roof, an arched double door and a small crescent moon emblem above it.',
    'prop_tree': 'One round leafy storybook tree with a short brown trunk and soft green canopy.',
    'prop_fence': 'One short horizontal wooden fence segment with two rails and three posts, pale weathered wood.',
    'prop_sign': 'One small wooden signpost with a blank board on a single post.',
    'prop_flowers': 'One small cluster of white and pink wildflowers with green leaves.',
    'prop_rock': 'One rounded grey boulder with soft moss on top.',
}

INTERIOR = {
    'prop_bed': 'One cosy single bed with a cream quilt, a soft pillow and a small wooden frame.',
    'prop_table': 'One small round wooden table with a teapot and two cups on it.',
    'prop_rug': 'One round woven rug with warm cream and sage stripes, lying flat.',
    'prop_shelf': 'One short wooden bookshelf with a few books and a small potted plant.',
}

PROP_SUFFIX = ('Three-quarter top-down game map view as used in a classic JRPG overworld, '
               'single object centered, isolated on a fully transparent background, '
               'no ground, no shadow on the ground, no scenery, no characters, no text.')

# name -> (terrain-mode, foreground texture, background texture, prompt)
TILESETS = {
    'tile_path_on_grass': ('dual', 'tex_path', 'tex_grass', 'Background is grass; foreground is dirt path.'),
    'tile_water_in_grass': ('dual', 'tex_water', 'tex_grass', 'Background is grass; foreground is water.'),
    'tile_tallgrass': ('foreground', 'tex_tallgrass', None, None),
}


def texture_file(name):
    """Return the downloaded 64x64 texture PNG for a finished texture job."""
    folder = OUT / name
    manifests = list(folder.rglob('final_outputs.json'))
    if not manifests:
        raise SystemExit('texture not generated yet: ' + name)
    manifest = json.loads(manifests[0].read_text(encoding='utf-8'))
    for item in manifest.get('outputs', []):
        src = pathlib.Path(item['path'])
        if not src.is_absolute():
            src = ROOT / src
        if src.suffix.lower() == '.png':
            return src
    raise SystemExit('no PNG output for texture: ' + name)


def build_jobs(stage):
    jobs = []
    if stage == 'textures':
        for name, body in TEXTURES.items():
            jobs.append((name, ['texture-gen-run', '--prompt', body, '--self-loop']))
    elif stage == 'props':
        for name, body in PROPS.items():
            prompt = ' '.join([body, STYLE, PROP_SUFFIX])
            jobs.append((name, ['image-2.5-run', '--prompt', prompt, '--aspect-ratio', '1:1',
                                '--resolution', '1K', '--quality', 'standard',
                                '--remove-bg-method', 'standard']))
    elif stage == 'interior':
        for name, body in INTERIOR.items():
            prompt = ' '.join([body, STYLE, PROP_SUFFIX])
            jobs.append((name, ['image-2.5-run', '--prompt', prompt, '--aspect-ratio', '1:1',
                                '--resolution', '1K', '--quality', 'standard',
                                '--remove-bg-method', 'standard']))
    elif stage == 'walls':
        jobs.append(('tex_wall', ['texture-gen-run', '--self-loop', '--prompt',
                                  'Warm cream plaster cottage wall with faint horizontal wood beams, '
                                  'flat material with no shadows and no objects.']))
    elif stage == 'anims':
        for hero in ('baibai', 'bubu'):
            for facing in ('down', 'up', 'left', 'right'):
                sprite = ROOT / 'Assets/Resources/Art' / ('walk_%s_%s.png' % (hero, facing))
                jobs.append(('anim_%s_%s' % (hero, facing), [
                    'animate-run', '--image-file', str(sprite),
                    '--prompt', 'The character walks forward in place, legs alternating, tail swaying gently',
                    '--animation-type', 'walk', '--output-frames', '8',
                    '--output-format', 'spritesheet', '--remove-bg-method', 'standard',
                    '--padding-top', '60', '--padding-down', '30',
                    '--padding-left', '40', '--padding-right', '40']))
    elif stage == 'tilesets':
        for name, (mode, fg, bg, prompt) in TILESETS.items():
            args = ['tileset-gen-run', '--terrain-mode', mode,
                    '--foreground-texture', str(texture_file(fg))]
            if bg:
                args += ['--background-texture', str(texture_file(bg))]
            else:
                args += ['--remove-bg-method', 'standard']
            if prompt:
                args += ['--prompt', prompt]
            jobs.append((name, args))
    else:
        raise SystemExit('unknown stage: ' + stage)
    return jobs


def run(job):
    name, args = job
    folder = OUT / name
    folder.mkdir(parents=True, exist_ok=True)
    if list(folder.rglob('final_outputs.json')):
        return name + ': already completed'
    log = folder / 'runner.log'
    with log.open('w', encoding='utf-8') as f:
        result = subprocess.run([sys.executable, '-X', 'utf8', RUNNER, *args,
                                 '--output-dir', str(folder)],
                                cwd=ROOT, stdout=f, stderr=subprocess.STDOUT)
    tail = [line for line in log.read_text(encoding='utf-8').splitlines()
            if 'progress=' not in line][-4:]
    return name + ': rc=' + str(result.returncode) + ' | ' + ' / '.join(tail)


if __name__ == '__main__':
    stage = sys.argv[1] if len(sys.argv) > 1 else 'textures'
    jobs = build_jobs(stage)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        for line in pool.map(run, jobs):
            print(line, flush=True)
