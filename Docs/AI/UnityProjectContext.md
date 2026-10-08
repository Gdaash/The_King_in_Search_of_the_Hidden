# Unity project context

Inspected 2026-10-07 for the introductory comic feature.

- Confirmed root: `E:/The_King_in_Search_of_the_Hidden`, Unity 6000.5.8f1, Windows standalone target.
- URP 17.5, uGUI/TMP, both legacy and new input enabled. Existing runtime UI uses Canvas prefabs.
- First-party runtime code primarily uses Assembly-CSharp under `Assets/Scripts/GameFoundation`; editor utilities live in `Assets/Scripts/Editor`. Tools and InventoryEngine have separate assemblies. Zenject, UniTask and PrimeTween are available; the comic does not require new dependencies.
- Startup scene: `MainMenu`; existing gameplay scenes: `Base` and `World`. MainMenuController selects one of three save slots before loading Base. RunSceneRouter handles Base/World travel.
- IntroComic is appended to Build Settings. New slots and interrupted introductions route through it; existing saves proceed to Base. See `Docs/IntroComic.md` for the panel reveal and localization setup.
- Persistence: SaveSlotPrefs delegates to GameSaveService, which stores a versioned JSON PlayerPrefs envelope for each slot, plus legacy key migration. Selecting a slot creates it immediately, so determine emptiness before calling Select.
- Localization: custom LocalizationService + LocalizationTable + LocalizedText, not the Unity Localization package. The shared table is `Assets/Resources/Localization/Base Localization.asset`; currently ru/en. The persistent service owns language selection and the existing settings UI changes it.
- UI instructions: `AGENTS.md`; sprite RectTransforms use twice source pixel dimensions, PPU 32, Point filtering, no compression. Resource amount UI requires ResourceType icons.
- Connected Unity MCP RunCommand and Console tools are available. Existing project checks use editor audit scripts, including Play Mode checks. Test Framework 1.7 is installed; no dedicated first-party test assembly was found.
- Baseline Console had no reported errors; existing obsolete FindObjects API and legacy serialization warnings were present. The Base scene was open, not dirty, outside Play Mode.

Relevant source paths: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `ProjectSettings/EditorBuildSettings.asset`, `Assets/Scripts/GameFoundation/Saves`, `Assets/Scripts/GameFoundation/Localization`, `Assets/Scripts/GameFoundation/MetaProgression/RunSceneRouter.cs`.
