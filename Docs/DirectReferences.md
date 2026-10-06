# Direct project references

Runtime buttons and building artwork use Inspector references rather than
GameObject.Find or Transform.Find. BaseUIController stores its buttons explicitly;
WorldBuildingButton stores its artwork root, SpriteRenderers and label. Deployment
and escape controllers reference the World portal through scene overrides.

Assets/Resources/Project References.asset holds references to resources, portal
locations, audio, building upgrades, CRT settings and the overlay material. It is
registered in Player Settings / Preloaded Assets, so runtime code does not load
these assets by filename. Keep it registered if you move or rename it.

ResourceType.Id and EnemyIdentity.Id are permanent save identifiers. Existing
asset and prefab names were captured once during migration, retaining current
save and localization keys. Renaming an asset does not alter its ID. Do not change
these IDs when renaming resources or enemies; assign a unique ID to new content.
Resource icons, portraits and unit prefabs remain direct Unity links.

Editor migration/setup tools may use old names to locate the original authoring
layout once. Those strings are not runtime references. Localization, save and
upgrade IDs remain strings intentionally; they are independent of object names.

Validation: runtime code compiled; all three scenes checked in Unity with their
GameObjects temporarily renamed; 46 UI/portal links retained identity. Resource
lookup retains identity after renaming resource assets. Scene copies were not saved
by validation. The user's open unsaved scene was not replaced by migration.
