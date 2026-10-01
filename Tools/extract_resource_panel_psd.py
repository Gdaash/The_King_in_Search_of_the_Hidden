"""Extract only the reusable panel pieces and five requested icons. Never resample PSD pixels."""
from pathlib import Path
import json
from PIL import Image
from psd_tools import PSDImage

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/Art/ResourcePanel'
PSD = PSDImage.open('C:/Users/schar/Desktop/панель ресурсов.psd')
LAYERS = list(PSD.descendants())

def merge(indices, box):
    result = Image.new('RGBA', (box[2] - box[0], box[3] - box[1]))
    for index in indices:
        layer = LAYERS[index]
        image = layer.topil().convert('RGBA')
        result.alpha_composite(image, (layer.left - box[0], layer.top - box[1]))
    return result

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    pieces = [
        ('Left Cap', [1], (13, -1, 34, 18)),
        ('Right Cap', [1], (605, -1, 626, 18)),
        ('Resource Cell', [17, 22], (490, 0, 548, 27)),
        ('Crown', [24], (46, 8, 57, 18)),
        ('Cart', [46], (325, 8, 345, 21)),
        ('Sword', [47, 49], (437, 4, 462, 22)),
        ('Bow', [48], (499, 4, 511, 22)),
        ('IronOre', [47, 49], (551, 4, 576, 22)),
    ]
    records = []
    for name, indices, box in pieces:
        image = merge(indices, box)
        if name in ('Sword', 'IronOre'):
            tight = image.getbbox()
            image = image.crop(tight)
            box = (box[0] + tight[0], box[1] + tight[1], box[0] + tight[2], box[1] + tight[3])
        image.save(OUT / (name + '.png'))
        records.append(dict(name=name, layers=indices, bounds=box, size=image.size))
    (ROOT / 'Temp/ResourcePanelVerification/Extraction.json').write_text(json.dumps(records, indent=2))
    print(json.dumps(records))

if __name__ == '__main__':
    main()
