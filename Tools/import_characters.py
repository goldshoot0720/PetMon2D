"""Import Meowa multi-view character sheets as the overworld walking sprites.

character-multi-view-run returns one PNG per direction. Crop all four cardinal
views to a single shared bounding box so the hero keeps the same scale and
footing whichever way they face, then write them as walk_<hero>_<dir>.png.
"""
import json, pathlib
from PIL import Image, PngImagePlugin

FRAMES = 8   # must match DreamMap.WalkFrames


def import_walk_cycle(hero, facing):
    """Turn one Meowa walk animation into a horizontal strip of FRAMES frames.

    Frames are read from the animated WebP and cropped to one shared bounding box
    so the character does not jitter between frames.
    """
    folder = ROOT / 'MeowaOutputs' / ('anim_%s_%s' % (hero, facing))
    clips = list(folder.rglob('result_output_url.webp'))
    if not clips:
        return None
    clip = Image.open(clips[0])
    frames = []
    for index in range(min(FRAMES, getattr(clip, 'n_frames', 1))):
        clip.seek(index)
        frames.append(clip.convert('RGBA'))
    while len(frames) < FRAMES:
        frames.append(frames[-1].copy())

    boxes = [f.getchannel('A').getbbox() for f in frames if f.getchannel('A').getbbox()]
    if not boxes:
        return None
    shared = (min(b[0] for b in boxes), min(b[1] for b in boxes),
              max(b[2] for b in boxes), max(b[3] for b in boxes))
    cut = [f.crop(shared) for f in frames]
    w, h = cut[0].size
    strip = Image.new('RGBA', (w * FRAMES, h), (0, 0, 0, 0))
    for i, f in enumerate(cut):
        strip.paste(f, (i * w, 0), f)
    return strip

ROOT = pathlib.Path(__file__).resolve().parents[1]
DEST = ROOT / 'Assets/Resources/Art'
VIEWS = {'front': 'down', 'back': 'up', 'left': 'left', 'right': 'right'}

report = []
for folder in sorted((ROOT / 'MeowaOutputs').glob('dir_*')):
    hero = folder.name[len('dir_'):]
    manifests = list(folder.rglob('final_outputs.json'))
    if not manifests:
        print(hero + ': not generated yet')
        continue
    job = json.loads(manifests[0].read_text(encoding='utf-8')).get('job_id', '')
    source = manifests[0].parent
    images = {}
    for name, facing in VIEWS.items():
        path = source / (name + '.png')
        if path.is_file():
            images[facing] = Image.open(path).convert('RGBA')
    if len(images) != len(VIEWS):
        print('%s: only %d of %d views present, skipped' % (hero, len(images), len(VIEWS)))
        continue

    boxes = [im.getchannel('A').getbbox() for im in images.values()]
    shared = (min(b[0] for b in boxes), min(b[1] for b in boxes),
              max(b[2] for b in boxes), max(b[3] for b in boxes))
    for facing, im in images.items():
        strip = import_walk_cycle(hero, facing)
        info = PngImagePlugin.PngInfo()
        if strip is not None:
            info.add_text('Source', 'Meowa ' + job + ' multi-view + walk animation ' + facing)
            out = strip
        else:
            info.add_text('Source', 'Meowa ' + job + ' multi-view ' + facing)
            out = im.crop(shared)
        target = DEST / ('walk_%s_%s.png' % (hero, facing))
        out.save(target, pnginfo=info)
        report.append((target.name, out.size,
                       '%d frames' % FRAMES if strip is not None else 'single pose'))

for name, size, kind in report:
    print('%-24s %-14s %s' % (name, size, kind))
print('%d walking sprites imported' % len(report))
