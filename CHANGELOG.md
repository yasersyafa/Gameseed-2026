# Changelog

All notable changes on the `yaser-backup` branch since divergence from `main`
(`45cb97e`). Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Greyboxing level scene (`2399401`).
- Basic player movement and dash (`c71a7e1`, `b02bad0`).
- `PlayerInputManager` and local multiplayer wiring (`5045fb3`, `4e7c45d`).
- Boomerang throw mechanic with returning flight + catch (`20300d8`, `0cd0ef3`).
- Boomerang melee + parry system (`b90791c`).
- Round system with countdown, scoring, and game-over flow (`e3b5c3f`).
- Round transition: iris wipe between rounds driven by `IrisConfigSO` (`d70049d`,
  `da5a000`).
- Player feel pass: anticipation crouch, hit flash, juice, charge throw, dash
  trail, smoke effect, death splat, aim indicator (`78bc2ed`).
- Custom URP shaders for outline and X-ray silhouette (`ea7ba5f`).
- Boomerang trail renderer with per-player tint (`619aa59`).
- Death particle prefab on player elimination (`1121b89`).
- UI Toolkit HUD scaffold (`193151a`).
- `YieldCollection` cached `WaitForSeconds` utility (`41a0506`).
- VContainer-based DI: `RootLifetimeScope` registers persistent services and
  injects runtime-spawned players (`e772253`).
- Power-up framework with mutex groups (Fire, Ice, Shield, Multi, Explosive).
- `PrimitivePool` for runtime-spawned cube/sphere VFX primitives (`ed6a656`).
- `ShaderHelper` and `FontHelper` utilities replacing scattered
  `Shader.Find` / TMP font lookups (`ed6a656`).

### Changed

- Refactored `PlayerController` to a finite state machine (`Idle`, `Charging`,
  `Attacking`, `Dashing`, `Frozen`, `Eliminated`); states own input handling
  (`cc343e1`, `0d3ad70`).
- Cross-system communication moved to `GameEvents` static event bus
  (observer pattern) (`1ad1cde`).
- Boomerang feel: spin scaling by speed, lateral curve on return, whoosh loop
  pitched by velocity (`aa51eab`).
- Camera shake on player hit removed; replaced by Cinemachine impulse source
  with curve preset and 0.15s duration (`8cdb2d4`, `71d0fa9`).
- VFX (smoke, dash trail, death chunks) now pool primitives instead of
  `Instantiate` + `Destroy` per spawn (`ed6a656`).
- `Boomerang.SetTrailColor` caches `Gradient` and key arrays — no per-call
  allocation on charge throws (`ed6a656`).
- `RumbleManager` injects `SettingsManager` and `GameManager` via VContainer;
  caches `Gamepad` lookup per player (`ed6a656`).
- `GameEvents.ClearAll` now hooked to `SceneManager.sceneUnloaded` to prevent
  stale subscribers across scene reloads (`ed6a656`).
- Renamed `LiveSystem` → `LivesSystem` to match DI registration (`ed6a656`).
- `RoundManager` drives `IrisTransition` via coroutines (`CoCloseAndHold`,
  `CoOpen`); round-event-driven iris subscriptions removed except `OnGameOver`
  (`3857b8c`).
- Pickup anchors and Fire power-up tint tuned (`71d0fa9`).

### Fixed

- Player throw input release detection: poll `InputAction.IsPressed` each
  frame because `SendMessages` mode does not fire `OnThrow` on Canceled phase
  (`ec8337d`).
- Boomerang melee hit on player now triggers the death pipeline (`a57c094`).
- Round handoff race: gameplay used to resume ~1.5s before the iris finished
  opening. `RoundEndRoutine` now awaits iris close+hold, and
  `StartRoundRoutine` awaits iris open before the countdown (`3857b8c`).
- Melee hit detection: sword had no Rigidbody and was a compound child
  collider under the player's Rigidbody, so `OnCollisionEnter` never fired
  on the sword GameObject. Replaced with `Physics.OverlapBoxNonAlloc` polled
  in `FixedUpdate` while the swing is active; collider resolved via
  `GetComponentInChildren` since it lives on a child mesh GO (`e06450c`).
- Phantom movement after melee swing: `Attacking`, `Dashing`, and `Frozen`
  states had empty `HandleMove`, so stale `_moveInput` survived the swing and
  slid the player on return to `Idle`. `OnMove` now stores input before
  delegating to state (`e06450c`).

### Performance

- VFX hot paths no longer allocate per spawn (pool reuse, cached materials,
  cached shader handles, `MaterialPropertyBlock` for color variants)
  (`ed6a656`).
- `RumbleManager` skips per-rumble device scan via cached `Gamepad`
  reference (`ed6a656`).
