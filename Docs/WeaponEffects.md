# Weapon visual effects

- Electric Orb uses weapon_ElectricOrb at 70% opacity. Flight rotation 85 degrees/sec, death rotation 425 degrees/sec. At impact: scale 1 to 1.3 in 0.05 sec, then collapse over 0.15 sec.
- Fire Shot uses an 8x22 central flame slice of weapon_FireShot. Bolts use weapon_Bolts. Opacity 78%.
- Other EnemyProjectile prefabs retain their artwork with 82% opacity. Small projectiles expand to 1.2 in 0.05 sec and fade over 0.2 sec.
- Colliders disable immediately after impact. Lifetime expiration also uses the death animation.
- Frost Seal emits six mesh snowflakes/sec, max 16 per seal, over its hex footprint. Meshes and material are shared by the controller.
- Spawn telegraph uses 72 independent tails, twice the previous scale, with randomized rim positions, angular speeds and lifetimes (1.7 to 4.1 sec) and one shared 64px imported charcoal-plum pixel streak sprite with an airy semitransparent smoke fringe; no per-marker materials or LineRenderers.

Validation: WeaponEffectsValidation.Begin() runs in an isolated temporary Play Mode scene. PASS: icon references, opacity, expansion/fade/collapse curves, disabled colliders, eventual destruction, snow emission and independent inward spiral tails. Preview: Temp/WeaponFXQA/preview.png.




