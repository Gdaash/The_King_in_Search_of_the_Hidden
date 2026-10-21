# Unit presentation and blacksmith layout

- ResourceWorker caches its SpriteRenderer and flips only the sprite from horizontal displacement. Original artwork faces left. Stationary and paused workers keep their last facing.
- HealthBar applies x1.1 width and y1.7 height to the initial local scale when attached below EnemyMovement or ResourceWorker. Existing billboard, parent-flip compensation, fading and pool reset remain in use.
- Blacksmith popup uses three consistent equipment rows and columns for stock, crafting and upgrades. Original crafting and cart button callbacks are preserved. Upgrade level squares use the same UpgradeLevelPips component as the laboratory. Stat labels distinguish damage from cart speed, localized in RU/EN.
- BlacksmithLayoutSetup.Apply authors the standalone popup and Base Location Variant. EquipmentUpgradeSetup.Configure applies this layout after configuring original controls.

Validation: UnitPresentationValidation.Run in the isolated Play Mode fixture. Both worker directions; Octopus and RedArcher bar multipliers; original crafting actions; all 25 equipment upgrade purchases, prices and caps; RU/EN labels; resource icon sizing. Captures: Temp/EquipmentQA/blacksmith-ru.png and blacksmith-en.png.
