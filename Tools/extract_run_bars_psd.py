"""Copy HUD pixels from hex1 (11).psd without resampling."""
from pathlib import Path
from PIL import Image
from psd_tools import PSDImage

p = PSDImage.open('C:/Users/schar/Downloads/Telegram Desktop/hex1 (11).psd')
l = list(p.descendants())
out = Path('Assets/Sprites/Ui/Run Bars')
out.mkdir(parents=True, exist_ok=True)
def save(name, im):
    im.convert('RGBA').save(out / (name + '.png'))
save('Experience Frame', l[849].topil().crop((0, 0, 180, 16)))
save('Alarm Frame', l[848].topil())
save('Experience Crystal', l[850].topil())
save('Divider', l[869].topil())
save('Skull Active', l[872].topil())
inactive = Image.new('RGBA', (9, 12))
inactive.alpha_composite(l[873].topil().convert('RGBA'), (0, 1))
save('Skull Inactive', inactive)
for name, index, width in [('Experience Fill',854,135), ('Alarm Fill',853,177)]:
    src = l[index].topil().convert('RGBA')
    im = Image.new('RGBA', (width,8))
    im.paste(src,(0,0))
    for x in range(src.width,width):
        im.paste(src.crop((1,0,2,8)),(x,0))
    save(name,im)
print('Exported 8 sprites from source layers.')
