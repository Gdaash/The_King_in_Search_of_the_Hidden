# Game audio coverage

## Music

- `MainMenu`: `MainMenu_BirthOfMagic`
- `Base`: `Base_ViridiNemus`
- `World`: `World_AmbientDrums`
- Legacy `BackgroundMusic` sources are stopped so tracks do not overlap.

## Automatically covered events

- Every current and runtime-created uGUI button: click, open, or confirm cue by action name.
- Global resource increases and decreases: gain and spend cues.
- Production timer completion: generic, wood, or stone cue based on producer.
- Building construction: construction completion cue.
- Hex opening: magic completion cue.
- Portal escape, military summon, and military recall: portal cue.
- Danger threshold: bell cue.
- Monster creation: spawn cue.
- Melee, ranged, and PortalTower attacks: sword, bow, and magic cues.
- Health damage and death: hit and death cues.
- Human, porter, friendly warrior, and enemy movement: alternating positional footsteps.

Runtime discovery repeats every 0.75 seconds, so generated map objects and spawned units are covered.

## Tuning

- Clip assignments: `Assets/Resources/GameAudioLibrary.asset`
- Music, UI, and world base volume: `GameAudioController` serialized defaults.
- Player settings: `GameSettingsService.Music` and `GameSettingsService.Effects`.
- Positional effects use a 14-source pool with linear rolloff from 3 to 22 world units.

## Import policy

- Music: stereo, Vorbis, Streaming, background loading.
- Effects: mono, PCM, Decompress On Load, optimized sample rate.
