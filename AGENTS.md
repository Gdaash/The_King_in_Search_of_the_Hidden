# UI art scale

For sprites displayed in a Unity Canvas, set the RectTransform width and height to twice the sprite's pixel width and height (24×24 px → 48×48 Canvas units). Stretchable 9-slice backgrounds are the exception: size them to their container.

Import every newly created sprite with Pixels Per Unit = 32. Keep pixel art sharp with Point filtering and no texture compression.

Whenever a game resource is shown in UI, use its ResourceType icon next to the amount. Do not use the resource name as a substitute for its icon. Size resource icons by the Canvas rule above and verify that icons, amounts, labels, and buttons fit without overlap.

# Text typography

Apply these rules to all authored text: game UI, dialogue, localization, marketing copy, video titles and subtitles, documents, and chat responses for this project.

- Use the short dash (en dash, U+2013) for punctuation. Never use the long dash (em dash, U+2014).
- Use straight double quotation marks (U+0022) on both sides of quoted text. Do not use angle quotation marks or curly/opening/closing double quotation marks.
- Keep source strings, fallback strings, localization tables, Google Sheets exports, and video captions consistent when changing typography.
- Preserve valid escaping in C#, YAML, JSON, and HTML. Do not change syntax delimiters, apostrophes, or technical hyphens/minus signs.
