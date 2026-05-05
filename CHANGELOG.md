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
- Build settings: `GameScene.unity` was missing — `SampleScene` was
  shipping by default. `EditorBuildSettings.asset` now lists
  `_Sandbox/GameScene.unity` as build index 0; `SampleScene` demoted.
- Countdown HUD intermittently skipped digits (`3, 1, GO!`). Root cause:
  schedule + `experimental.animation` race — fade-out (1→0 over 400ms,
  starting at +550ms) had a 50ms safety margin before the next tick;
  editor stutter pushed the running fade past the 1000ms boundary so the
  next tick's `style.opacity = 1` was overridden on the animation's next
  update. Rewrote `HandleCountdown` as a coroutine on
  `Time.unscaledDeltaTime` that restarts on every tick, force-writing
  opacity=1 on the first frame.
- Pause button click never registered: scene was missing `EventSystem`
  with `InputSystemUIInputModule` (project uses new Input System), and
  UI Toolkit `PerformPick` early-outs on `pickingMode = Ignore`. Root +
  `top-right` zones are now `Position`; `HUDBootstrap.EnsureEventSystem`
  spawns the module if absent and replaces a legacy `BaseInputModule`.
- P# tag font missing: `ApplyFonts` queried only labels existing at
  Awake; cloned `PlayerRowTemplate` labels were created later and got no
  font. `EnsurePlayerRow` now sets `unityFontDefinition` on the tag
  label explicitly after instantiation.
- `Boomerang.OnCollisionEnter` owner-catch branch now sets `_hasHit`
  before deferred `Destroy`. A second collision in the same frame
  could re-enter the catch path and fire `CatchBoomerang` twice.
- `Boomerang.Reflect` refuses ownership swap to an eliminated
  parrier and `StartReturning()`s instead. Previously a parried-into
  -eliminated player meant the boomerang chased the off-map hold
  position forever.
- Juice listeners silently stopped firing after the first scene
  reload. `JuiceBootstrap` ran at `RuntimeInitializeOnLoadMethod`
  (once per app launch), spawned a DDOL `[Juice]` GO, and subscribed
  to `GameEvents` exactly once. `RootLifetimeScope` nulls every
  delegate via `GameEvents.ClearAll` on `sceneUnloaded`; the listener
  GO never disabled, so its `OnEnable` never re-ran to re-subscribe.
  Migrated to scene-baked `[Juice]` (see Added) so subscribe/unsubscribe
  follows scene lifecycle.
- Game-over `WINNER!` card + `RESTART` button were invisible — iris
  Canvas (`sortingOrder = 200`) painted over the HUD's UIDocument
  (`sortingOrder = 100`), and the panel was being shown
  synchronously with `OnGameOver` *before* the iris finished its
  close animation. Two-part fix:
  1. Render order — split game-over into its own UIDocument /
     `GameOverPanelSettings.asset` at `sortingOrder = 300`. HUD
     stays at 100 so it sits *behind* the iris wipe during normal
     round transitions.
  2. Timing — `IrisTransition.HandleGameOver` now drives a
     coroutine that yields the close tween then raises a new
     `GameEvents.OnIrisClosed` event. `GameOverView.HandleGameOver`
     stashes the winner index; `GameOverView.HandleIrisClosed`
     reveals the panel only after the iris is fully closed.
- Round label lagged the countdown — `RaiseRoundStarted` fired
  *after* the 3-2-1-GO! sequence, so the HUD `ROUND N` only
  updated once gameplay armed. Now raised before the countdown
  loop. `PickupSpawner.SpawnLoop` already has its own
  `initialDelay` so starting earlier doesn't drop a pickup
  mid-countdown.

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
- HUD migrated from runtime-built uGUI/UIElements to UXML/USS
  asset-driven design system. Legacy `GameHUDOverlay` (uGUI) and
  the interim `GameHUDToolkit` (runtime VisualElements) deleted.
- `LivesSystem.eliminatedHoldPos` is now a serialized `Vector3` —
  was hardcoded `(0, -100, 0)` and would trap any arena whose
  floor extends below 0.
- `StatusEffectController` reuses a cached scratch `List<>` for
  `ClearAll` and the per-frame expiry sweep instead of allocating
  per call.
- Trail / line materials in `Boomerang`, `DashTrail`, and
  `AimIndicator` now share `ShaderHelper.SharedUnlit()` via
  `sharedMaterial` instead of `new Material(ShaderHelper.GetUnlit())`
  per spawn. Per-throw VRAM leak gone; color is driven by gradients.
- HUD authoring is scene-baked. `[GameHUD]` GameObject lives in the
  active scene with a serialized `UIDocument` (referencing
  `HUDView.uxml` + `HUDPanelSettings.asset`). `GameHUDView` reads
  `playerRowTemplate` + `hudStyleSheet` `SerializeField` refs instead
  of `Resources.Load` and no longer creates `UIDocument` or
  `PanelSettings` at runtime.
- DI: `RootLifetimeScope` registers `GameHUDView` via
  `RegisterComponentInHierarchy`, so its `[Inject] Construct(...)`
  fires reliably. `GameHUDView` dropped its `FindFirstObjectByType`
  fallbacks — DI is now the only path.
- Global juice listeners (`PostFxJuice`, `HitImpactVfx`,
  `WinFlourish`) live on a scene-baked `[Juice]` GameObject built by
  `Tools > Boomerang Fu > Setup/Juice (Scene Listeners)`. All
  `SerializeField` knobs (vignette / chromatic intensities + durations,
  burst counts, confetti palette) are now inspector-editable.
- Renamed `Boomerang.ApplyStatsFromSO` and
  `PlayerController.ApplyStatsFromSO` to `ApplyConfigFromSO`,
  matching the convention used by `RoundManager`, `LivesSystem`,
  and `IrisTransition`. One name, one pattern.
- `CinemachineCameraManager` subscribes to `OnGameOver` and runs a
  cinematic stinger — deeper FOV punch (-22° / 1.2s) plus a
  larger shake. Target group already drops eliminated players, so
  the lens auto-zooms toward the lone surviving winner.
- `LivesSystem.ApplyConfigFromSO` honours
  `RoundConfigSO.suddenDeath` — when true, `livesPerPlayer` is
  forced to 1 regardless of the SO's explicit lives count.
- HUD `sortingOrder` reverted to 100 (behind iris). New
  `[GameOverOverlay]` GameObject hosts a separate UIDocument at
  `sortingOrder = 300` (above iris). HUD elements (player rows,
  round card, ELIM popups, pause modal) stay hidden behind the
  iris during round transitions; only the winner card + RESTART
  button paint on top of the closed iris.
- `GameHUDView` lost its game-over reveal responsibilities —
  `_gameOverPanel` / `_gameOverContent` / `_gameOverLabel` /
  `_pendingWinner` / `_restartButton` and the `HandleGameOver` /
  `HandleIrisClosed` / `RestartMatch` methods migrated to
  `GameOverView`. Cleaner separation, no shared state across
  panels.

### Removed
- `GameEvents.OnLivesChanged` (no consumers since the HUD lives
  pip indicator was dropped) and its `Raise` helper / `LivesSystem`
  callsites.
- `GameManager.EnableJoining` (dead public API).
- `PowerUpController.ActiveEffects` allocator-getter (no callers).
- Empty `HitImpactVfx.HandleParry` subscription stub.
- `HUDBootstrap.cs` (runtime UI spawn). Replaced by editor-baked
  scene authoring; `EventSystem` + `InputSystemUIInputModule`
  creation moved into `HUDSetupAutomation`.
- `JuiceBootstrap.cs` (`RuntimeInitializeOnLoadMethod` →
  `DontDestroyOnLoad` spawn of juice listeners). Replaced by
  scene-baked `[Juice]` GameObject + `JuiceSetupAutomation` editor
  generator. Closes the silent-break-after-scene-reload bug.
- HUD layout: per-player info now in a top-left vertical stack.
  Round info (`ROUND N`) is centered top. Top-right exposes a
  debug-only pause button.
- Player score on HUD: numeric label replaced by a row of ink-filled
  square dots, one per point.
- Round label format dropped `R X / Y` in favour of `ROUND N`.
- Lives indicator pips and per-player corner panels removed.
- `Boomerang.Reflect` retints the trail to the new owner's color and
  reassigns `_throwerIndex` on parry ownership swap.
- `PlayerController.EnsureRuntimeComponents` auto-adds
  `ChargeTellVfx` and `RespawnInvulnFlash` alongside existing juice
  components.
- `CinemachineCameraManager` now resolves `CinemachineCamera` and
  punches `Lens.FieldOfView` on parry / elimination (unscaled time).
- Player HUD rows materialise on `OnRoundStarted` for every joined
  player; idempotent across rounds.

### Added
- `PrimitivePool` for runtime-spawned cube/sphere VFX primitives
  (`ed6a656`).
- `ShaderHelper` and `FontHelper` utilities replacing scattered
  `Shader.Find` / TMP font lookups (`ed6a656`).
- Juice layer:
  - `HitImpactVfx` — directional pooled-sphere burst on `OnPlayerHit`.
  - `PostFxJuice` — runtime URP `Volume` (priority 100) pulses
    `Vignette` + `ChromaticAberration` on hit / elim / parry / game
    over, on unscaled time.
  - `ChargeTellVfx` — per-player ground ring scaling with charge
    time; pops on release.
  - `WinFlourish` — confetti cube burst at winner position on
    `OnGameOver`, palette + winner-color mix.
  - `RespawnInvulnFlash` — `visual.enabled` toggle for 1.5s as i-frame
    tell on `OnPlayerRespawned`.
  - `JuiceBootstrap` — `RuntimeInitializeOnLoadMethod` spawns global
    juice listeners on a `DontDestroyOnLoad` GameObject.
- Audio cues: `BoomerangParry`, `ChargeLoop` IDs.
  `AudioManager` handlers for wall bounce, parry, pickup spawn,
  pickup collect, charge start. `AudioSetupAutomation` resolves
  pitch / volume metadata for the new cues so re-running setup
  scaffolds silent stubs.
- Camera FOV punch via `CinemachineCameraManager.PunchFov` (parry
  −8°/0.18s, elim −12°/0.45s, unscaled coroutine).
- `GameEvents.OnLivesChanged(playerIndex, lives)` event raised by
  `LivesSystem.RegisterPlayers` + `PlayerDied`.
- Fonts: Archivo Black (display) and JetBrains Mono Bold (mono),
  both OFL, under `Assets/_Project/Fonts/Resources/`. New
  `FontHelper.Display()` / `FontHelper.Mono()` cached lookups.
- HUD design system (UXML + USS) under `Assets/_Project/UI/HUD/Resources/`:
  - `HUDView.uxml` — root tree with named zones (top-left, top-center,
    top-right, center-countdown, world-layer, game-over-panel,
    pause-modal).
  - `PlayerRowTemplate.uxml` — info card (P# tag + ink-dot points).
  - `HUDStyle.uss` — neo-brutalism + soft pastel palette
    (cream / peach / sky / mint / lemon / lavender / dustypink / ink),
    `.brutal-card` composition (shadow + content + tint variants),
    `.brutal-button` with `:hover` / `:active`, `.point-dot`
    primitives, typography classes.
- `GameHUDView` generator MonoBehaviour: loads UXML/USS via
  `Resources`, clones `PlayerRowTemplate` per joined player, queries
  named elements, binds `GameEvents`, drives countdown coroutine,
  ELIM popup, off-screen arrows, pause modal, game-over reveal.
- `HUDBootstrap` now also ensures an `EventSystem` with
  `InputSystemUIInputModule` exists at `AfterSceneLoad` so UI Toolkit
  Buttons receive pointer events under the new Input System.
- Debug-only pause modal (`UNITY_EDITOR || DEVELOPMENT_BUILD`):
  `Resume` and `Exit` actions toggle `Time.timeScale` and
  `AudioListener.pause`; `Exit` stops play in editor or
  `Application.Quit` in build.
- Editor automation: `Tools > Boomerang Fu > Setup/HUD
  (Scene + Asset Refs)`. Idempotent — creates
  `HUDPanelSettings.asset`, builds `[GameHUD]` GameObject with
  `UIDocument` + `GameHUDView` + `SettingsScreen` + `IrisTransition`,
  wires every UXML / USS / PanelSettings reference via
  `SerializedObject`, and ensures an `[EventSystem]` GO with
  `InputSystemUIInputModule`. Bundled into `Setup All`.
- Editor automation: `Tools > Boomerang Fu > Setup/Juice (Scene
  Listeners)`. Idempotent — finds or creates `[Juice]` GameObject in
  the active scene and `Undo.AddComponent`s `PostFxJuice` +
  `HitImpactVfx` + `WinFlourish`. Bundled into `Setup All`.
- Editor automation: `Tools > Boomerang Fu > Setup/PowerUps
  (Scaffold SO Assets)`. Iterates the `PowerUpKey` enum and creates
  a `PowerUp_{Key}.asset` for every missing entry under
  `Assets/_Project/ScriptableObjects/Powerups/`, populating tint /
  duration / mutex / display name. Skip-if-exists, so hand-tuned
  Fire / Shield / Multi assets are preserved. Bundled into
  `Setup All`. Effect *behaviour* still lives in `PowerUpFactory` —
  the eight stub keys (`Caffeinated`, `DashThroughWalls`, `Teleport`,
  `Explosive`, `Extra`, `Disguise`, `Telekinesis`, `Decoy`) now have
  pickable assets even though their `StubPowerUp` impl is still a
  no-op.
- `RoundConfigSO.suddenDeath` boolean — flip it to switch the match
  from N-stocks-with-respawn to Boomerang Fu's 1-life knockout.
- `RoundManager.Restart()` — wired to the new HUD `RESTART` button
  on the game-over panel. Zeroes scores, resets transition flags,
  reopens the iris, runs `StartRoundRoutine`. Unblocks the iteration
  loop without leaving Play mode.
- HUD game-over modal gained a `RESTART` button (mint, brutal-button
  styling). `GameHUDView.RestartMatch` hides the panel and calls
  `RoundManager.Restart`. The panel's `picking-mode="Ignore"` was
  removed so the button receives clicks.
- `CinemachineCameraManager` exposes
  `stingerFovDelta` / `stingerFovTime` / `stingerShake` `SerializeField`s
  for tuning the last-kill cinematic.
- `GameEvents.OnIrisClosed` event raised by `IrisTransition` when
  its game-over close tween completes. Lets the new
  `GameOverView` defer its reveal until the wipe is fully closed.
  Edge case: if the iris shader is missing (`_mat == null`), the
  event raises immediately so the reveal still happens.
- `GameOverView` MonoBehaviour + `GameOverView.uxml` +
  `GameOverPanelSettings.asset`. Standalone overlay carrying just
  the winner card + RESTART button, mounted on `[GameOverOverlay]`
  via `Tools > Boomerang Fu > Setup/HUD`. Registered in
  `RootLifetimeScope` so `[Inject] Construct(RoundManager)` fires
  for the RESTART hookup.

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
