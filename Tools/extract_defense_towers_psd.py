"""Extract original visible defense-tower layers, without resampling or painting."""
from pathlib import Path
import json
from psd_tools import PSDImage

source = Path(r"C:/Users/schar/Downloads/Telegram Desktop/hex1 (10).psd")
psd = PSDImage.open(source)
layers = list(psd.descendants())
out = Path("Assets/Sprites/World/Defense Towers")
out.mkdir(parents=True, exist_ok=True)
manifest = {}
for name, index in [("Magic Tower", 236), ("Arrow Tower", 247), ("Stone Tower", 262)]:
    group = layers[index]
    image = group.composite(viewport=group.bbox, layer_filter=lambda layer: layer.is_visible() and layer != layers[237])
    image.save(out / (name + ".png"))
    manifest[name] = {"group": index, "bounds": group.bbox, "layers": [layers.index(l) for l in group.descendants() if l.is_visible() and l != layers[237]]}
layers[237].topil().save(out / "Magic Projectile.png")
layers[274].topil().save(out / "Stone Projectile.png")
manifest["Stone Projectile"] = {"layer": 274, "bounds": layers[274].bbox}
manifest["Magic Projectile"] = {"layer": 237, "bounds": layers[237].bbox}
(out / "Source.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps(manifest))
