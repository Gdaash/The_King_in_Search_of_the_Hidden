"""Extract only the danger HUD from hex1 (9).psd. No resampling or painted pixels."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image
from psd_tools import PSDImage


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("psd", type=Path)
    args = parser.parse_args()
    psd = PSDImage.open(args.psd)
    layers = list(psd.descendants())
    out = Path("Assets/Sprites/Ui/Alarm")
    out.mkdir(parents=True, exist_ok=True)
    manifest = {}

    def composite(indices, box):
        image = Image.new("RGBA", (box[2] - box[0], box[3] - box[1]))
        for index in indices:
            layer = layers[index]
            image.alpha_composite(layer.topil().convert("RGBA"), (layer.left - box[0], layer.top - box[1]))
        return image

    def save(name, image, indices, box):
        image.save(out / (name + ".png"))
        manifest[name] = dict(layers=indices, bounds=box, size=image.size)
        return image

    # Transparent top padding keeps the whole bar on its original 227 x 25 canvas.
    box = (314, 29, 541, 54)
    frame = save("Alarm Frame", composite([860], box), [860], box)
    start = save("Start Divider", layers[868].topil(), [868], layers[868].bbox)
    dividers = composite([864, 865, 867, 869], box)
    ticks = []
    for i, x in enumerate([381, 426, 471, 516]):
        crop = (x - box[0], 0, x - box[0] + 3, 21)
        ticks.append(save(f"Threshold Divider {i + 1}", dividers.crop(crop), [864, 865, 867, 869], (x, 29, x + 3, 50)))

    active = save("Threshold Skull Active", layers[870].topil(), [870], layers[870].bbox)
    inactive = Image.new("RGBA", (9, 12))
    inactive.alpha_composite(layers[871].topil().convert("RGBA"), (0, 1))
    save("Threshold Skull Inactive", inactive, [871], (423, 42, 432, 54))
    save("Alarm Skull", layers[847].topil(), [847], layers[847].bbox)

    # The PSD has a partial fill. Extend its identical middle columns by copying,
    # retaining the exact row colours and original first column; no resizing.
    source_fill = layers[863].topil().convert("RGBA")
    fill = Image.new("RGBA", (180, 8))
    fill.paste(source_fill, (0, 0))
    column = source_fill.crop((1, 0, 2, 8))
    for x in range(source_fill.width, fill.width):
        fill.paste(column, (x, 0))
    save("Alarm Fill", fill, [863], (338, 35, 518, 43))

    # Reassemble the visible PSD example and prove the extracted pixels match it.
    actual = frame.copy()
    actual.alpha_composite(fill.crop((0, 0, 71, 8)), (24, 6))
    actual.alpha_composite(start, (22, 0))
    for i, tick in enumerate(ticks):
        actual.alpha_composite(tick, (67 + i * 45, 0))
        actual.alpha_composite(active if i == 0 else inactive, (64 + i * 45, 13))
    expected = composite([860, 863, 864, 865, 867, 868, 869, 870, 871, 872, 873], box)
    a, b = np.array(actual), np.array(expected)
    assert np.array_equal(a[:, :, 3], b[:, :, 3])
    assert np.array_equal(a[a[:, :, 3] > 0], b[b[:, :, 3] > 0]), "Extraction differs from PSD"
    temp = Path("Temp/AlarmPsd")
    temp.mkdir(parents=True, exist_ok=True)
    actual.save(temp / "extracted-preview.png")
    (temp / "extraction.json").write_text(json.dumps(dict(source=str(args.psd),
        sha256=hashlib.sha256(args.psd.read_bytes()).hexdigest(), pixel_match=True,
        sprites=manifest), ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Exported {len(manifest)} sprites; assembled pixels match the PSD exactly.")


if __name__ == "__main__":
    main()
