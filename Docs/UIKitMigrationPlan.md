# UI Kit migration plan

## Prepared source set

The UI PSD contains nine interface compositions: Battle HUD, Dialog, Evolution popup, compact Evolution popup, Field popup, two Tavern variants, Monster Battle popup and Tower popup. The complete layer export is stored in `Assets/Sprites/UI Kit`; reusable uGUI prefabs are stored in `Assets/Prefabs/UI Kit`.

The migration must preserve existing scripts, localization keys, button events and save logic. Only presentation objects and layout are replaced.

## Shared foundation

1. Replace popup root backgrounds with the appropriate `UIKit Panel` prefab and keep them as sliced Images.
2. Replace action controls with `UIKit Button` variants. Keep existing Button components and event bindings.
3. Use `UIKit Card Dark` for lists, resource rows and statistic blocks; use speech bubble variants for descriptions and tooltips.
4. Use the matching decorative header as a separate nonstretching image, then place localized TMP text over it.
5. Keep resource icons driven by `ResourceType`; display them at two Canvas units per source pixel.
6. Add content size fitters or scroll views only where runtime content can exceed the PSD reference dimensions.

## Current interface mapping

| Current interface | UI Kit target | Notes |
|---|---|---|
| ResourcesUI, Alarm Bar, Game Speed Controls, Military Controls, Base Military Overview | Battle HUD reference + compact cards/buttons | Preserve the existing shared Base/World resource prefab and its dynamic resource list. |
| Settings Popup, CheatResourcePopup | Panel Large/Compact + Primary buttons | Settings remains one shared prefab across scenes. Cheat rows remain generated from global resource definitions. |
| Tooltip, Building Construction Tooltip, Portal Location Tooltip | Speech Bubble Dark/Light | Keep automatic sizing and localization; resource costs remain icon plus amount. |
| Laboratory Popup, Laboratory Skill Tree | Evolution popup + Header Evolution | Preserve pan, zoom, keyboard movement, dependency arrows and ScriptableObject bindings. |
| Warehouse Popup, Blacksmith Popup, Fort Popup, Archery Range Popup | Tower/Tavern panels + action buttons/cards | Reuse one production popup shell and inject each building's recipes and localization. |
| Housing Popup, Refugees Popup, Square Popup | Tavern/Field panels | Use cards for population, food and day result rows. |
| Global Map, portal location entries | Tower panel + cards/buttons | Portal availability, cost icons, danger skulls and location tooltip stay data driven. |
| Next Day Confirmation | Dialog panel + Header Dialog | Preserve hunger forecast and crown change rows. |
| Escape Statistics Popup and defeat variant | Dialog/Monster Battle panel | Keep one statistics prefab and conditionally show the red portal destroyed message. |
| Danger Level Notification | Compact panel/card | Keep skull progression and three second notification animation. |
| Main Menu save selection and Settings | Panel Large + Primary buttons | Logo remains separate; save slot contents remain dynamic. |

## Rollout order

1. **Shared primitives:** buttons, tooltip shell, popup shell, close button, cards and resource rows.
2. **Persistent HUD:** ResourcesUI, alarm bar, speed controls and military panels on Base and World.
3. **Base popups:** warehouse and production buildings, housing/refugees/square, laboratory and next day flow.
4. **World popups:** escape statistics, defeat state, portal map and danger notification.
5. **Main menu:** settings and save slots.
6. **Final pass:** test Russian and English text, all supported aspect ratios, disabled/hover/pressed states, long resource lists and every popup open/close path.

## Acceptance checks

- Every background Image is Sliced and has nonzero borders.
- Every imported PSD PNG is 32 PPU, Point filtered, uncompressed and has mip maps disabled.
- Nonstretching art is displayed at exactly two Canvas units per source pixel.
- No text from the PSD is used as runtime text.
- All resource amounts use ResourceType icons and fit without overlap.
- Existing button callbacks, localization updates, pause behavior and save data continue to work.
- Base, World and MainMenu compile and run without Console errors after each rollout stage.
