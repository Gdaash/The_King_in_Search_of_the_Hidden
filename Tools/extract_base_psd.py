"""Extract original PSD pixels without resampling; record bottom-to-top placement."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image
from psd_tools import PSDImage

SOURCE = Path(r"C:/Users/schar/Desktop/Base.psd")
ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/Sprites/World/Base/Layers"
NAMES = [
    "Background", "Roads", "Ravine", "Forest", "Trees Back", "Fields", "Rocks",
    "Castle", "Castle Road", "Bridge", "House 1", "House 2", "Castle Bridge and Fir",
    "Portal", "Rocks Beneath Houses", "House 3", "House 5", "House 4", "Archery Range Annex",
    "Warehouse", "Warehouse Barrels and Crates", "Magic Library Glow", "Magic Library",
    "Laboratory", "Tree 1", "Blacksmith", "House 6", "Trees Front", "Refugee Camp",
    "Market", "Tree 2", "Archery Range", "Fort", "House 7", "Tree 3", "Tree 4", "Tree 5",
]

def main():
    psd = PSDImage.open(SOURCE)
    layers = list(psd.descendants())
    assert len(layers) == len(NAMES)
    OUT.mkdir(parents=True, exist_ok=True)
    assembled = Image.new("RGBA", psd.size)
    records = []
    for order, (layer, name) in enumerate(zip(layers, NAMES)):
        assert not layer.is_group() and layer.blend_mode.value == b"norm"
        assert layer.opacity == 255 and not layer.has_mask()
        pixels = layer.topil().convert("RGBA")
        filename = f"{order:02d}_{name.replace(' ', '_')}.png"
        pixels.save(OUT / filename)
        left, top, right, bottom = layer.bbox
        assert pixels.size == (right - left, bottom - top)
        records.append(dict(name=name, originalName=layer.name, assetPath=(OUT / filename).relative_to(ROOT).as_posix(),
                            left=left, top=top, width=pixels.width, height=pixels.height, order=order, visible=layer.visible))
        if layer.visible:
            assembled.alpha_composite(pixels, (left, top))
    reference = psd.topil().convert("RGBA")
    verification = ROOT / "Temp/BaseSceneVerification"
    verification.mkdir(parents=True, exist_ok=True)
    reference.save(verification / "Photoshop.png")
    assembled.save(verification / "ExtractedLayers.png")
    diff = np.abs(np.asarray(reference).astype(int) - np.asarray(assembled).astype(int))
    report = dict(width=psd.width, height=psd.height, pixelsPerUnit=32,
                  sourceSha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(), layers=records)
    (OUT.parent / "BaseSceneLayers.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(dict(layers=len(records), size=psd.size, differingPixels=int(np.any(diff, axis=2).sum()), maximumChannelDifference=int(diff.max()))))

if __name__ == "__main__":
    main()
