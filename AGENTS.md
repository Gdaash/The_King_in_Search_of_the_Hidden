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

# Project handbook acceptance workflow

- Keep `Docs/ProjectManual/index.html` current with accepted project changes. Update the handbook's source chapters and generator data as needed, then regenerate the HTML rather than editing only the generated file.
- Treat a sequence of fixes and iterations for one mechanic as one change cycle. Do not document experimental intermediate behavior as accepted behavior.
- When the user explicitly accepts the changes and requests documentation, update the handbook to describe the final implementation and remove superseded information.
- Before starting a different mechanic or unrelated change cycle, if the previous cycle has not yet been accepted for documentation, ask the user whether to add those previous changes to the handbook. Do not assume that switching topics is acceptance.
- The default intention is to document all final changes that will be retained, but the user decides whether the current iteration is ready. If documentation is deferred, keep track of the pending cycle and revisit it at the next transition.
- An explicit instruction to add accepted changes to the handbook is sufficient authorization; do not ask again for the same cycle.
- After an accepted handbook update, also publish the updated HTML to the existing public Sites project `appgprj_6ae5d4922e1881918d3c3472efa86888` using `Docs/ProjectManual/hosting` and the Sites workflow. Preserve the public URL and audience. See `Docs/ProjectManual/HOSTING.txt` for deployment details. Do not create a new Site for each update.

# Pending gameplay validation reminders

- At the start of a new task, read `Docs/AI/VerificationQueue.md` if it exists. On the first task of a new calendar day (Europe/Moscow), remind the user about listed uncompleted gameplay checks and record the reminder date. Do not repeat the reminder in each message on the same day.
- Documentation acceptance does not mean runtime validation. Keep unverified features explicitly marked in the handbook until actual gameplay checks pass; do not open Unity just to clear the queue without authorization.
- UI and feedback conventions are collected in `Docs/ProjectRules.md`; the dated audit and proposed refactors are in `Docs/UIConsistencyAudit.md`. Audit suggestions are not authorization to modify gameplay or all UI at once.


# Popup and construction UI

- After changing UI layout, render the affected window in Unity and inspect a screenshot or Game View at a representative resolution. Include the state changed by the task, such as a locked card. Compilation alone is not a visual check.
- Anchor the shared close button to the top-right corner of the visible window frame. Preserve the standard inner inset when resizing the window. Check the resulting position in Play Mode.
- Equal-level popup columns share width, background, padding and heading style. Alternate all resource rows by index.
- Defensive construction uses the building circular progress prefab with the same geometry and lower-hex offset. Recolor the fill blue.
- Every resource-spend button has one row: action on the left, ResourceType icons and amounts on the right, all inside the same animated visual. Prices scale together with the caption on hover.
- Do not add thin separators below popup headings or yellow left/hover accent strips. Use the shared cornered 9-slice panel, consistent gaps and scrollable lists.
- Regular popup text is at least 22 Canvas units; compact unit stat tables are the exception. Keep construction actions aligned in one column and validate all eight languages.
