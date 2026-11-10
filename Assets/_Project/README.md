# Platformer Template

A 2D platformer starter for Unity 6 (URP 2D, Input System). Tight, Celeste-style movement with double jump, wall slide / wall jump, dash, melee and ranged attacks, and three ready-made ways to play: **Level**, **Metroidvania** and **Endless**. Everything uses generated placeholder art and procedural placeholder sound so you can swap in your own.

## First run (3 steps)

1. Wait for Unity to compile. If the Console shows errors, copy them to me and I will fix them. This code was written without a Unity compiler.
2. **Tools > Platformer Template > 1. Setup Project (layers)** adds the physics layers the template needs (Player, Enemy, Ground, OneWay, Hazard, PlayerHitbox, EnemyHitbox, Pickup, Trigger) and the sorting layers.
3. **Tools > Platformer Template > 2. Build Demo Content** generates sprites, tiles, data assets, prefabs and these scenes (also added to Build Settings):

| Scene | What it is |
|---|---|
| `MainMenu` | Mode select, level select, continue / new game, settings |
| `Gym` | Test room with every ability on: gaps, one-way platforms, wall shaft, spikes, moving platform, enemies. Use it to tune the feel |
| `Level_1` to `Level_3` | Three short levels, abilities granted per level, level 3 ends with a boss that unlocks the exit |
| `Metroidvania_Demo` | Six linked rooms, three ability pickups, a dash gate, wall-jump shaft to a secret upper room, saves at checkpoints |
| `Endless_Demo` | Procedural chunk runner with score and best score |

Open `MainMenu` and press Play. Any gameplay scene can also be played directly; the persistent systems (`[Platformer Systems]`: game manager, audio, UI, debug overlay) create themselves.

Re-running step 3 regenerates prefabs, scenes and layouts and **overwrites edits to them**. Data assets (`PlayerTuning`, `EnemyData`, `LevelDatabase`) are kept.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | A D / arrows | Left stick / D-pad |
| Jump (hold for height) | Space | South |
| Drop through one-way | Down + Jump | Down + South |
| Dash | Left Shift | West |
| Melee (aim up/down for pogo) | J | East |
| Ranged | K | Right trigger |
| Pause | Esc | Start |
| Debug overlay | F3 | |

## Tuning the feel

Select `Assets/_Project/Data/PlayerTuning.asset`. Jump is defined by **height and time to apex**, not by raw force, so `jumpHeight`, `tapJumpHeight`, `timeToApex` and `fallGravityMultiplier` are all you need for the arc. Coyote time, jump buffer, corner correction, apex hang and dash are in the same asset. The Gym scene plus the F3 overlay (state, velocity, coyote and buffer timers) is the place to iterate. Values live only in this asset, never in scenes or prefabs.

## Architecture in one page

```
Core/      GameManager (state, run stats, checkpoints, scene loading), GameEvents (static hub),
           SaveSystem (JSON slots), GameSettings, ScriptableObject data (PlayerTuning, EnemyData, LevelDatabase)
Player/    KinematicBody2D (box-cast mover, no dynamic forces), PlayerController (state machine),
           PlayerInputReader (rebindable actions), PlayerAbilities (flags), PlayerJuice (squash, dust, trails)
Combat/    Health, DamageDealer, Projectile, HitFlash
Enemies/   Enemy base + Patrol, Flyer, Shooter, Charger, Boss (each implements Think())
World/     Room, Checkpoint, LevelExit, Collectible, AbilityPickup, AbilityGate, MovingPlatform,
           Spring, BreakableBlock, EndlessChunk, PlayerSpawn
Modes/     GameMode base + LevelMode, MetroidvaniaMode, EndlessMode, GymMode
Cam/       CameraFollow (dead zone, look-ahead, room bounds, shake)
UI/        Code-built menus and HUD (UIFactory, UIManager, MainMenuController), DebugOverlay
Editor/    Setup, demo content builder, ASCII level layouts, scene validator
```

The rule that keeps modes swappable: **player, combat and world code never ask which mode is running.** They call into `GameMode` (death, checkpoint, level exit, ability unlocked) and the active mode decides what happens.

## Adding things

- **New ability**: add a flag to `AbilityFlags` (Core/Types.cs), gate the behaviour in `PlayerController` with `Has(AbilityFlags.X)`, add a pickup using `AbilityPickup`.
- **New enemy**: subclass `Enemy`, implement `Think(float dt)`, create an `EnemyData` asset, copy an existing enemy prefab and swap the script. Use the helpers `ApplyGravity`, `MoveBody`, `PlayerInRange`, `HasLineOfSight`, `Fire`.
- **New level**: duplicate `Level_1`, paint the Ground tilemap (the Ground layer plus a composite collider is already set up), drop prefabs in, add the scene to Build Settings and to the `LevelDatabase`. Run **Validate Open Scene** to catch layer and id mistakes.
- **New metroidvania room**: add a `Room` trigger (unique `roomId`) around it. The camera clamps to the room bounds.
- **New endless chunk**: make a prefab with an `EndlessChunk` on the root (origin bottom-left). Ground must start and end at the same height as every other chunk so any chunk can follow any other. Add it to `EndlessMode.chunkPrefabs`.
- **Your art**: replace the PNGs in `Art/` (16 px per unit, point filtered) or assign sprites on the prefabs. Sound: create a `SoundBank` asset (Create > Platformer Template > Sound Bank), name it `SoundBank` and put it in a `Resources` folder; any missing clip falls back to the procedural placeholder.
- **Demo layouts** are plain text in `Editor/LevelLayouts.cs`. The legend is in that file's header comment.

## Things to know

- **Movement collisions** use box casts with a small skin gap and resolve X then Y. One-way platforms and moving platforms are handled in `KinematicBody2D` and `MovingPlatform` (execution order -50 so riders see the platform's delta).
- **Save data** is JSON in `Application.persistentDataPath`: metroidvania slot 0, level slot 1. The endless best score is in PlayerPrefs (`endless_best`).
- **Death rules**: level mode reloads the scene and respawns at the last checkpoint; metroidvania respawns at the last checkpoint with abilities kept; endless ends the run.
- **Static events** (`GameEvents`) are reset at startup so Enter Play Mode without domain reload works.
- The project must use **Active Input Handling = Input System Package (New)**. It already does.

## Deviations from the GDD, and what is not done

- No Cinemachine; `CameraFollow` covers dead zone, look-ahead, room bounds and shake.
- No in-game rebinding screen yet (the rebinding API exists in `PlayerInputReader`).
- No boss health bar, no minimap or fast travel, English only.
- Placeholder audio is generated at runtime, not imported.
- Not compiled or play-tested by the author: treat the first Console as part of setup.

## Checklist when you start a real game from this template

1. Rename the namespace and asmdefs (`PlatformerTemplate.*`), product name and company in Project Settings.
2. Replace placeholder art and sound; set camera `pixelSnap` if you go pixel-perfect.
3. Decide the mode(s) you ship; delete the other modes and their scenes.
4. Re-tune `PlayerTuning` to taste and freeze it; then build the regression course in the Gym scene.
5. Remove or disable `DebugOverlay` for release (it is already editor / development builds only).
