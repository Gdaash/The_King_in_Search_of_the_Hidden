# Sprite organization

## Current assets — Assets/Sprites

| Folder | Contents |
| --- | --- |
| Branding | Current game logo |
| Ui/Common | Shared buttons, backgrounds and UI elements |
| Ui/Icons | Construction and other interface icons |
| Ui/UnitStats | Unit characteristic icons |
| Ui/ResourcePanel | Resource panel parts |
| Ui/Islands | Building island sprites |
| Ui/Kit | Referenced UI Kit components and source manifests |
| Ui/Evolution | Referenced interface assets from the original art pack |
| Units | Allies, enemies and NPCs |
| World | Base artwork, buildings, hexes, tiles and backgrounds |
| Resources | Resource icons and assets that may be loaded by Resources.Load |

253 sprite assets are retained here. References from all serialized scenes,
prefabs, ScriptableObjects, materials, animation assets and sprite atlases were
considered, including backup prefabs. Assets inside Resources folders were kept
conservatively because they may be loaded by name at runtime.

## Archive — Assets/SpritesOld

2,243 sprite assets without those references were moved here, preserving their
original relative folder structure. Two additional unused texture source sheets
are archived here as well. Archived assets have not been deleted; they can still
be inspected and reused. This folder remains inside Assets and is imported by Unity.

## Preservation and validation

- All 2,496 sprite files retain their exact bytes, metadata, GUIDs and sprite subasset IDs.
- Dependency GUID sets of 448 serialized roots remain identical, including all three scenes.
- The open Base scene retains the same sprite references.
- Resources-relative runtime load paths are preserved.
- Editor setup scripts, extraction scripts and PSD manifests use the new paths.
- No sprite sizing, PPU, filtering, compression or scene layout was changed.

`SpriteMoves.json` records every original path, destination and GUID, together with
the validation snapshot. `Temp/SpriteOrganizationPlan.json` is the local working
journal. The editor helper `SpriteOrganizationTool` is a one-time migration tool;
do not discard the journal and rerun it over the already reorganized project.
