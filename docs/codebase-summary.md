# Codebase Summary

## Project Overview
Unity 6 project combining the Sinkii09 UIFramework package (MVVM + DI) with a Memory Flip Card Game that demonstrates full framework integration. The game is complete and playable with sound, animations, and win detection.

## Package Dependencies (`Packages/manifest.json`)
- **UniTask 2.5.11** (`com.cysharp.unitask`) — async/await support
- **R3 1.3.1** (`com.cysharp.r3`) — reactive extensions
- **VContainer 1.18.0** (`jp.hadashikick.vcontainer`) — dependency injection
- **DOTween Pro** — tween engine (installed via Asset Store, not UPM)
- **TextMeshPro** — Unity built-in

All CySharp/Hadashikick packages resolve through a single OpenUPM scoped registry.

**Scripting defines required:**
- `VCONTAINER_UNITASK_INTEGRATION` — enables VContainer async scope support
- `UNITASK_DOTWEEN_SUPPORT` — **NOT defined**; DOTween↔UniTask bridged manually via `UniTaskCompletionSource` in `TweenExtensions.AwaitAsync` (extension method on `Tween`)

---

## UIFramework Package (`Packages/com.sinkii09.uiframework/`)

### Core Systems
| File | Purpose |
|------|---------|
| `Runtime/Core/MVVM/UIView<T>.cs` | Base view — binds ViewModel, exposes `OnShowAsync`/`OnHideAsync` |
| `Runtime/Core/MVVM/UIViewBase.cs` | Caches `CanvasGroup`, `RectTransform`; drives show/hide lifecycle |
| `Runtime/Core/Navigation/UINavigator.cs` | Stack-based screen navigation |
| `Runtime/Core/Animation/DOTweenUIAnimator.cs` | `IUIAnimator` impl; fade/scale transitions via DOTween. Calls `.SetLink(viewBase.gameObject)` on every tween before awaiting via `TweenExtensions.AwaitAsync`. Private `AwaitTween` removed — single bridge in `TweenExtensions`. |
| `Runtime/Core/MVVM/UIBindingExtensions.cs` | Extension helpers: `BindToText`, `BindButton`, etc. |

### Installer Wizard
`Packages/com.sinkii09.uiframework/Editor/Installer/UIFrameworkInstallerWizardSteps.cs`

6-step in-Editor setup: installs OpenUPM packages → adds scripting defines → validates DOTween → creates UIRoot prefab → creates config ScriptableObject → creates folder structure.

---

## Memory Flip Card Game (`Assets/UIFramework/Features/MemoryGame/`)

**Assembly:** `UIFramework.MemoryGame`

### Folder Structure
```
Features/MemoryGame/
├── Logic/
│   ├── CardData.cs           ← Card state (id, pairIndex, flipped/matched flags)
│   ├── MemoryCardGame.cs     ← Pure domain engine (shuffle, flip, win detection)
│   └── FlipResult.cs         ← Enum: NeedSecond|Match|Mismatch|Locked|AlreadyFlipped|AlreadyMatched
├── ViewModels/
│   ├── MainMenuViewModel.cs  ← Title, RequestPlay/Settings/Quit commands
│   ├── GameplayViewModel.cs  ← Game state + card visibility reactive bindings
│   ├── WinViewModel.cs       ← Win screen state (moves, time, replay/menu commands)
│   ├── SettingsViewModel.cs  ← Music/SFX toggle state
│   └── WinArgs.cs            ← Navigation args passed to WinView
├── Views/
│   ├── MainMenuView.cs       ← DOTween entrance/exit animations (scale punch + stagger); buttons inside LayoutGroup use DOScale (not DOAnchorPosY — layout overrides position)
│   ├── CardView.cs           ← Single card; flip animation on tap
│   ├── GameplayView.cs       ← Board; instantiates CardView grid
│   ├── WinView.cs            ← Win screen
│   └── SettingsView.cs       ← Music/SFX toggles
├── Audio/
│   ├── ISoundService.cs      ← Interface: PlaySFX, PlayMusic, StopMusic, SetSFXEnabled, SetMusicEnabled
│   └── SoundManager.cs       ← MonoBehaviour impl; two AudioSources (music loop + SFX one-shot); persists state to PlayerPrefs
└── States/
    └── MemoryGameState.cs    ← IGameState; navigation entry point
```

### Key Architecture Notes
- `MemoryCardGame` — pure C#, no Unity dependency; all game rules isolated here
- `SoundManager` — two separate `AudioSource` components required (`_musicSource`, `_sfxSource`). The `RequireComponent(AudioSource)` provides one; second must be added manually in Inspector
- `MainMenuView` — all DOTween tweens use `SetUpdate(true)` (pause-safe at `Time.timeScale=0`). Pre-hides title+buttons in `Awake` so `SetActive(true)` renders first frame already invisible (no flash)
- `DOTweenUIAnimator.AwaitTween` — shared pattern; `CancellationTokenRegistration` disposed on both `OnComplete`/`OnKill` to avoid stale kill callbacks on DOTween's recycled tween pool

### Scene & Prefabs
- `Assets/Resources/` — all view prefabs loaded by name via UINavigator
- `Assets/Resources/GameplayView.prefab` — root board view
- `Assets/UIFramework/Features/MemoryGame/Prefabs/Card.prefab` — card template

---

## Aircraft Striker 2D Game (`Assets/UIFramework/Features/AircraftStriker/`)

**Assembly:** `UIFramework.AircraftStriker` — zero coupling with MemoryGame.

### Folder Structure
```
Features/AircraftStriker/
├── Scripts/
│   ├── Bootstrap/          AircraftLifetimeScope, AircraftGameBootstrap
│   ├── States/             AircraftGameplayState, AircraftMainMenuState (IGameState)
│   ├── Logic/              Enums, SOs (BulletConfig, BulletPatternConfig, EnemyConfig,
│   │                        WaveConfig, WaveDatabase, ShopCatalog), PlayerData,
│   │                        CheckpointState, GameScore, BulletOwner, HitEffectType
│   ├── Pooling/            PooledObject (base), AircraftPoolManager
│   ├── Input/              AircraftInputHandler (IDragHandler)
│   ├── Gameplay/           BulletController, BulletPatternExecutor, GrazeDetector,
│   │                        EnemyController, BossController, PickupController,
│   │                        PlayerController, BackgroundScrollController,
│   │                        WaveManager, GameplayController, CheckpointManager,
│   │                        HitEffect
│   ├── Progression/        IProgressionService, PlayerPrefsProgressionService, ShopService
│   ├── Audio/              IAircraftSoundService, SFXType, AircraftSoundManager
│   ├── ViewModels/         AircraftMainMenuViewModel, AircraftHUDViewModel,
│   │                        AircraftHUDChannel, AircraftGameOverViewModel,
│   │                        AircraftVictoryViewModel, AircraftPauseViewModel,
│   │                        ShopViewModel, SkinSelectionViewModel,
│   │                        GameOverArgs, VictoryArgs
│   ├── Views/              AircraftMainMenuView, AircraftHUDView, AircraftGameOverView,
│   │                        AircraftVictoryView, AircraftPauseView, ShopView,
│   │                        SkinSelectionView, ShopItemRow, SkinItemRow
│   └── Editor/             AircraftStrikerSetupWizard, HitEffectBuilder
├── Prefabs/                Player/, Enemies/, Projectiles/, Pickups/, VFX/
├── AssetBundles/           Addressable view prefabs (see UI_Prefab Addressables group)
├── ScriptableObjects/      Waves/, Shop/
└── Scenes/                 AircraftGame.unity  ← must be created manually
```

### Key Architecture Notes
- **AircraftHUDChannel** — Singleton bridge solving UIViewFactory child-scope isolation: `GameplayController` writes to channel, `AircraftHUDViewModel` proxies its `ReactiveProperty<T>` fields. Child scope inherits parent singleton so the channel is visible to both sides.
- **CheckpointManager** — Pure C# state machine. Saves before each boss wave; `RequestRestore()` sets `IsPendingRestore` flag consumed by `GameplayController.StartGame()` on retry.
- **WaveManager** — MonoBehaviour (needs coroutines). Receives `GameplayController` via `StartWaves(this)` method, not constructor, to break circular DI dependency.
- **BulletPatternExecutor** — Pure C# stateless. Supports Ring/SpiralCW/SpiralCCW/AimedFan/BurstFan/Wall/DualSpiral patterns; spiral uses `Time.time * SpiralStepDegrees`.
- **PooledObject.OnReturn** — wired in `AircraftPoolManager.CreatePool<T>` via `createFunc`: `obj.OnReturn += p => { if (p.gameObject.activeSelf) pool?.Release((T)p); }`.
- **HitEffect system** — `HitEffect` extends `PooledObject` and plays burst particle VFX at impact position. Auto-returns to pool after particle lifetime expires. Spawned by `AircraftPoolManager.SpawnHitEffect(Vector3, HitEffectType)` with color-coded feedback per hit type (orange=enemy, gold=boss, cyan=player). Lifetimes: 0.6s (enemy), 0.8s (boss), 0.7s (player).
- **View animations** — All views use `OnPrepareForShow()` + `OnShowAsync()` DOTween pattern (same as MemoryGame). GameOver/Victory: panel scale-in + staggered stat labels + DOVirtual.Float score count-up.

### Scene Setup (Manual — not yet done)
1. Create `AircraftGame.unity`
2. Add `AircraftLifetimeScope` root GO; set parent scope to `UIFrameworkLifetimeScope`
3. Assign all `[SerializeField]` MonoBehaviours and ScriptableObjects in Inspector
4. Create prefabs for all `BulletController`, `EnemyController`, `BossController`, `PickupController` types
5. Create `WaveDatabase` SO with 10 waves (boss at wave 5 and 10)
6. Create `ShopCatalog` SO with weapon upgrade, bonus HP, skin unlock items

---

## Recent Changes

### 2026-07-05 — Aircraft Striker: activated Addressables loading for UI views
**Goal:** Finish an already-scaffolded Resources→Addressables migration for the AircraftStriker UI (the `IUILoader`/`ResourcesUILoader`/`AddressablesUILoader` abstraction in the UIFramework package existed but was never switched on). CDN (Unity CCD) wiring is deferred to a later task.
**Root cause of why it wasn't working:** three independent gaps, any one of which would have kept it on the Resources path — (1) the 7 `[UIViewKey]` strings on AircraftStriker views are folder-prefixed (`AircraftStriker/AircraftGameOverView`) but the Addressables group's `m_Address` entries were bare class names (`AircraftGameOverView`) — `Addressables.LoadAssetAsync` would have thrown "Failed to load" for every view; (2) `AddressablesUILoader` and its DI registration branch are gated by `#if ADDRESSABLES`, and that symbol was never defined for any platform, so the Addressables branch was compiled out entirely; (3) `UIFrameworkConfig.LoaderMode` was still `Resources`.
**Fix:**
- `Assets/AddressableAssetsData/AssetGroups/UI_Prefab.asset` — renamed the 7 top-level view addresses to match their `[UIViewKey]` strings exactly (`ShopItemRow`/`SkinItemRow` addresses left as-is — they're plain `[SerializeField]` refs in `ShopView`/`SkinSelectionView`, never loaded via `IUILoader`).
- `ProjectSettings/ProjectSettings.asset` — added `ADDRESSABLES` scripting define to `Standalone` and `WebGL` (the two confirmed target platforms).
- `Assets/UIFramework/Features/AircraftStriker/ScriptableObjects/UIFrameworkConfig.asset` — `LoaderMode: Resources` → `Addressables`. `MemoryGame`'s separate `UIFrameworkConfig.asset` was left untouched (its 4 views are still plain `Assets/Resources/*.prefab`, out of scope).
- `AircraftStrikerSetupWizard.cs` — `ViewsRoot` const updated from `Assets/Resources/AircraftStriker` to the new `AssetBundles/AircraftStriker` location, since the wizard is "safe to re-run" and would otherwise regenerate prefabs back under `Resources/`, undoing the migration. Added a caution comment: re-running the wizard post-migration can still change prefab GUIDs (delete+recreate at the same path), which would desync the Addressables group's `m_GUID` bindings — verify the Addressables Groups window after any re-run.
**Manual step required (cannot be done from CLI):** open Unity Editor, confirm no console errors, then do a full Addressables rebuild (Groups window → Build, or Build & Release once CCD is wired) before playing — address-string changes aren't picked up by a stale/incremental build.

### 2026-06-21 — Aircraft Striker: bullet hit effect VFX system
**Feature:** Burst particle VFX on bullet impact with color-coded feedback (orange=enemy, gold=boss, cyan=player).
**New files:**
- `HitEffectType.cs` — enum: Enemy, Boss, Player
- `HitEffect.cs` — `PooledObject` subclass; plays burst particles, auto-returns to pool after particle lifetime expires via `UniTask.Delay`
- `HitEffectBuilder.cs` (editor) — menu item `AircraftStriker > Setup Wizard > Build Hit Effect Prefab` generates `HitEffect.prefab` with wired serialized fields
**Modified files:**
- `AircraftPoolManager.cs` — added `_hitEffectPrefab` field, `_hitEffectPool: ObjectPool<HitEffect>`, `SpawnHitEffect(Vector3, HitEffectType)` with embedded color constants
- `BulletController.cs` — `OnTriggerEnter2D` now captures `Vector2 hitPos = transform.position` and passes it to gameplay callbacks
- `GameplayController.cs` — `OnPlayerHit()`, `OnEnemyHit()`, `OnBossHit()` now accept `Vector2 hitPos` parameter and call `_pool.SpawnHitEffect(hitPos, type)`
**Manual step:** Run "Build Hit Effect Prefab" menu, then assign the generated prefab to `AircraftPoolManager._hitEffectPrefab` in Inspector.

### 2026-06-20 — UIFramework Phase 2 Bug Fixes (remaining CRITICALs + W13)
**`GameLifecycleManager.cs`** — Added `IUINavigator` injection. `RestartCurrentStateAsync` now calls `_navigator.CloseAllAsync()` between `OnExitAsync` and `OnEnterAsync`, clearing any views left on the nav stack by `OnExitAsync`. If `OnExitAsync` already cleared the stack, `CloseAllAsync` is a no-op.

**`UIStateMachine.cs`** — Rollback split into two cases: `OperationCanceledException` restores `_currentState = previous` (cancellation aborted before commit; previous is still valid); all other exceptions after `OnExitAsync` succeeded set `_currentState = null` (previous already exited; restoring it would cause a double-exit on the next transition). `exitCompleted` bool tracks whether `OnExitAsync` ran.

**`UIViewFactory.cs`** — Added `_pending: Dictionary<Type, UniTaskCompletionSource<IUIView>>`. If `CreateGenericAsync` for a type is in-flight, a second concurrent caller awaits the first result instead of instantiating a duplicate GameObject. Root cause: main-thread async interleaving (not threading). `_pending` entries are removed in a `finally` block.

**`ViewModelBase.cs`** — Added `if (_disposed) return;` guard at top of `NotifyHide()` (hygiene: prevents post-dispose DisposableBag resurrection in edge-case VContainer lifetime scenarios).

**`ScaleTransition.cs`, `ZoomOutFadeTransition.cs`** — Added `OnKill` restore guards matching `SlideTransition` pattern. Cancelled transitions now restore scale/alpha to their pre-animation state instead of leaving the transform at a mid-tween position.

### 2026-06-20 — UIFramework Phase 1 Bug Fixes (5 CRITICALs from adversarial review)
**Root cause:** Adversarial review of all 67 Runtime files found 9 critical bugs; Phase 1 addresses the 5 with highest correctness impact.

**`IViewModel.cs` / `ViewModelBase.cs` / `UIView.cs`** — Renamed `IViewModel.Show()` → `IViewModel.NotifyHide()`. The method was named `Show` but executed hide-side teardown (`OnHide()` + `_showDisposables.Dispose()`). Any external `IViewModel` implementor that read the interface assumed `Show()` = init, breaking teardown. `UIView.HideAsync` caller updated.

**`UIView.cs`** — Pre-await `if (_viewModel == null) return` in `HideAsync` was silently skipping `base.HideAsync`, leaving `IsVisible = true` and the GameObject active. Changed to `Debug.LogError` only — `base.HideAsync` always runs. Post-await null guard preserved for `FactoryReset` race.

**`NavigationStack.cs`** — `PushAsync` now awaits `view.ShowAsync(ct)` **before** adding to `_views`. Failure or cancellation leaves the stack unchanged and re-throws. Prevents phantom stack entries from cancelled shows.

**`UIViewRegistry.cs`** — Added `HashSet<string>` and `HashSet<Type>` duplicate-key detection in `AutoRegister`. Collisions now produce `Debug.LogError` and skip the duplicate. `ResetOnDomainReload` clears both sets.

**`DOTweenUIAnimator.cs`** — Added `.SetLink(viewBase.gameObject)` on every transition tween before awaiting. Removed private `AwaitTween` duplicate — now uses `TweenExtensions.AwaitAsync`. Orphaned tweens on force-destroy are now killed automatically by DOTween via the `SetLink` binding.

**Note for custom view code:** `TweenExtensions.AwaitAsync` does NOT call `SetLink` — callers writing DOTween Sequences in `OnShowAsync`/`OnHideAsync` must chain `.SetLink(gameObject)` themselves.

### 2026-06-20 — UIFramework: SetScopeContainer + RestartCurrentStateAsync additions
**`IUIViewFactory.cs` / `UIViewFactory.cs`** — Added `SetScopeContainer(IObjectResolver)` / `ResetScopeContainer(IObjectResolver?)` to support game-specific DI child scopes. A game `LifetimeScope` calls `SetScopeContainer(Container)` after `base.Awake()` so ViewModels created in that scene can resolve game-level services (e.g. `IProgressionService`, `WaveManager`) from the game scope rather than the framework root scope. `ResetScopeContainer(expected)` guards against stale refs when a reloaded scene's `OnDestroy` fires after a newer scope has already set a different override.

**`GameLifecycleManager.cs`** — Added `RestartCurrentStateAsync()`: exits then re-enters the current state in-place, bypassing the state machine's same-state guard. Required for Retry/Restart from pause — `ChangeStateAsync<AircraftGameplayState>()` is silently rejected when already in `AircraftGameplayState`.

### 2026-06-20 — Aircraft Striker: two lives lost on one hit + i-frames
**Bug:** One burst/spread enemy shot pattern consumed two player lives (two `BulletController.OnTriggerEnter2D` calls reached `OnPlayerHit()` in the same physics step before either returned).
**Root cause:** `GameplayController.OnPlayerHit()` only short-circuited when `_isGameActive = false` (death state). While the player was alive with 2+ lives, both hits ran `TakeDamage()` concurrently.
**Fix:**
- `GameplayController.cs` — Added `_invincibleUntil` (float timestamp) + `IsInvincible` property. `OnPlayerHit()` guards with `if (!_isGameActive || IsInvincible) return`. After a surviving hit, sets `_invincibleUntil = Time.time + 1.5f`. On death, i-frames are NOT granted (`_isGameActive = false` stops all further processing). `StartGame`/`StopGame` reset `_invincibleUntil = 0f`.
- `PlayerController.cs` — `Update()` blinks `_spriteRenderer` at 10 Hz during i-frames: `enabled = !_gameplay.IsInvincible || (Time.time % 0.2f < 0.1f)`. Runs OUTSIDE the `IsGameActive` guard so sprite restores to visible when the game stops.
**Inspector note:** Assign the player ship's `SpriteRenderer` to `PlayerController._spriteRenderer` field; blink silently no-ops if null.

### 2026-06-20 — Aircraft Striker: GameOver buttons appear too late (~1.9s delay)
**Bug:** After GameOver view appeared, player had to wait ~2 seconds before Retry/Menu buttons became visible.
**Root cause:** `AircraftGameOverView.OnShowAsync` inserted `_retryButton` at timestamp `1.9f` and `_menuButton` at `2.0f`, sequentially after a 1.1s score count-up starting at `0.72f`. Total sequence: ~2.3s before any button.
**Fix:** Parallelized button entrance with score count-up — `_retryButton` moved to `0.80f`, `_menuButton` to `0.90f`. Score count-up shortened from `1.1f` to `0.7f`. Total sequence reduced to ~1.42s; buttons appear at ~0.8s.

### 2026-06-20 — Aircraft Striker: PauseView does not pause game / restart broken
**Bug 1:** Game continued running at full speed while PauseView was open (enemies moved, bullets fired).
**Bug 2:** Restart button in PauseView could silently fail — no error callback on `.Forget()`. Menu button used raw navigator calls (`CloseAllAsync` + `ShowAsync<AircraftMainMenuView>`) instead of the state machine, leaving `_currentState` stale — same bypass bug already fixed in GameOver/Victory ViewModels.
**Root cause:** `AircraftPauseView` had no `Time.timeScale` management. All three exit paths (Resume/Restart/Menu) never restored `timeScale`, which would also freeze DOTween hide animations once the game-freeze was added.
**Fix:**
- `AircraftPauseView.cs` — Override `OnShowAsync` to set `Time.timeScale = 0f` AFTER the entrance animation completes (so the slide-in plays at normal speed).
- `AircraftPauseViewModel.cs` — All three handlers restore `Time.timeScale = 1f` synchronously BEFORE any async call (so hide animations DOTween-animate at normal speed). `GoToMainMenuAsync()` now calls `_lifecycle.ChangeStateAsync<AircraftMainMenuState>()` instead of raw navigator. All `.Forget()` calls have error callbacks.

### 2026-06-20 — Aircraft Striker: pooled objects visible on screen after returning to main menu
**Bug:** After game-over → "Return to Menu", enemies, boss, bullets, and pickups remained visible on the main menu screen.
**Root cause:** `GameplayController.StopGame()` cancelled CancellationTokenSources and stopped wave spawning but never returned active pool objects to their pools. Pool objects are only deactivated (`SetActive(false)`) when individually released via `pool.Release()` — which only fires when an object dies and calls `ReturnToPool()`. Live enemies at game-over just froze in place, still active.
**Fix:**
- `PooledObject.cs` — `ReturnToPool()` changed from `protected` to `public` so the pool manager can bulk-call it.
- `AircraftPoolManager.cs` — Added `ReturnAll()`: uses `GetComponentsInChildren<PooledObject>(false)` (snapshots active children) then calls `ReturnToPool()` on each. Pool's `OnReturn` handler guards against double-release via `if (p.gameObject.activeSelf)`. VFX `ParticleSystem` handled separately (stop + deactivate) since they don't extend `PooledObject`.
- `GameplayController.cs` — `StopGame()` calls `_pool.ReturnAll()` after stopping waves.

### 2026-06-20 — Aircraft Striker: Return-to-Menu skips state machine, Play Again silently blocked
**Bug:** After game over or victory, pressing "Return to Menu" closed all views but left `UIStateMachine._currentState` pointing to `AircraftGameplayState`. Pressing "Play Again" from MainMenu triggered the guard `if (currentState == targetState) return;` silently without transitioning.
**Root cause:** `AircraftGameOverViewModel` and `AircraftVictoryViewModel` called `_navigator.CloseAllAsync()` + `_navigator.ShowAsync<AircraftMainMenuView>()` directly, bypassing the state machine. State machine never knew the game had ended, so `_currentState` remained `AircraftGameplayState`.
**Fix:** Introduced new `AircraftMainMenuState : IGameState`. Both ViewModels now call `_lifecycle.ChangeStateAsync<AircraftMainMenuState>()` instead. This triggers `AircraftGameplayState.OnExitAsync` (which stops the game loop, destroys the player, closes gameplay views), properly updates `_currentState` to `AircraftMainMenuState`, and correctly transitions to the menu. Playing again now calls `_lifecycle.ChangeStateAsync<AircraftGameplayState>()` which properly exits the menu state and enters gameplay.
**Files changed (5):**
- `AircraftMainMenuState.cs` (new) — IGameState impl; `OnEnterAsync` shows MainMenu, `OnExitAsync` does nothing
- `AircraftLifetimeScope.cs` — registers `AircraftMainMenuState`
- `AircraftGameBootstrap.cs` — initializes `AircraftMainMenuState` as the initial state instead of showing the view directly
- `AircraftGameOverViewModel.cs` — `GoToMainMenuAsync()` now calls `_lifecycle.ChangeStateAsync<AircraftMainMenuState>()`
- `AircraftVictoryViewModel.cs` — `GoToMainMenuAsync()` now calls `_lifecycle.ChangeStateAsync<AircraftMainMenuState>()`

### 2026-06-16 — Aircraft Striker: HP → lives system
**Bug:** Any boss bullet with `BulletConfig.Damage ≥ 3` (= MaxHealth) killed the player instantly in one hit.
**Root cause:** `PlayerData.TakeDamage(int amount)` subtracted the raw damage amount. No per-hit cap meant a boss bullet with `Damage=3` reduced `CurrentHealth` from 3 to 0 immediately.
**Redesign:** Converted from HP system to lives system — any hit always costs exactly 1 life regardless of bullet damage.
**Files changed (12):**
- `PlayerData.cs` — renamed `MaxHealth`→`MaxLives`, `CurrentHealth`→`Lives`; `TakeDamage()` now takes no param and decrements 1 life; `RestoreHealth`→`RestoreLife`
- `CheckpointState.cs` — renamed `HealthAtCheckpoint`→`LivesAtCheckpoint`
- `AircraftHUDChannel.cs` — renamed reactive props `CurrentHealth`→`Lives`, `MaxHealth`→`MaxLives`; updated `Push()`
- `AircraftHUDViewModel.cs` — renamed proxy properties
- `AircraftHUDView.cs` — updated `vm.Lives` / `vm.MaxLives` binding
- `GameplayController.cs` — `OnPlayerHit()` now no-param; calls `TakeDamage()` no-param; `RestoreLife(1)` on health pickup; `LoadBonusLives()`
- `BulletController.cs` — calls `_gameplay.OnPlayerHit()` with no argument (damage param removed)
- `IProgressionService.cs` — `LoadBonusHealth()` → `LoadBonusLives()`
- `PlayerPrefsProgressionService.cs` — impl renamed accordingly
- `ShopItemType.cs` — `BonusHealth` → `BonusLives` (same ordinal, no asset migration needed)
- `AircraftStrikerSetupWizard.cs` — seed data updated to "Extra Life" / "Start with +1 max life."
- `bonus_hp.asset` — DisplayName "Extra Life", Description "Start with +1 max life."

### 2026-06-15 — Aircraft Striker: game-over guard (player can still play after game over)
**Bug:** After game-over overlay appeared, player could still move, shoot, graze, collect pickups, and enemy bullets could trigger duplicate `HandleGameOver` calls.
**Root cause:** No `_isGameActive` flag in `GameplayController`. `OnPlayerHit` had no re-entry guard. `WaveManager.StopWaves()` only called from `StopGame()` (on Retry/Menu click), not on player death.
**Fix:**
- `GameplayController.cs` — Added `_isGameActive` bool + `IsGameActive` property. `OnPlayerHit` guards with `if (!_isGameActive) return`. On player death: sets `_isGameActive = false` + calls `StopWaves()` synchronously before `HandleGameOver().Forget()` (eliminates same-frame multi-hit window). `OnAllWavesComplete` mirrors the same pattern. `StartGame`/`StopGame` set the flag.
- `PlayerController.cs` — `Update()` and `OnTriggerEnter2D()` both check `!_gameplay.IsGameActive` — stops movement, shooting, and graze registration after game over.
- `PickupController.cs` — `OnTriggerEnter2D()` checks `!_gameplay.IsGameActive` — prevents pickups from mutating final score during overlay.

### 2026-06-15 — Aircraft Striker: player ship runtime spawn/destroy + session CTS race fix
**Changes:**
- `AircraftGameplayState.cs` — `PlayerController` is now spawned via `Object.Instantiate(_playerShipPrefab)` in `OnEnterAsync` and destroyed in `OnExitAsync`. Camera-relative spawn position (`-camHalf * 0.7f`). Null/re-entrancy guards added. Eliminates Bug 2 (prefab asset reference assigned as scene instance).
- `GameplayController.cs` — Added `_sessionCts` (`CancellationTokenSource`) created per `StartGame()`, cancelled/disposed in `StopGame()`. `HandleGameOver` and `ShowVictoryAsync` now pass this token to `ShowAsync`, so stale game-over/victory overlays are cancelled if the player retries before the animation finishes.
- `AircraftLifetimeScope.cs` — `_playerController` Inspector field is now intentionally a **prefab asset reference** (not scene instance). The `RegisterInstance` registers it as a spawn template. Updated comment to reflect this.
**Inspector note:** Assign `PlayerShip.prefab` from the Project window directly to `AircraftLifetimeScope._playerController` — a prefab reference is now correct (opposite of the previous "scene instance" instruction).

### 2026-06-15 — Post-review corrections to player spawn and session lifecycle
**Changes (second-pass review corrections):**
- `GameplayController.cs` — implements `IDisposable` (VContainer auto-calls on scope teardown); `_currentWave`/`_isBossWave` now initialized BEFORE `StartWaves()` in both branches (previously set after — caused HUD to flash wave 1 on checkpoint restore); progression saves (`SaveHighScore`, `SaveCoins`) moved here from ViewModels — saves happen at game-over/victory determination time, not at overlay display time.
- `AircraftGameOverViewModel.cs`, `AircraftVictoryViewModel.cs` — `Initialize()` now display-only (reads `LoadHighScore` for BestScore display, no more writes). `AircraftVictoryViewModel.OnPlayAgainPressed` fixed to use `RestartCurrentStateAsync` (was `ChangeStateAsync<AircraftGameplayState>` — silently dropped by same-state guard).
- `UIViewFactory.cs` (framework) — added `ct.ThrowIfCancellationRequested()` at the start of `CreateGenericAsync` — prevents `Initialize(args)` from running on cached views when the session CT is already cancelled (fast-retry scenario).

### 2026-06-13 — Aircraft Striker 2D game — full implementation (Phases 1–8)
**Feature:** Complete standalone aircraft shooter game built on Sinkii09 UIFramework. 60 scripts across Bootstrap, Logic, Pooling, Input, Gameplay, Progression, Audio, ViewModels, Views.
**Key gotchas discovered:**
- `UIViewFactory` creates child scopes per view → pre-registering a ViewModel as parent Singleton creates two instances. Fixed with `AircraftHUDChannel` singleton bridge.
- `PooledObject.ReturnToPool()` fires `OnReturn` event but nobody subscribes unless wired in `createFunc`. Fixed in `AircraftPoolManager`.
- `WaveManager` receives `GameplayController` via method (`StartWaves(this)`) to break circular DI.
- `DOKill()` returns `int`, not `Transform` — cannot chain `.DOPunchScale()` on it. Must split into two statements.
**Status:** All C# code complete. Editor Setup Wizard created — run it once in Unity to generate all assets automatically.

### 2026-06-13 — Aircraft Striker Editor Setup Wizard
**Feature:** `Assets/UIFramework/Features/AircraftStriker/Scripts/Editor/AircraftStrikerSetupWizard.cs`
**What it does:** Menu `AircraftStriker > Setup Wizard > Run Full Setup` generates every asset in one click:
- 6 BulletConfig SOs, 9 BulletPatternConfig SOs, 4 EnemyConfig SOs, 10 WaveConfig SOs, WaveDatabase, 5 ShopItemConfig SOs, ShopCatalog
- 6 bullet prefabs, 4 enemy prefabs, 3 pickup prefabs, 2 VFX ParticleSystem prefabs
- System prefabs: AircraftPoolManager (all arrays wired), WaveManager, BackgroundScroll, AircraftSoundManager, PlayerShip (with PlayerHitbox child), AircraftInputHandler
- 9 view/row prefabs in `Assets/Resources/AircraftStriker/` with all SerializeField refs wired
**Also patched:** `UIViewFactory.InstantiateViewAsync<TView>` now checks `[UIViewKey]` attribute first (supports subfolder paths for all registration paths). All 7 view classes have `[UIViewKey("AircraftStriker/ClassName")]` + correct `UILayer` override.
**Remaining manual step:** Open `AircraftGame.unity`, assign prefabs/SOs to `AircraftLifetimeScope` in Inspector, add art sprites.

### 2026-06-08 — MainMenuView animation flash fix
**Issue:** Buttons visible for one frame before entrance animation started (flash on show).
**Root cause 1:** `OnHideAsync` finally block restored buttons to `Vector3.one` instead of `Vector3.zero`; next `SetActive(true)` rendered that frame at full scale before `OnShowAsync` reset them.
**Root cause 2:** No initial hidden state set in `Awake`; first-ever show also flashed.
**Fix:** Set `localScale = Vector3.zero` for title + all buttons in `Awake`; corrected finally block from `Vector3.one` → `Vector3.zero`.

### 2026-06-08 — MainMenuView LayoutGroup animation compatibility
**Issue:** `DOAnchorPosY` animation broken — buttons visible only at wrong positions (LayoutGroup overrides `anchoredPosition` every frame).
**Fix:** Replaced position animation with `DOScale` (0→1, `Ease.OutBack`). LayoutGroup never touches `localScale`. Removed `_buttonSlideDownOffset`, `_buttonOrigins`, `_buttonRects`; replaced with `_buttonTransforms: Transform[]`.

### 2026-06-08 — DOTweenUIAnimator CancellationTokenRegistration leak fix
**Issue:** `ct.Register(() => tween.Kill())` result never disposed; stale kill callback could fire on DOTween's pooled/recycled tween objects.
**Fix:** Capture `CancellationTokenRegistration reg` and dispose in both `OnComplete` and `OnKill` lambdas. Applied to both `DOTweenUIAnimator.AwaitTween` and `MainMenuView.AwaitTween`.

### 2026-06-07 — Sound system
Added `ISoundService` + `SoundManager` with separate music/SFX AudioSources. Music loops; SFX plays one-shot. State persisted to `PlayerPrefs`. `SettingsView` toggles wired through `SettingsViewModel`.

### 2026-06-08 — Zoom-punch scene transition (MainMenu → Gameplay)
**Feature:** Option B zoom-out-fade transition between screens.
- New `ZoomOutFadeTransition.cs` (UIFramework package) — `UITransition` subclass; `CreateHideTween` joins root scale 1→ZoomOutScale + CanvasGroup fade 1→0 simultaneously. `CreateShowTween` is the inverse.
- `MainMenuView.OnHideAsync` simplified to state-reset only (element scales back to 0, root scale back to 1) — visual is owned by the UITransition.
- `GameplayView.OnShowAsync` — sets root scale to 0.9, spawns grid, then punches scale to 1 with `Ease.OutBack` (0.4 s) giving "entering the game" feel.
**Key lifecycle insight:** `UIViewBase.HideAsync` runs `_animator.HideAsync` (UITransition) BEFORE `OnHideAsync`. `OnHideAsync` runs after the view is already visually hidden — use it for state reset/cleanup only, not visible animations.
**Manual step required:** In Unity Inspector, create `ZoomOutFadeTransition` asset via `Assets > Create > UIFramework/Transitions/ZoomOutFade` and assign it to `MainMenuView` prefab's `_hideTransition` field.

### 2026-06-07 — MainMenuView juice animations
Added DOTween entrance/exit to `MainMenuView`: title scale-punch (`OutBack`), buttons stagger scale-in (`OutBack`, 80ms apart). All run with `SetUpdate(true)`.

---

## Code Standards
- DOTween↔UniTask bridge: always use `UniTaskCompletionSource` pattern (no `UNITASK_DOTWEEN_SUPPORT`); always dispose `CancellationTokenRegistration`
- DOTween tweens: always `SetUpdate(true)` so they survive `Time.timeScale = 0`
- Animations inside LayoutGroup: use `DOScale`, never `DOAnchorPosY`/`DOMove`
- Pre-hide animated views in `Awake` (`localScale = Vector3.zero`) to prevent first-frame flash
- `UIViewBase.HideAsync` order: UITransition FIRST → `OnHideAsync` SECOND. `OnHideAsync` runs after view is visually hidden — use for cleanup/reset only, not visible animations. Visible hide animations must live in a `UITransition` subclass assigned to `_hideTransition`
