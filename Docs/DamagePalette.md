# Damage color coding

| Damage | Visual family |
| --- | --- |
| Physical | Natural material colors; original stone, metal and arrow artwork |
| Fire | Red-orange shadows and body, pale orange core |
| Ice | Pale cyan and near-white blue highlights |
| Electric | Golden-yellow body and creamy near-white yellow core |
| Magic | Violet and lavender |

These are all five DamageType values. Poison is not a separate damage type.

DamagePalette supplies shared world and UI palette materials from Resources/Effects/Damage Palettes. The atlas, sprite shapes, alpha, pixel grid and animation remain unchanged. UI uses a stencil/RectMask compatible variant. World sprites use the project's URP 2D sprite instancing/flip path. Palette shaders map existing brightness into shadow, midtone and highlight colors without resampling textures.

EnemyProjectile applies the palette from the actual damage type at Setup/SetupHit, including enemy and defense tower magic projectiles. Physical artwork retains its original material. Impacts use the same damage family. Lightning flash/glow, frost area and burning light/trails follow the same color language.

The obsolete Resources/Iron.png sprite was replaced in the Iron ResourceType, ground/carry/UI references and notifications, then deleted after dependency verification. Iron's persistent resource ID was preserved. Additional obsolete weapon artwork is listed in .ai/damage-palette/cleanup.txt; referenced assets are retained.

Validation: DamagePaletteValidation.Run in an isolated Play Mode fixture checks six weapon icon/effect palettes, runtime projectiles, both shader variants, current iron and removed old asset. Visual comparison: Temp/DamagePaletteQA/palette.png.
