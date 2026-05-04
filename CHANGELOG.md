# Changelog

All notable changes on the `yaser-backup` branch since divergence from `main`
(`45cb97e`). Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Newest first.

## 2026-05-05

### Fixed
- Melee hit detection: sword has no Rigidbody and was a compound child
  collider under the player's Rigidbody, so `OnCollisionEnter` never fired
  on the sword GO. Replaced with `Physics.OverlapBoxNonAlloc` polled in
  `FixedUpdate` while the swing is active. Collider resolved via
  `GetComponentInChildren` since it lives on a child mesh GO (`e06450c`).
- Phantom movement after melee swing: `Attacking`, `Dashing`, and `Frozen`
  states had empty `HandleMove`, so stale `_moveInput` survived the swing
  and slid the player on return to `Idle`. `OnMove` now stores input
  before delegating to state (`e06450c`).
- Round handoff race: gameplay used to resume ~1.5s before the iris
  finished opening. `RoundEndRoutine` now awaits iris close+hold and
  `StartRoundRoutine` awaits iris open before the countdown (`3857b8c`).

### Changed
- `RoundManager` drives `IrisTransition` via coroutines (`CoCloseAndHold`,
  `CoOpen`); round-event-driven iris subscriptions removed except
  `OnGameOver` (`3857b8c`).
- VFX (smoke, dash trail, death chunks) pool primitives instead of
  `Instantiate` + `Destroy` per spawn (`ed6a656`).
- `Boomerang.SetTrailColor` caches `Gradient` and key arrays — no
  per-call alloc on charge throws (`ed6a656`).
- `RumbleManager` injects `SettingsManager` and `GameManager` via
  VContainer; caches `Gamepad` lookup per player (`ed6a656`).
- `GameEvents.ClearAll` hooked to `SceneManager.sceneUnloaded` to
  prevent stale subscribers across scene reloads (`ed6a656`).
- Renamed `LiveSystem` → `LivesSystem` to match DI registration
  (`ed6a656`).
- Pickup anchors and Fire power-up tint tuned; hit impulse switched
  to curve preset with 0.15s duration (`71d0fa9`).

### Added
- `PrimitivePool` for runtime-spawned cube/sphere VFX primitives
  (`ed6a656`).
- `ShaderHelper` and `FontHelper` utilities replacing scattered
  `Shader.Find` / TMP font lookups (`ed6a656`).

## 2026-05-01

### Added
- VContainer-based DI: `RootLifetimeScope` registers persistent
  services and injects runtime-spawned players (`e772253`).
- Round transition: iris wipe between rounds driven by `IrisConfigSO`
  (`d70049d`, `da5a000`).
- Player feel pass: anticipation crouch, hit flash, juice, charge
  throw, dash trail, smoke effect, death splat, aim indicator
  (`78bc2ed`).

### Changed
- Backup snapshot of in-progress feel work (`a1f0177`).

## 2026-04-25

### Added
- Boomerang melee + parry system (`b90791c`).

### Changed
- Cross-system communication moved to `GameEvents` static event bus
  (observer pattern) (`1ad1cde`).

### Fixed
- Boomerang melee hit on player now triggers the death pipeline
  (`a57c094`).

## 2026-04-23

### Added
- Round system with countdown, scoring, and game-over flow
  (`e3b5c3f`).

### Changed
- Refactored `PlayerController` to a finite state machine (`Idle`,
  `Charging`, `Attacking`, `Dashing`, `Frozen`, `Eliminated`); states
  own input handling (`cc343e1`, `0d3ad70`).
- Camera shake on player hit removed (replaced later by Cinemachine
  impulse source) (`8cdb2d4`).

## 2026-04-22

### Changed
- Boomerang feel: spin scaling by speed, lateral curve on return,
  whoosh loop pitched by velocity (`aa51eab`).

## 2026-04-21

### Added
- Custom URP shaders for outline and X-ray silhouette (`ea7ba5f`).
- Boomerang trail renderer with per-player tint (`619aa59`).
- Death particle prefab on player elimination (`1121b89`).
- `YieldCollection` cached `WaitForSeconds` utility (`41a0506`).
- UI Toolkit HUD scaffold (`193151a`).

## 2026-04-20

### Added
- Local multiplayer wiring (`4e7c45d`).

## 2026-04-19

### Added
- Boomerang flight + return + catch mechanics (`0cd0ef3`).

### Fixed
- Player throw input release detection: poll `InputAction.IsPressed`
  each frame because `SendMessages` mode does not fire `OnThrow` on
  Canceled phase (`ec8337d`).

## 2026-04-16

### Added
- Boomerang throw mechanic skeleton (`20300d8`).
- `PlayerInputManager` (`5045fb3`).

## 2026-04-15

### Added
- Greyboxing level scene (`2399401`).
- Basic player movement (`c71a7e1`).
- Player dash (`b02bad0`).
