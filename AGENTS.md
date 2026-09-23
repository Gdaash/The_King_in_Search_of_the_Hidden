# UI art scale

For sprites displayed in a Unity Canvas, set the RectTransform width and height to twice the sprite's pixel width and height (24×24 px → 48×48 Canvas units). Stretchable 9-slice backgrounds are the exception: size them to their container.

Import every newly created sprite with Pixels Per Unit = 32. Keep pixel art sharp with Point filtering and no texture compression.

Whenever a game resource is shown in UI, use its ResourceType icon next to the amount. Do not use the resource name as a substitute for its icon. Size resource icons by the Canvas rule above and verify that icons, amounts, labels, and buttons fit without overlap.
