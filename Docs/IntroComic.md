# Introductory comic

`Assets/Scenes/IntroComic.unity` is registered after the existing scenes in Build Settings, preserving their indices. Start MainMenu, press Play, and select an empty slot to view it.

The first panel appears automatically. Each left click reveals one additional panel with a short fade: four panels on page 1, three on page 2, three on page 3. Revealed panels remain visible. A complete page waits for another click before changing pages. After the final panel, the next click loads the gameplay scene configured by MainMenuController (currently Base).

MainMenuController checks slot existence before SaveSlotPrefs.Select creates the save. A new slot receives `foundation.intro.pending = 1` inside its existing save envelope. Completion clears the flag. Interrupted intros replay on the next selection; existing and legacy saves without that key continue directly into gameplay. No save-format migration or package changes are required.

## Artwork and localization

The three language-neutral PNGs are in `Assets/Sprites/Ui/IntroComic`. The first two retain empty narration/speech balloons. The final page has no sound-effect lettering or burst. The original approved art remains under `Docs/Art/IntroComic/DrawnComic_2026-10-07`.

The six story strings and two input hints are in the existing `Base Localization` table under `intro.*`, with Russian and English translations. Each label uses the existing LocalizedText component and LocalizationService. Language changes refresh visible labels and newly revealed panels. Adding another language does not require editing the PNGs; provide translations and a font with the required glyph coverage.

The dedicated TMP font uses the project's existing Roboto-Bold source. Text auto-sizes inside the empty balloons. The Canvas reference is 1920×1080; the composition fits uniformly within the viewport, with a localized click hint below. Native art dimensions are 1672×941, 1672×941 and 1671×941. Every Image RectTransform is twice its sprite dimensions; the composition parent handles screen fitting. Sprites use PPU 32, Point filtering, no compression and no mipmaps.

Each section uses RectMask2D to show the correct region of its page. Artwork is referenced directly by the scene. No runtime Resources lookup or texture copying is needed.

## Validation

- Unity 6000.5.8f1 compiled the new runtime and editor scripts.
- `IntroComicAudit.RunEdit`: 54 checks passed for progression, eight keys in both locales, font coverage, imports, scene registration, missing references and Canvas sizing.
- `IntroComicAudit.BeginPlay`: 68 runtime checks passed through real scene transitions and EventSystem pointer-click dispatch. Covered empty-slot launch, one panel per click, page boundaries, interrupted-intro replay, language changes, text fitting, Base transition, existing-save continuation and legacy-save bypass.
- Captured and inspected 1920×1080 screenshots in Russian/English plus 1280×960 and 2560×1080 framing. Results are under `Docs/Art/IntroComic/IntegrationValidation`.
- The runtime audit used previously empty slot 3, removed only its test-created data afterwards and restored language/selection preferences. Existing slot 1's saved envelope was unchanged. Save caches are invalidated after restoration so the Editor cannot retain the temporary selection.
- An initial audit clicked before UI geometry was ready. Its diagnostic assertion remains in the Console history; the subsequent full pass captures the initialized Canvas before input and passed. No unresolved feature error was found. Existing project deprecation warnings remain. A standalone player build was not produced.

The editor menu has focused validation commands under `Tools/Intro Comic`. The scene builder refuses to overwrite an existing comic scene. Edit the authored scene normally for follow-up changes.

Image edits used the built-in imagegen tool. Prompts are saved in `Docs/Art/IntroComic/textless-generation-prompts.txt`.
