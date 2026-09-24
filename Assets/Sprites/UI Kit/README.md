# UI Kit from UI.psd

Source: `Assets/UI.psd` (640 x 360).

The PSD was exported without repainting or regenerating any artwork. Each popup folder contains:

- `_PopupPreview.png` — flattened visual reference for the original composition;
- `_GroupPreview.png` — flattened references for nested groups;
- numbered PNG files — trimmed original PSD layers, including hidden alternative states marked `_Hidden`;
- `UI_Kit_Manifest.json` — source group, visibility and bounding box for every exported item.

`Components` contains the reusable layers selected from those exports. `Component_Source_Map.json` records the exact source layer for every selected component.

## Unity import rules

- Sprite (2D and UI), Single;
- 32 Pixels Per Unit;
- Point filtering;
- no mip maps;
- no texture compression;
- Clamp wrapping.

Every component under `Components/Backgrounds` and `Components/Controls` has a nonzero sprite border. Its matching prefab uses `Image.Type.Sliced`. Decorative headers retain their original aspect and use `Image.Type.Simple`.

Canvas images default to twice the source pixel dimensions. Stretchable sliced images may be resized to their container.

## Prefabs

Reusable uGUI prefabs are in `Assets/Prefabs/UI Kit`. `UIKit Catalog.prefab` is an overview of the shared panel, card, header and button pieces.

Text visible in PSD previews is reference content only. Runtime text should remain TextMeshPro text connected to the localization system.
