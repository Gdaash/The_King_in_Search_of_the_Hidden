# Game audio coverage

## Music

- `MainMenu`: `MainMenu_BirthOfMagic`
- `Base`: `Base_ViridiNemus`
- `World`: `World_AmbientDrums`
- Legacy audio files, music services and volume trackers were removed. Only the current audio library supplies game audio.

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

## Dialogues and starting quests
- Dialogue text reveals whole words at 0.12 second intervals, using unscaled time. First click completes the text; the next advances or accepts. Completed and interrupted typing stops new word sounds.
- Dialogue opening, advancing, closing; quest acceptance, completion, reward, panel flight and building unlock star arrival have separate cues.
- Recruitment, disarming, first refugee arrival and refugee admission have success cues. Construction and crafting retain existing effects.
- Word tick, quest chime and flight whoosh come from E:/GDrive/Egg or chicken games/музыка/Kingsgrave/Sounds/UI. Exact sources and the word excerpt processing are recorded in Assets/Audio/SFX/QuestAudioSources.json.
- Tune clips in Assets/Resources/GameAudioLibrary.asset. Tune Word Interval and Word Sound Volume in the QuestDialogueView component on Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab. All effects follow the existing effects volume setting.
- Play Mode validation: QuestDialogueValidation (typing, pause, skip, interrupt, blocking, acceptance, plain conversations); StartingQuestChainValidation (all 3 quests, construction, crafting, recruitment, refunds, rewards, refugees and audio dispatch); FirstPortalRunValidation (generation, alarm wave, escape presentation and second visit).
