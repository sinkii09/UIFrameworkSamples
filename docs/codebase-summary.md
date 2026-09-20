# Codebase Summary

## Project Overview
Unity 6 project built around the Sinkii09 UIFramework package (MVVM + DI + R3 + UniTask), consumed as a pinned git dependency from its own repository. Four games share it and double as its proving ground, each under `Assets/UIFramework/Features/<Game>/`: **Memory Flip Card** (complete and playable — sound, animations, win detection), **Aircraft Striker 2D**, **Color Stack Sort**, and **AstralChorus** (in progress; content and economy layers exist, no gameplay yet).

## Package Dependencies (`Packages/manifest.json`)
- **com.sinkii09.uiframework** — **pinned git tag**, `#v3.3.0` as of 2026-09-20:
  `"https://github.com/sinkii09/com.sinkii09.uiframework.git#v3.3.0"`.
  Re-pin with the `package-add` tool, never by hand-editing this file or `manifest.json` — the
  lock file has to move with it.
  For most of 2026-08-01/02 this was instead a local path dependency
  (`"file:../../com.sinkii09.uiframework"`), used deliberately so framework fixes could be
  compile-verified from this project before being tagged. That mode consumes the other repo's
  **working tree** — uncommitted edits there change this project immediately, and a compile error
  there blocks this project's entire test suite (it did, twice). Swap back to `file:` only while
  actively co-developing the framework, and re-pin as soon as the work is tagged.
  Source edits always happen in the checkout at `e:\Hoc_2025\1_1_2025\com.sinkii09.uiframework`,
  never in `Library/PackageCache/`, which is a read-only clone.
  **Changing this line requires a re-resolve to take effect** — a raw manifest edit alone does not
  reliably trigger one; re-run the package add for the same id (see memory
  `unity-test-assembly-and-bee-gotchas`).
  **As of 2026-07-19 this is no longer an embedded package** — it was extracted to its own repo so
  other projects can depend on it too. Source edits happen in a checkout of that repo, not under
  `Packages/` here. Canonical docs: Obsidian vault at
  `C:\Users\user\OneDrive\Documents\Obsidian Vault\UIFramework\`.
  `manifest.json` also carries `"testables": ["com.sinkii09.uiframework"]` — required for the
  package's own PlayMode tests to appear in the Test Runner; do not remove it.
- **com.unity.nuget.newtonsoft-json 3.2.2** — pulled in by the framework's persistence system
  (was already resolving transitively via Addressables before it was declared).
- **UniTask 2.5.11** (`com.cysharp.unitask`) — async/await support
- **R3 1.3.1** (`com.cysharp.r3`) — reactive extensions
- **VContainer 1.19.0** (`jp.hadashikick.vcontainer`) — dependency injection
- **DOTween Pro** — tween engine (installed via Asset Store, not UPM)
- **TextMeshPro** — Unity built-in

All CySharp/Hadashikick packages resolve through a single OpenUPM scoped registry.

**Scripting defines required:**
- `VCONTAINER_UNITASK_INTEGRATION` — enables VContainer async scope support
- `UNITASK_DOTWEEN_SUPPORT` — **NOT defined**; DOTween↔UniTask bridged manually via `UniTaskCompletionSource` in `TweenExtensions.AwaitAsync` (extension method on `Tween`)

---

## UIFramework Package (`https://github.com/sinkii09/com.sinkii09.uiframework`, git dependency)

### Core Systems
| File | Purpose |
|------|---------|
| `Runtime/Core/MVVM/UIView<T>.cs` | Base view — binds ViewModel, exposes `OnShowAsync`/`OnHideAsync` |
| `Runtime/Core/MVVM/UIViewBase.cs` | Caches `CanvasGroup`, `RectTransform`; drives show/hide lifecycle |
| `Runtime/Core/Navigation/UINavigator.cs` | Stack-based screen navigation |
| `Runtime/Core/Animation/DOTweenUIAnimator.cs` | `IUIAnimator` impl; fade/scale transitions via DOTween. Calls `.SetLink(viewBase.gameObject)` on every tween before awaiting via `TweenExtensions.AwaitAsync`. Private `AwaitTween` removed — single bridge in `TweenExtensions`. |
| `Runtime/Controls/Collections/RecyclerView.cs` (+ `.Pump`/`.Cells`) | Recycling list — live cell count tracks the viewport, not the item count (10k items ≈ 11 cells). Cell sizes are **declared, never measured**: uniform by default, per-index via `SetItemSizeProvider`. Pure `RecycleWindow` decision function tested in EditMode; integration surface in PlayMode. |
| `Runtime/Controls/Collections/IItemOffsets.cs` (+ `UniformOffsets`, `PrefixSumOffsets`) | Where each item sits in offset space. Pure, no Unity types. `UniformOffsets` reproduces the pre-variable-size arithmetic exactly and is the regression anchor; `PrefixSumOffsets` carries per-index declared sizes. Spacing is included in offsets, excluded from sizes. |
| `Runtime/Core/MVVM/UIBindingExtensions.cs` | Extension helpers: `BindToText`, `BindButton`, etc. |
| `Runtime/Core/Lifecycle/TransitionOverlayView.cs` | Resident full-screen overlay on the `Overlay` layer; shown/hidden by `GameLifecycleManager` around every state transition to hide the blank-screen gap. Optional per-game — see `ITransitionOverlay`/`NullTransitionOverlay`. |
| `Runtime/Core/Persistence/JsonSaveService.cs` | `ISaveService` impl — inject it, call `SaveAsync(poco)` / `LoadAsync<T>()`. Key defaults to `typeof(T).Name`. Orchestration only: per-key locking, R3 events, backup policy. |
| `Runtime/Core/Persistence/LocalFileStorageBackend.cs` | The one storage swap seam (`IStorageBackend`). Atomic writes to `persistentDataPath/Saves/<key>.json` + rolling `.bak`. |
| `Runtime/Core/Config/UIViewPolicyConfig.cs` (+ `UIViewPolicyResolver`) | Per-view `Resident` / `NeedsBackdrop` / `PreloadOnBoot`, keyed by **load key** not class name. Inspector-only asset on `UIFrameworkLifetimeScope`; empty means framework defaults for every view. Resolver is registered unconditionally and is a null-object when no asset is assigned. |
| `Runtime/Core/MVVM/UIViewCacheSweeper.cs` | Entry point running a `UniTask.Delay` loop that calls `UIViewFactory.SweepAsync`. Destroys views idle past `ViewCacheGraceSeconds` (`0` = off, the default). Only registered when eviction is enabled. |
| `Runtime/Core/MVVM/UIBackdrop.cs` | One reusable dim `Image` parked directly beneath any view whose policy sets `NeedsBackdrop`. Driven by `UINavigator.RefreshLayerBlocking` — same authority as layer blocking. Colour from `UIFrameworkConfig.BackdropColor`. |
| `Runtime/Core/MVVM/UIViewPreloader.cs` | Warms `PreloadOnBoot` views into the factory cache. Never runs on its own — call `PreloadAllAsync()` from the game's boot sequence. Saves the load, the `Instantiate` and the reparent; **not** the scope or the ViewModel, which are rebuilt on first show. |
| `Runtime/Core/Tooltip/TooltipService.cs` (+ `TooltipViewIndex`, `TooltipPositioner`) | Resident single-instance tooltip owner. Deliberately **off** the nav stack (extends `UIViewBase`, not `UIView<T>`). Timing state machine `Idle→Pending→Shown→Grace` advances in `Tick()` off `Time.unscaledDeltaTime`, so it works at `timeScale = 0` and is frame-testable. Must be registered with `RegisterEntryPoint` or `Initialize`/`Tick` never dispatch. |
| `Runtime/Core/Tooltip/TooltipViewBase.cs` (+ `TooltipView`, `TooltipContent`) | Tooltip view base + the built-in sections view (title/icon/body/stat lines/footer). Subclass and set `_viewKey` for a custom look. Never takes raycasts — re-asserted after both `ShowAsync` and `HideAsync`. |
| `Runtime/Controls/Core/TooltipTrigger.cs` | Raises tooltips from hover / click / focus / touch long-press. Payload from `ITooltipSource` on the widget, or inline title/body. `NotifyContentChanged()` for pooled cells rebound in place. |
| `Editor/Tools/UIFrameworkUIRootUpgrader.cs` | `Tools/UIFramework/Upgrade UIRoot Layers` — adds missing layer children and wires `_layers` on existing UIRoots. Required migration for any project created before a layer was added; also the wiring path the installer wizard never had. |
| `Runtime/Core/DI/UIViewKeys.cs` | `For(Type)` — the single source of load-key derivation, previously duplicated in `UIViewFactory.GetKey` and `UIViewRegistry.AutoRegister` with nothing keeping them in agreement. |

**Persistence (added v1.1.0):** missing save → `null`; a file that is present but not a valid
envelope → recovers from the `.bak`, else throws. `SaveAsync(null)` throws. Renaming a save POCO
orphans its saves (the class name *is* the filename) — prefer an explicit `const string` key for
anything shipped. No flush-on-quit hook exists; `await` saves at your own safe point. Full API and
gotchas in the vault's `Persistence System.md` and `Known Gotchas.md`.

### Installer Wizard
`Packages/com.sinkii09.uiframework/Editor/Installer/UIFrameworkInstallerWizardSteps.cs`

6-step in-Editor setup: installs OpenUPM packages → adds scripting defines → validates DOTween → creates UIRoot prefab → creates config ScriptableObject → creates folder structure.

### Known Issues (2026-08-01 audit)
Full detail: `plans/reports/code-review-260801-2110-uiframework-consolidated.md`. Package is a
git dependency (read-only cache) — fixes land in the upstream repo (`com.sinkii09.uiframework`)
checked out at `e:\Hoc_2025\1_1_2025\com.sinkii09.uiframework`, not in `Library/PackageCache/` here.

**Fixed 2026-08-01, committed as `com.sinkii09.uiframework` v1.2.0** (commits through `03bc885`,
tagged `v1.2.0` and pushed, plan: `plans/260801-2148-correctness-cluster/` in that repo):
- `UIViewFactory` concurrent-creation race — both the default (auto-registration) and manual
  paths now share one dedup guard.
- `GameLifecycleManager` now routes through `UINavigator` instead of bypassing it — the nav
  stack clears and `OnExitAsync` genuinely runs on every transition. `MemoryGame`'s
  `MainMenuViewModel`/`WinViewModel` migrated off the now-removed `IUINavigator.ChangeStateAsync`
  in the same pass, so both features finally share one navigation pattern.
- DOTween cancel-restore — replaced with a `UITransition.RestoreOnCancel` hook called from
  `DOTweenUIAnimator`'s catch blocks instead of a tween callback `AwaitAsync` was clobbering.
- Two related cancellation bugs: `ShowAsync` now propagates `OperationCanceledException` instead
  of swallowing it, and `UIStateMachine`'s cancellation-branch rollback no longer double-exits a
  state.

**Fixed 2026-08-02, committed as `com.sinkii09.uiframework` v1.2.1** (commits `f3c8855`..`3250d46`,
tagged `v1.2.1` and pushed, TheEnd's `Packages/manifest.json` repinned to the tag and re-verified
compiling clean (56/56 PlayMode + 4/4 EditMode), plan: `plans/260802-1122-hardening-cluster/plan.md`
in that repo):
- `ISafeAreaProvider` now has a Null-Object fallback (`NullSafeAreaProvider`, mirrors
  `ITransitionOverlay`/`NullTransitionOverlay`) — a scene missing `SafeAreaProvider` degrades
  gracefully (full-screen rect, warning logged) instead of crashing DI resolution.
- `ViewViewModelCreatorWizard` now checks both target paths before writing either and confirms via
  dialog before overwriting an existing View/ViewModel file.
- `UIViewRegistry.AutoRegister`'s reflection scan recovers loadable types on a partial
  `ReflectionTypeLoadException` instead of discarding the whole assembly's views silently.
- Bonus fix: `Editor.Tools` asmdef was missing a `versionDefines` block for its own
  `defineConstraints`, so the entire assembly (both setup wizards, the wizard above, the custom
  inspector, all menu items) never compiled in any consuming project, silently, since it was
  created — unrelated latent bug found while adding a test assembly for it.

**Fixed 2026-08-02, committed as `com.sinkii09.uiframework` v1.3.0** (commits `8e37734`, `63f2d46`,
`2c3af11`, tagged `v1.3.0` and pushed, plan:
`plans/260802-1358-animation-transition-hardening/plan.md` in that repo) — animation/transition
subsystem audit, 1 CRITICAL + 2 WARNING:
- **CRITICAL:** `LoadingState`'s documented `onLoaded` callback pattern deadlocked
  `GameLifecycleManager` — the callback called back into `GLM.ChangeStateAsync<TNext>` nested
  inside the outer call's still-`true` `_isTransitioning`, silently no-op'ing and leaving the state
  machine stuck on `LoadingState` forever (zero errors, overlay hides normally). No consumer had
  used this pattern yet. `ILoadingContext.OnLoaded` removed (breaking, zero known consumers); new
  `GameLifecycleManager.LoadSceneAndChangeStateAsync<TNext>(scene, ct)` composes "load scene" +
  "enter TNext" as sequential sibling calls instead of nesting them.
- A view's `CanvasGroup.interactable`/`blocksRaycasts` were restored to `true` by
  `DOTweenUIAnimator` immediately after the entrance tween — before `OnShowAsync` finished (or,
  with no show transition assigned, before it even started). Now owned solely by
  `UIViewBase.ShowAsync`, restored only after `OnShowAsync` completes.
- `CanvasGroup.alpha` could get stuck at 0 forever when mixing transition types (e.g. Fade hide +
  Scale show on the same view) — `ScaleTransition`/`SlideTransition` never touched alpha.
  `DOTweenUIAnimator` now unconditionally normalizes alpha after every successful show/hide.

**Still open (deliberately deferred, not yet planned):** two divergent setup wizards
(`UIFrameworkInstallerWizard` vs `UIFrameworkSetupWizard`) with no clear canonical entry point;
machine-global `EditorPrefs` first-run flag; no path-traversal validation on the View/ViewModel
wizard's name field. See `plans/reports/code-review-260801-2110-uiframework-consolidated.md` for
the fuller "New WARNINGs" / "STILL-BROKEN" lists (Phase 3 candidates).

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

## Color Stack Sort Game (`Assets/UIFramework/Features/ColorStackSort/`)

Colour-sorting puzzle (ball-sort / hoop-stack genre). Mobile portrait, Android + iOS.
Levels are **procedurally generated and provably solvable** — none are authored, and no level
data is stored anywhere: a level's identity is its seed.

**Status:** Phases 1–5 of 6 complete (logic core, board presentation, DI wiring + scene, level flow
+ HUD, progression persistence). **Playable in the Editor** — open `Scenes/ColorStackSort.unity`
and press Play for a generated board with a move counter, undo, restart, and a win panel that
advances to a harder next level; the reached level now survives a restart of the Editor.
Phase 6 remaining: juice (UIEffect, particles, SFX).
Plan: `plans/260801-1151-color-stack-sort/`.

`Scenes/ColorStackSort.unity` is in `EditorBuildSettings` as of 2026-08-02 (appended after the two
pre-existing `Assets/Test2/Scenes/` entries, which were preserved).

### Folder Structure

```
Scripts/Logic/              UIFramework.ColorStackSort.Logic.asmdef  (noEngineReferences: true)
  ColorId.cs                readonly struct over byte — stops colour/container index confusion
  Move.cs                   player move (From, To); run length always derived, never stored
  ReverseStep.cs            generator-only step (From, To, Amount) — partial runs allowed
  StackContainer.cs         bounded stack; TopRunLength is the MAXIMAL top run
  BoardState.cs             owns the PLAYER's move rule + IsSolved
  LevelParams.cs            colours / capacity / empties / scramble budget + Validate()
  GeneratedLevel.cs         board + a solution proving it solvable
  LevelGenerator.cs         reverse-scramble generator; owns the LOOSER generator rule
  DeterministicRandom.cs    in-repo PCG32 — see caveat below
    BoardInteraction.cs       tap state machine; the board's ONLY write path + undo history
    MoveRecord.cs             an applied move, with the run length undo needs
    DifficultyCurve.cs        level -> LevelParams, and level -> seed
    TapResult.cs / TapOutcome.cs
Scripts/                    UIFramework.ColorStackSort.asmdef  (framework half)
  Config/
    ColorStackSortSettings.cs base seed + a debug fixed-recipe override; else the curve
  Bootstrap/
    ColorStackSortLifetimeScope.cs  root scope (subclasses UIFrameworkLifetimeScope)
    ColorStackSortBootstrap.cs      IInitializable + IAsyncStartable; enters gameplay
  Progression/
    LevelProgressService.cs   current level (Singleton), persisted via ISaveService; fail-closed
    ColorStackSortSaveData.cs the persisted POCO — one int, explicit SaveKey
  States/
    ColorStackSortGameplayState.cs  IGameState; builds the current level and shows BoardView
  ViewModels/
    BoardArgs.cs              LevelParams + Seed + Level; implements IViewArgs
    BoardViewModel.cs         thin R3 wrapper over BoardInteraction; undo + restart commands
    ColorStackSortWinArgs.cs / ColorStackSortWinViewModel.cs   level-complete panel
  Views/
    BoardView.cs              UIView<BoardViewModel>; owns the animation lock + view lifecycle
    BoardInputRouter.cs       tap/undo -> board intent (extracted from BoardView, Phase 6)
    BoardRenderer.cs          instantiates/destroys tubes and balls; fires the completion burst
    BoardAnimationScope.cs    the per-show cancellation token and its renewal rule
    BoardControlBar.cs        level, moves, undo, restart — inside BoardView, not UILayer.HUD
    ColorStackSortWinView.cs  Popup-layer win panel; Next advances the level
    BoardMoveAnimator.cs      cross-tube ball travel via the overlay (undo replays it backwards)
    TubeView.cs               slots, ball column, lift + reject shake
    TubeFeedback.cs           UIEffect completion sweep + rejected-tap red flash
    BallView.cs               colour holder; ResetTransformTweens + landing impact
    BallPalette.cs            ColorId -> display colour
    JuiceBurstEmitter.cs      UIParticle burst — one shared per board, one per win panel
    ButtonPressPunch.cs       scale punch on press; respects Selectable.interactable
  Editor/                   UIFramework.ColorStackSort.Editor.asmdef
    ColorStackSortPrefabBuilder.cs      create-if-missing prefab generation
    ColorStackSortPanelPrefabBuilder.cs control bar + win panel
    ColorStackSortJuicePrefabParts.cs   sweep / burst / punch wiring (Phase 6)
    ColorStackSortAssetBuilder.cs       UIFrameworkConfig + Settings assets
    ColorStackSortSceneBuilder.cs       UIRoot, 5 layers, EventSystem, camera, wiring
    UiPrefabFactory.cs
Configs/                    UIFrameworkConfig.asset (LoaderMode=Resources), Settings asset
Scenes/ColorStackSort.unity playable scene
Prefabs/                    Ball.prefab, Tube.prefab  (serialized refs, not views)
Resources/ColorStackSort/   BoardView.prefab, ColorStackSortWinView.prefab
                            — paths MUST match each [UIViewKey]
Tests/Editor/               UIFramework.ColorStackSort.Tests.asmdef  (EditMode)
  FakeSaveService.cs        in-memory ISaveService; mirrors the real key + cancellation contracts
```

### Key Architecture Notes

- **Two assemblies, deliberately.** The logic half sets `noEngineReferences: true`, making "no
  engine types in the game's brain" a compile error rather than a convention. Payoff: the core
  compiles in a plain `dotnet` console project (parameter sweeps, mutation testing) and its tests
  are EditMode forever — PlayMode has wedged the Unity-MCP bridge before.
- **Two move rules that must never be merged.** `BoardState.IsLegal` is the player's (whole
  maximal top run only). `LevelGenerator.IsLegalReverseStep` is the generator's and is
  deliberately looser (partial runs), which is what keeps the scramble invertible. Collapsing
  them produces unsolvable levels. Both have their own tests.
- **Solvable by construction, not by search.** Levels are scrambled backwards from a solved board
  with steps that each invert to one legal player move; the recorded inverses are a guaranteed
  solution. Guaranteed-solvable is *not* un-losable — a player can still legal-move into a dead
  board. That is answered by undo in Phase 4.
- **`ScrambleSteps` is a budget, not a difficulty dial.** A longer random walk is not a harder
  puzzle and solution length is not a difficulty measure. Phase 4's curve must move board *shape*
  (colours, capacity, empties).
- **Own PRNG on purpose.** `System.Random` is not stable across .NET versions, and Unity
  Mono/IL2CPP differ again. Since level identity is the seed with nothing stored, a runtime change
  would silently rewrite every level in the game. `DeterministicRandom` (PCG32) is pinned by
  golden-value tests, verified identical under .NET 10 and Unity Mono.
- Editor-only code has its own nested asmdef (as `MemoryGame` does). Note `AircraftStriker`
  does *not* — its `Scripts/Editor/` sits inside a non-Editor asmdef, a latent player-build risk.
- **Root scope, not child scope.** `ColorStackSortLifetimeScope` subclasses
  `UIFrameworkLifetimeScope` (MemoryGame pattern) rather than parenting a child scope to it
  (AircraftStriker pattern). The feature registers *no* scene MonoBehaviours — the whole board is
  inside the prefab — so the child-scope `SetScopeContainer` lifecycle would be overhead with
  nothing to gain. Root registrations are visible to per-view child scopes anyway.
- **The view key is namespaced on purpose.** `UIViewRegistry` scans every assembly in the AppDomain
  and *drops* a second view sharing a key. `[UIViewKey("ColorStackSort/BoardView")]` must match the
  path under `Resources/` exactly — `ResourcesUILoader` passes the key verbatim to
  `Resources.LoadAsync`, with no prefix. An unqualified `BoardView` would eventually collide with
  another feature.
- **`[Preserve]` on view, ViewModel, state and bootstrap.** All four are reached only reflectively
  (registry scan, VContainer). Under IL2CPP — which this game targets — stripping them fails in a
  way Editor Play can never reproduce. Fully qualify it: VContainer ships its own
  `PreserveAttribute` and the short name is ambiguous.

---

## AstralChorus (`docs/astral-chorus-gdd.md`) — content layer built, no gameplay yet

Offline waifu-collection turn-based RPG. GDD complete 2026-09-19. Code has started:
`UIFramework.AstralChorus.Logic` (engine-free economy simulator, 45 tests) and, from 2026-09-20,
the content pack registry plus the game's first runtime assembly and LifetimeScope (58 tests).
**No gameplay, no views, no scene yet.**

- **Design docs:** [`docs/astral-chorus-gdd.md`](astral-chorus-gdd.md) (425 L) and
  [`docs/astral-chorus-content-architecture.md`](astral-chorus-content-architecture.md) (386 L).
  Written in Vietnamese prose with English API/system names. **These two are canonical.**
- **Illustrated read-only companion:** [`docs/astral-chorus-gdd.html`](astral-chorus-gdd.html) — a
  standalone page (no build step, no network except Google Fonts) with SVG diagrams for the core
  loop, element cycle, Aria triangle, Chorus formation and the UI layer collision, plus charts for
  the pull distribution and the Echo Fragment conversion chain. It duplicates content from the two
  markdown files, so **it goes stale unless edited alongside them** — when they disagree, markdown
  wins. Also published as an Artifact at https://claude.ai/artifact/LHWVjYqdEqa6YrEQ87wngc (private
  unless shared); republishing that URL and editing this file are two separate actions.
- **Shape:** fully offline (no server, no IAP), mobile portrait 1080x1920, 14 characters across 3
  rarity tiers, one turn-based engine with a swappable resource layer. Three battle **formats**, a
  closed set owned by core: **Aria** (1v1, Momentum), **Gauntlet** (Bo3 — three Aria rounds, HP
  carries, Momentum resets, swap between rounds), **Chorus** (3v3, Link gauge + front/back).
  Chapter story with dialogue scenes.
- **Format vs Mode** is the axis of the modular design: a *format* is how one battle resolves (core,
  closed); a *mode* is what a run of battles means (pack, open). Phase 1 modes are offline
  translations of the usual gacha menu — The Ascent (tower), Standing Colossus (world boss), Echo
  Duel (arena). Guild war and PvP are cut: they need other players, not a server.
- **Central design rule:** gacha is a *reward schedule*, not an economy. Pull currency is finite and
  authored; the farmable currency must never convert into it.
- **Content packs:** a pack is a folder + one `ContentPackManifest` SO, data-only, no asmdef. Core
  never names a pack; everything resolves through `packId:entityId` string IDs. Build-time blocking
  is logic-layer only (a profile SO holding **IDs, not object references**); byte-stripping via
  Addressables is deferred to Phase 2.
- **Known build items the framework does NOT provide:** a multi-column collection grid
  (`RecyclerView` is single-axis only), a dialogue system, gacha/RNG, currency/inventory, turn-based
  scaffolding, autosave. Plus: the repo still has **zero character art**.
- **First task when implementation starts:** a Monte-Carlo economy sim in the engine-free `Logic`
  assembly. The GDD's economy tables are calculated, not playtested, and review caught two economy
  defects in the first draft.

**GildedLedger is deprioritized** — its GDD is kept as reference, implementation is not planned.

---

## Recent Changes

Moved to [`changelog-summary.md`](changelog-summary.md) on 2026-09-20, when this file hit 1,867
lines against an 800-line cap and the log was 78% of it. **This file is the current state; that
one is the history.** New entries go there.

## Code Standards
- DOTween↔UniTask bridge: always use `UniTaskCompletionSource` pattern (no `UNITASK_DOTWEEN_SUPPORT`); always dispose `CancellationTokenRegistration`
- DOTween tweens: always `SetUpdate(true)` so they survive `Time.timeScale = 0`
- Animations inside LayoutGroup: use `DOScale`, never `DOAnchorPosY`/`DOMove`
- Pre-hide animated views in `Awake` (`localScale = Vector3.zero`) to prevent first-frame flash
- `UIViewBase.HideAsync` order: UITransition FIRST → `OnHideAsync` SECOND. `OnHideAsync` runs after view is visually hidden — use for cleanup/reset only, not visible animations. Visible hide animations must live in a `UITransition` subclass assigned to `_hideTransition`
