# Stage `feel` - SFX and VFX

Plan section: Feel (the event table: event → cue → clip, event → effect → prefab, haptic).

## SFX

- Every gameplay event in the table gets an `AudioCue` (appended) and a row in `AudioSettings.asset`
  (`cueEntries`). Clips come from the game's audio folders; match by name first, then by length and
  role (short tick for taps, rising for wins).
- No clip for a cue: leave the row empty (an empty cue plays nothing, by design) and list it in the
  report. Never generate or download audio.
- Music: `musicClip` when a music file exists. WebGL re-encodes audio: import as mono, quality 0.4.
- Haptics are a no-op on WebGL; still call `IHapticService` where the framework does.

## VFX

- One prefab per effect under `<Root>/Prefabs/VFX/`, a `ParticleSystem` using the game's own sprites
  (a texture-sheet module for sprite shapes), sized for the portrait camera, `TimedDespawner` for its
  lifetime, declared in `PoolCatalog.asset`, spawned through `IObjectPoolService`.
- Effects that play over a popup use unscaled time.
- Keep the count per burst low (WebGL fill rate): 30 particles or fewer unless the plan says otherwise.

## Done when

Every row of the plan's event table is wired or listed as missing; Play mode shows each effect once
(capture for the report); gate green.
