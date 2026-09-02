# Codebase Summary

## Project Overview
Unity 6 project combining the Sinkii09 UIFramework package (MVVM + DI) with a Memory Flip Card Game that demonstrates full framework integration. The game is complete and playable with sound, animations, and win detection.

## Package Dependencies (`Packages/manifest.json`)
- **com.sinkii09.uiframework** — **pinned git tag** as of 2026-08-29:
  `"https://github.com/sinkii09/com.sinkii09.uiframework.git#v1.6.0"`.
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

## Recent Changes

### 2026-09-02 — Coalesced bindings + render suspend (UIFramework v3.0.0, Phases 2 & 3)

Phases 2 and 3 of the Melvor-patterns sprint, released together as the **breaking** `v3.0.0`.
TheEnd repinned to `#v3.0.0` and verified against the published tag: **EditMode 276, PlayMode 300**.

**What changed for this project.** UI writes now coalesce to one per rendered frame. Of TheEnd's 28
binding call sites, **21 change timing** — 20 `BindToText` plus 1 `BindToFillAmount`, all pure
display. The other 7 (`BindToActive` ×3, `BindToInteractable` ×2, `BindTwoWay` ×2) stay immediate by
default. No call site needed editing; the new `UIBindMode` parameter is optional and last.

The rule behind the defaults: **display paths coalesce, input paths do not.** A coalesced write
lands up to a frame late — invisible on a score label, a correctness bug on anything gating
interaction. A late `SetActive(false)` leaves an object raycastable for the rest of the frame; a
late `interactable = false` leaves a button clickable; and `BindTwoWay`'s `text != v` guard
evaluated at flush time would overwrite text the user typed that frame and move their caret.

The **first** value of every binding still applies synchronously, so views built through the async
loader path never flash unbound state.

`IUIRenderScheduler.Suspend()` also ships (refcounted, `IDisposable`-scoped) but is **EXPERIMENTAL
and unused here** — TheEnd has no offline-catch-up loop. The framework deliberately never calls it
itself: the scene load runs inside `LoadingState` while that state's view is on screen, and the
transition curtain may be `NullTransitionOverlay` or semi-transparent, so it cannot know suspending
is safe.

**Migration risk to watch in this project:** set-then-read layout. Reading `preferredWidth`, or
driving `LayoutRebuilder` / a content-size fitter immediately after a bound write, now sees stale
geometry — pass `UIBindMode.Immediate` at those sites. Nothing in TheEnd currently does this.

**Framework-side note worth carrying:** R3 1.3.1's `ThrottleFirstLastFrame` is defective — once a
window has emitted a trailing value, a lone value in the next window emits correctly *and* the
trailing edge fires again carrying `default(T)`. In a binding that is a score label flashing "0".
`ThrottleLastFrame` is clean. Do not "simplify" the coalescing operator into it.

### 2026-09-02 — Notification / toast service (UIFramework v2.2.0, Phase 4)

Fourth phase of the Melvor-patterns sprint. The framework had no toast system; it now has
`INotificationService` with identity-keyed merging (repeated events edit one row's quantity instead
of stacking), priority ordering, and a resident scene-owned host on a new `UILayer.Notification`
between `Tooltip` and `Overlay`. Built in the `TooltipService` shape, so toasts never enter the
navigation stack.

Additive — no existing signature changed — but **one manual step is required in this project**:
`UIRootLayerRefs` serialises by field name, so TheEnd's existing UIRoot deserialises `Notification`
as null. **Run `Tools/UIFramework/Upgrade UIRoot Layers` once.** Until then the service falls back
to the Overlay layer and logs one error naming the command; toasts work but draw over the loading
curtain. Note the upgrader skips prefab variants and only scans open scenes.

`UILayer` also gained explicit spaced values (`HUD = 0 … Debug = 400`) so future inserts never
renumber — previously every insert was a silent renumber, safe only because no `UILayer` value is
ever serialized (re-audited across both repos before the change).

Design points worth knowing before touching it, all documented at length in the vault note:
- **Visibility is sticky.** Once a toast is on screen it holds its slot until it expires or is
  dismissed; an arrival never displaces it at any priority. Priority orders the *waiting* set.
- **The dismiss timer runs only while visible** and pauses behind the loading curtain — but not on
  the Overlay fallback, where the toast is plainly visible over it.
- **`MaxLifetime` never pauses and is never reset by a merge.** It is the guarantee an entry
  terminates; pausing it would let an overlay that never hides make every entry immortal.
- **No async anywhere.** Fades are a synchronous per-slot state machine advanced in `Tick`, which
  makes the cancelled-tail hazard (a superseded hide stranding an invisible toast in a live slot)
  inexpressible rather than merely guarded against.

27 new PlayMode tests. Suites green against the published tag: **276 EditMode / 259 PlayMode**.
Plan and three review rounds: `plans/260902-phase4-notification-service/plan.md`.


### 2026-09-02 — Navigation queue (UIFramework Phase 1b, pending release as v2.1.0)

Third phase of the Melvor-patterns sprint. Where Phase 1a made refusal *visible*, this makes it
*avoidable* for callers that cannot wait. `GameLifecycleManager` gains three fire-and-forget
methods — `EnqueueStateChange<T>`, `EnqueueRestart`, `EnqueueSceneLoad<TNext>` — that run now if
idle and otherwise after the in-flight transition finishes, instead of being refused.

Purely additive: the four existing awaitable entry points are behaviourally unchanged, so nothing
in TheEnd needs to migrate. New `Runtime/Core/Lifecycle/NavigationRequestQueue.cs` (single-consumer
FIFO, main-thread only, GLM-only — the navigator is deliberately not a participant, since GLM calls
into it). `GameLifecycleManager` also became `IDisposable` so scope teardown drops queued requests
and cancels the one in flight.

Two things drove the design and are documented at length in the vault note, because both are easy
to "fix" into breakage:

- **The `Enqueue*` methods return `void` deliberately.** A queued request runs after the current
  one, so a caller running *inside* the current one (a state's `OnEnterAsync`, a view's
  `OnHideAsync`) would block the drain forever if it could await its own request. That hazard
  cannot be detected — UniTask does not flow `ExecutionContext` — and every flag wide enough to
  catch it also rejects nearly everything. Returning `void` makes it inexpressible.
- **The drain waits for idle before invoking each item.** The entry points keep their own guards,
  so a queue without that wait drains straight into rejections while looking like it works. The
  idle predicate covers GLM's flag, `UINavigator.IsTransitioning`, and a `_hasStarted` boot gate.

Not shipped: `NavigationResult.Cancelled`, which the sprint plan had slated for this phase. With a
void API there is no result channel, so it would be unobservable; deferred.

20 new PlayMode tests. Suites green at **276 EditMode / 232 PlayMode** against the local package.
Plan and three review rounds: `plans/260902-phase1b-navigation-queue/plan.md`.

### 2026-09-02 — Navigation reports refusal (UIFramework v2.0.0, Phase 1a)

Second phase of the Melvor-patterns sprint. Every guarded navigation entry point now returns
`NavigationResult { Completed, Rejected }` instead of a bare `UniTask`.

**The defect.** Navigation guards drop requests that arrive mid-transition, but returned a `UniTask`
that completed normally — so an awaiting caller could not tell "the view is on screen" from "your
request was discarded", and carried on updating its own state as though it had happened. Two guards
refused with no log whatsoever: `UINavigator.HideAsync`'s transitioning check, and
`GameLifecycleManager.RestartCurrentStateAsync`'s null-current-state check (reachable whenever a
Retry button is wired up before `StartAsync` — symptom: a button that does nothing, silently).

Refusal turned out to have **more sources than the transition guard**: `NavigationStack` also
declines a push past `MaxNavigationDepth` and a pop on an empty stack. Both were reporting success.

**This is source-breaking.** `await nav.ShowAsync<T>()` and `.Forget()` still compile; what breaks is
*returning* the task directly from a `UniTask`-returning method — `=> _navigator.ShowAsync<X>(ct);`
in an `IGameState.OnEnterAsync`. Four such sites in TheEnd were migrated to `await` (one also needed
`async` added): `MemoryGameState` (×2), `ColorStackSortGameplayState`, `AircraftGameplayState`.

The diff review caught a **critical** the implementation missed: `ShowAsync` already *detected* a
declined push — it re-derives layer blocking on exactly that branch — and then returned `Completed`
anyway, leaving the headline bug alive on the most-used entry point. Following that up found the
same false-success in `PopAsync`, which discarded `NavigationStack.PopAsync`'s null return. Also
fixed: `StartAsync` and `LoadSceneAndChangeStateAsync` were discarding results; `Cancelled` was
dropped from the enum as unreachable (cancellation still throws) pending the queued path.

EditMode **276 passed**, PlayMode **212 passed** (205 baseline + 7 new).

### 2026-09-01 — Fail-fast view validation (UIFramework v1.9.0, Phase 5)

First phase of the Melvor-patterns sprint (plan: `C:\Users\user\.claude\plans\wiggly-exploring-candle.md`;
execution order 5 → 1a → 1b → 4 → 2 → 3 → 6). Released **separately** from Phase 1a rather than
paired with it as the plan originally had: 1a turned out to be source-breaking, and this phase is
purely additive, so it did not deserve to be buried inside a major bump.

- **`UIViewValidator`** (new) — reports every unassigned `[SerializeField]` `UnityEngine.Object`
  reference on a view in one error naming the fields, so a misconfigured prefab is identified at
  `Awake` instead of surfacing later as a `NullReferenceException` inside a binding lambda whose
  stack trace points at framework code. Editor/development builds only; call sites are stripped by
  `[Conditional]` and the body compiles away in a release player.
- **`[UIOptional]`** (new attribute) — exempts fields where null is meaningful. Applied to
  `UIViewBase._showTransition`/`_hideTransition`; without it every transition-less view in every
  consuming project would be reported.
- **`UIViewBase.Awake`** calls the validator; **`UIViewFactory`** repeats the call after
  `Instantiate` as a backstop, because `Awake` is `protected virtual` and an override that forgets
  `base.Awake()` would otherwise skip validation silently. Reports are deduped per view type.

Reviewed post-implementation. The review produced four warnings, all resolved: the "every view is
covered" claim was false (the `virtual Awake` bypass above); repeated logging per instance; a
`readonly` false positive; and — the important one — **nothing pinned the two `UIViewBase` edits**,
so deleting `[UIOptional]` would have broken every consuming project with the suite still green.

Two traps worth remembering. Unity does **not** call `Awake` on `AddComponent` outside play mode, so
the first integration test written for that gap passed vacuously; it now invokes `Awake` reflectively.
And the `[UIOptional]` guard was then **mutation-tested** — removing the attribute turns the suite
red on exactly that test, and restoring it returns 9/9 — because a test written to catch a regression
is worth nothing until it has been seen to fail.

EditMode **276 passed, 0 failed** (267 baseline + 6 + 3 new).

### 2026-09-01 — Async lifetime utilities (UIFramework v1.8.0)

Two additive API pieces plus one documentation-only change. Theme: async work started from UI must
have a defined owner, cancellation point, and restore-on-exit. Nothing existing changed signature.

- **`ViewModelBase.ShowToken`** — per-show `CancellationToken`, the token half of
  `_showDisposables`. Cancelled on hide, fresh on next show. Closes a real gap: a ViewModel had a
  per-show disposable bag but no per-show cancellation, so work begun in `OnShow()` ran until the
  ViewModel was disposed — for a cached view, possibly never. Views were already covered
  (`OnShowAsync`/`OnHideAsync` take a `ct`).
- **`UIBindingExtensions.BindButtonAsync`** — async click handler with a re-entrancy guard.
  Previously the house style was a sync `UnityAction` firing `.Forget()`, and nothing stopped N
  presses launching N concurrent operations. `disableWhileRunning` defaults to `false` (the guard is
  the correctness mechanism; disabling the button is cosmetic and conflicts with
  `BindToInteractable`).
- **`TabIndicator`** — comment only, no behaviour change. Its bare `DOKill()` is correct today
  because it tweens `anchoredPosition` only; the comment names the invariant so the next person
  adding a scale/colour tween there does not silently create a corruption site.

Reviewed twice (plan + diff). The diff review caught three defects that compiled clean and passed
tests: a throwing per-show disposable could strand the ViewModel on a cancelled token permanently
(`BindButtonAsync`'s own listener-removal disposable makes that reachable —
`Button.onClick.RemoveListener` throws once the Button is destroyed); the `running` guard could
latch forever if the prologue threw; and `NotifyHide` lacked the re-entrancy guard `Dispose` had.

Released as **v1.8.0**, tagged and pushed. TheEnd repinned to
`https://github.com/sinkii09/com.sinkii09.uiframework.git#v1.8.0`, and both suites re-run against
the pushed tag rather than the local working copy: EditMode 267 passed, PlayMode 205 passed.

### 2026-08-31 — Tooltip system (UIFramework, targeting v1.7.0)

New `Runtime/Core/Tooltip/` (13 files) + `TooltipTrigger` control + `UIFrameworkUIRootUpgrader`
editor command. Four input sources (hover / click / focus / touch long-press), two content models
(built-in `TooltipContent` sections view, or a project's own `TooltipViewBase` prefab by key).

**`UILayer` gained a `Tooltip` member, inserted between `Popup` and `Overlay`** (sortOrder 250).
The ordinal is load-bearing (`BlockLayersBelow` compares `(int)layer`), so this was only safe
because a grep of both repos confirmed no `UILayer` value is serialized anywhere — every use is a
code-level `override Layer => UILayer.X`. Re-verify that before inserting another.

`UINavigator` gained a **trailing-optional** `ITooltipService` parameter (after `UIBackdrop`) and
calls `HideImmediate()` on `ShowAsync`/`CloseAllAsync`/`ChangeStateAsync`. Trailing-optional
mirrors the existing `UIBackdrop` precedent so the positional `new UINavigator(...)` calls in tests
keep compiling — it is *not* an optional dependency; VContainer ignores C# defaults, so any
hand-built container must register one.

**Migration required for existing projects:** run `Tools/UIFramework/Upgrade UIRoot Layers`. An
existing UIRoot deserialises `Tooltip` as null and `SetLayerInteractable` returns *silently* on a
null transform, so the failure would be invisible. The service falls back to the Overlay layer with
a one-shot error if the layer is still missing.

Gates: compile clean; **EditMode 257 passed, PlayMode 197 passed** (baseline 165 + 32 new tooltip
tests). Three review gates ran; the plan gate and its delta gate together corrected 9 design
defects before implementation, and the diff gate found 3 CRITICALs after. See
`plans/260830-1206-tooltip-system/plan.md` and the four reports in `plans/reports/`.

### 2026-08-30 — UI/EnergyCoreOrb sample shader (`Assets/UIFramework/Samples/`)

Added `UIEnergyCoreOrb.shader` + six per-tier materials (`UIEnergyCoreOrb_qua{3..8}_mat.mat`).
Fully procedural emissive orb — soft halo, ridged swirling filaments on a fake sphere remap,
pulsing core with starburst arms, twinkling sparks, rim band. Additive (`Blend One One`), no
`_MainTex` sample, UV from `IN.texcoord`. Replaces the 16-frame-per-tier flipbook
(`item_cell_qua/qua{3..8}_{1..16}.png`, 96 PNGs / ~3.2 MB) from the Phoenix reference dump, which
stays untouched. Palettes were sampled from the reference pixels, not eyeballed — which revealed
two tiers are structurally different effects, hence `_CoreIntensity` (0 = qua3's hollow ring) and
`_Prismatic` (qua7's iridescence).

Honest trade-off: this is *more* GPU than the flipbook (~150 ALU/px vs one texture fetch). The win
is atlas size, runtime recolor and resolution independence. An early-out (`r > cullRadius` →
return 0) skips ~21% of the quad to keep an inventory grid affordable.

Built on `UIAnimatedGlow.shader`'s reviewed CGPROGRAM boilerplate. Two reviewer gates (plan +
diff). The plan gate killed the seam fix outright: `[IntRange] _Strands` works only when the
angular coord is normalised 0..1, so `atan2` was removed from the noise domain entirely and the
swirl is a Cartesian rotation — which also removes the need for any `fmod` time wrap, since
rotation preserves magnitude. Diff gate found 1 CRITICAL (rgb multiplied by shape twice, driving
the halo to ~1e-4) plus a dead `_RimRadius` in both materials. Both fixed and re-verified.

Verified via `unity-mcp-cli run-tool` (the session's MCP tools were down): `ShaderHasError` false,
`isSupported` true, `GetShaderMessageCount` 0, all six materials bound; plus an offscreen
`RenderTexture` render of every tier compared against the reference frames.

**Usage constraint (in the shader header too):** Image `Source Image` must be EMPTY and Type =
Simple — a null sprite is what guarantees a 0..1 rect UV. The RectTransform must be square; there
is no aspect correction, so a non-square cell renders an ellipse.

### 2026-08-29 — UIFramework v1.6.0 released, project repinned

Shipped a four-feature sprint as **v1.6.0** (`56ccf20`), merged to `main`, tagged and pushed.
`Packages/manifest.json` repinned from the temporary `file:../../com.sinkii09.uiframework` dev path
back to the git tag. Verified against the **tagged** package, not the local path:
**EditMode 257/257 · PlayMode 165/165** (PlayMode was 105 at v1.5.0 — the sprint added 60).

What landed, all **default-OFF** so this project's behaviour is unchanged until the new config
assets are authored:

- **`UIViewPolicyConfig`** — per-view `Resident` / `NeedsBackdrop` / `PreloadOnBoot` flags, keyed by
  the view's **load key** (its `[UIViewKey]` value, else the class name) because a ScriptableObject
  cannot serialize a `Type`. Assign on `UIFrameworkLifetimeScope`; Inspector-only, no Resources
  fallback. Boot-time validation warns on any key matching no registered view.
- **Timed cache eviction** — `UIViewCacheSweeper` destroys views idle past
  `UIFrameworkConfig.ViewCacheGraceSeconds` (`0` = disabled, the default). Before this,
  `UIViewFactory._cache` only ever grew: every view a player opened held its GameObject and its
  loader handle for the whole session.
- **`UIBackdrop`** — one reusable dim `Image` under any view whose policy sets `NeedsBackdrop`.
- **`UIViewPreloader`** — warms `PreloadOnBoot` views. Nothing preloads automatically; the game calls
  `PreloadAllAsync()` itself.

Two latent `UINavigator` bugs were closed with the backdrop, because a full-screen raycast blocker
turns both from invisible raycaster mis-toggles into softlocks: a push declined at
`MaxNavigationDepth` (which warns and returns rather than throwing) left blocking applied for a view
never pushed, and a view deactivated by a failed hide was still top-of-stack when the navigator
refreshed against it.

**If this project adopts eviction**, note the trap: any view held directly from the factory (the
HUD-channel pattern) must be marked `Resident`, or it is destroyed once hidden past the grace period
and the held reference becomes a Unity-null. Framework docs: the Obsidian vault's
`View Policy & Caching.md`.

### 2026-08-29 — UI/BorderTraceFrame: per-tier color + prismatic shimmer (`Assets/UIFramework/Samples/`)

Follow-up to the same-day `UIBorderTraceFrame.shader` addition below: replaced the always-on
rainbow hue-cycle (`_HueSpeed`, removed) with a static, per-tier-configurable `_FrameColor`,
plus an optional `_PrismaticShimmer` (0..1) blend toward an animated multi-hue sweep for the
highest rarity tier — hue varies by **perimeter position** (`perim`, reused from the comet
math) as well as time, so several colors are visible around the ring at once instead of the
whole ring flashing one color. `UIBorderTraceFrame_mat.mat` migrated via `script-execute`
(`SerializedObject` array-delete) to drop the orphaned `_HueSpeed` entry Unity doesn't
auto-prune from `.mat` YAML when a shader's properties change, and given starter values for
the new properties.

Tier 1 diff review (code-reviewer) verified 6 design claims (removal completeness, spatial-vs-
time-only shimmer, single shared `perim`, intensity applied once, half-precision discipline,
downstream premultiply/clip untouched) all held, then caught 2 WARNINGs: `_ShimmerFrequency`
was a continuous `Range(1,8)` float, but the shimmer hue math (`frac(perim*freq + t*speed)`)
is only continuous across the perimeter's `atan2` wrap point when `freq` is a whole number —
a non-integer value paints a hard color seam down one edge; fixed with `[IntRange]`. The
sample material still carried the stale `_HueSpeed` value and had none of the new properties
set (would ship as a flat, non-demonstrating default) — fixed via the migration script above.

### 2026-08-29 — UI/BorderTraceFrame sample shader (`Assets/UIFramework/Samples/`)

Added `UIBorderTraceFrame.shader` + `UIBorderTraceFrame_mat.mat`: a procedural neon square
frame with two bright comet highlights traveling around its edge, 180° apart, moving together.
Ignores `_MainTex` content entirely (pure geometry-driven effect) — drops onto any UI `Image`
whose sprite spans 0..1 UV across the rect (Simple image type, no atlas/9-slice; documented in
the shader header). Border-trace-only rebuild, by user request, of the never-shipped
`UI_QualityGlow_Qua7.shader` scratchpad prototype from the external-game reverse-engineering
session earlier this session — orb-fill and sparkle layers dropped (YAGNI), only the
edge-traveling highlight kept, doubled to two comets instead of one.

Tier 1 diff review (code-reviewer) round 1 caught 1 CRITICAL (`OUT.localUV = v.texcoord`
silently breaks for atlased/9-sliced sprites — the common default-Image-setup case — documented
as an explicit usage constraint rather than solved generically, since deriving true rect-relative
UV for arbitrary Image Types needs data a stock shader doesn't have) and 4 WARNINGs: dead outer-
ring smoothstep (`ringOuter` sat exactly at the UV boundary `boxDist` never exceeds, so the outer
edge hard-clipped instead of fading — fixed by insetting `ringOuter` by `_FrameSoftness`); comet
tail width unclamped (`width*3` could exceed the 0.5 wrap point and seam against the lead width —
clamped to 0.49); a self-inflicted `fmod(t,100)` time-wrap pop (appropriate for the prior
noise-domain-warp shader, wrong here — removed, since this shader has no noise domain requiring
bounded precision); and an `atan2(0,0)` NaN at the exact rect centre (epsilon-guarded). All fixed
and re-verified compile-clean (`assets-shader-get-data` → `HasErrors: false`).

### 2026-08-29 — UI/AnimatedGlow sample shader (`Assets/UIFramework/Samples/`)

Added `UIAnimatedGlow.shader` + `UIAnimatedGlow_mat.mat`, animating the existing static
`show1_glow_00067_tex.png` (no companion flipbook frames) via two independently-scrolling
domain-warp noise layers + a breathing brightness pulse, additive-blended for a light-emitting
UI glow look. Standalone sample asset — doesn't touch the UIFramework package or any feature.

Tier 1 diff review (code-reviewer) caught one CRITICAL (premultiply happened before the
`RectMask2D`/`UnityGet2DClipping` factor was applied, so the glow ignored scroll-view/mask
clipping entirely under `Blend One One`) and 6 WARNINGs — most notably `fixed4`/lowp overflow
past ±2 on GLES/mobile from the brightness×pulse chain, and brightness/pulse being folded into
the alpha used for premultiply (squaring both once multiplied through). All fixed; also caught
one more instance of the same return-type-precision mistake in self-verification after the first
round of fixes (`frag`'s return type was left as `fixed4`, silently undoing the half4 conversion
at the final return). Shader compiles clean (`HasErrors: false`) post-fix.

### 2026-08-23 — UIFramework v1.5.0 released, project repinned

Shipped the RecyclerView Phase 2 work below as **v1.5.0** (`d4d299c`), tagged, pushed, GitHub release
created. `Packages/manifest.json` repinned from the temporary `file:../../com.sinkii09.uiframework`
dev path back to the git tag; lock hash `d4d299c`.

Verified against the **tagged** package, not the local path: **EditMode 257/257 · PlayMode 105/105**.

Both runs used Unity **batch mode** (`-batchmode -runTests`) because the MCP bridge was down — the
plugin hosts the server and it had not started this session. Note `-quit` must **not** be combined
with `-runTests`: Unity honours the quit and shuts down before running anything, producing a log that
ends in "Batchmode quit successfully invoked" and no results file.

### 2026-08-22 — RecyclerView Phase 2: variable cell size

`SetItemSizeProvider(Func<int,float>)` and `RefreshSizes()` on `RecyclerView`. Sizes are **declared,
never measured** — the view asks before it binds, so content extent and every cell position are exact
from the first frame and the list never shifts to correct an estimate. SuperScrollView instead
measures-then-corrects, and most of its complexity lives in that machinery.

There is deliberately no per-index size setter. An earlier draft had one; it was silently discarded
by the next `SetItemCount` (the rebuild re-asks the provider), and making it persist would have been
worse — an override keyed by index outlives the item it was meant for the moment the list's contents
shift, rendering the wrong row tall. One source of truth instead: keep size in the consumer's data,
call `RefreshSizes()`.

Smaller than expected: Phase 1 had pre-built the scaffolding (`CellHandle.DeclaredSize`,
`WindowState.HeadSize`/`TailSize`) and **`RecycleWindow.Decide`/`NeedsReseed` never referenced
stride**, so the recycling core needed no change at all. Only the offset *supply* was hardcoded —
9 call sites, now behind a pure `IItemOffsets`.

Three latent bugs surfaced while implementing, each invisible under uniform sizing:

- `ContentLayout.ConfigureCell` sized cells once per `Instantiate`, so a pooled cell kept the size of
  whichever index first created it.
- `Rebind` wrote no size at all — `RefreshIndex` on a multi-prefab list rendered the refreshed cell at
  its replacement's size. Invisible to any single-prefab test.
- The content rect was rebuilt only by `SetItemCount`, which sufficed only while count was the one
  thing that could move an offset.

**Two more defects passed all 362 green tests** and are the entry worth remembering: a reentrancy
flag that was set but never *read* (found by grepping the symbol, not by testing), and a size
provider that, on throw, stayed installed and made every later `SetItemCount` throw forever — the
test asserted the offset table survived but never called anything afterwards. Both are the same
shape: code that looks like it satisfies a requirement, with the step that actually satisfies it
missing.

Package commit `5d649bb` on `feature/recycler-view-phase2`. **EditMode 258/258 · PlayMode 106/106**,
zero pre-existing test assertions changed — the `UniformOffsets` parity anchor held.

Released as **v1.5.0** — see the entry above.

### 2026-08-18 — UIFramework v1.4.0 + v1.4.1 released, project repinned

Shipped the RecyclerView work below as **v1.4.0**, then **v1.4.1** as a packaging fix: three
orphaned `.meta` files (`Runtime/Core/Pooling.meta`, `Runtime/Resources.meta`,
`Runtime/Resources/UIFramework.meta`) outlived their folders and made every consuming project warn
on import — git cannot store an empty directory, so Unity found each `.meta` without its folder,
recreated it, and warned. Both tagged, pushed, and published as GitHub releases (the repo had none
before).

`Packages/manifest.json` moved off the local `file:` development pin onto
`…com.sinkii09.uiframework.git#v1.4.1` (lock hash `169d0c0`). Verified against the packaged copy:
EditMode 62/62, PlayMode 90/90, no import warnings.

> `package-add` with a git URL exceeds the unity-mcp-cli 60s tool timeout and reports
> `Tool call timed out` — the operation still completes. Check `manifest.json`, `packages-lock.json`
> and `Library/PackageCache/<pkg>@<hash>` rather than trusting the error.

### 2026-08-18 — RecyclerView Phase 1: first test run, 18 failures, 1 CRITICAL fix

**In-flight feature completed and verified.** The 51 tests written alongside `RecyclerView` had
never actually been executed. Running them surfaced 18 failures — all test-side — plus a review
that found one genuine CRITICAL in the runtime:

1. **10 CellPool failures: a MonoBehaviour cannot live in an editor-only assembly.**
   `AddComponent<TestCell>()` returned null because `Tests/Editor/` compiles with
   `includePlatforms: ["Editor"]`; Unity refuses to attach such scripts, and the error names the
   *file*, not the class. `CellPool.DestroyAll` also calls `Object.Destroy`, illegal in edit mode.
   Pool tests moved to the PlayMode assembly. Rule: **a test needing a real GameObject with your own
   MonoBehaviour on it is a PlayMode test.**
2. **8 ScrollAxis failures: a wrong XML doc became a wrong test.** `ViewportStart` was documented as
   the inverse of `ToLocal`; it is the negative — one places cells inside the content root, the other
   reads the content root's own position, and content travels the opposite way. Tests rewritten
   table-driven against concrete per-direction positions; doc corrected.
3. **CRITICAL (runtime): the pump's iteration cap was a fixed 64.** A reseed grows the window one
   cell per iteration, so a 1920px viewport with 30px rows needs ~77 — past the cap the pump logged
   an error and abandoned the tick every frame, leaving a permanently under-filled list. Now derived
   from geometry via `RecycleWindow.MaxIterationsFor`. The prior design memo had rejected
   SuperScrollView's 9999 bail-out as "never having proved termination" — 64 was the same unproven
   bound, only tight enough to actually hit.

Also closed: `SetCellProvider` now pumps; `RentCell` refuses out-of-provider and double-rent calls
(both leaked silently); a provider that rented then returned null no longer loses the cell;
`RefreshIndex` on a throwing provider no longer strands a staged cell; `ScrollToIndex`/
`ForEachShownCell` gained the reentrancy guard the other mutators had.

**Coverage went from 0 integration tests to 19** (`RecyclerViewVirtualizationTests`,
`RecyclerViewContractTests`, `RecyclerViewTestHarness`), covering the ~530 previously untested LOC.
Final: **EditMode 62/62, PlayMode 90/90.**

Added `GameObject > UI > UIFramework > Recycler View` and the `Samples~/RecyclerViewList` consumer.

> Machine note: the Unity test runner failed mid-session with `Failed to create CoreCLR` /
> `GC heap initialization failed`, which reads like a compile error but is system commit exhaustion
> (22.1 GB of a 23.7 GB limit). Reclaiming idle MSBuild worker nodes and restarting the Roslyn
> compiler server cleared it.

### 2026-08-11 — ColorStackSort Phase 7: visual polish fixes (post-playtest bug batch)

**Bug fix, root cause non-obvious (Unity RectTransform anchor-fraction math).** Four playtest
reports, one plan, one batched Tier-2 fix:

1. **Balls stacked from tube center, not bottom.** `ColorStackSortPrefabBuilder.CreateTube`'s
   `Slots` child rect was full tube-height (118×400) with a bottom pivot — but Unity resolves a
   point-anchored child's position against the PARENT'S RECT BOUNDS, independent of the parent's
   own pivot, landing balls ~200 units too high. Fix: `Slots` height → 0, which collapses the
   parent's rect bounds to a single point regardless of any child's anchor fraction. Visually
   confirmed via Play Mode screenshot before/after.
2. **Ball travel was a flat slide.** Replaced with a hand-rolled `DOVirtual.Float` parabola
   (`BoardMoveAnimator.CreateJumpTween`) — deliberately NOT DOTween's own `DOJumpAnchorPos`, which
   a plan reviewer flagged as unverifiable (closed-source DOTween.dll) when nested in an
   already-staggered `Sequence`.
3. **Tube-completion burst and win-panel confetti fired simultaneously.** Added a 0.4s
   `UniTask.Delay` (unscaled, cancellable) — placed INSIDE the win-only branch of
   `BoardView.RunAnimationAsync`, not before it, or it would have taxed every move including undos.
4. **Completion sweep was invisible.** It was rendered through the tube body's 13%-alpha
   background and left at UIEffect's default (unrelated) blue. Fixed: body alpha now flashes up for
   the sweep's duration, and `transitionColor` is tinted to the tube's actual completed colour
   (same value already computed for the burst — `BoardRenderer.CelebrateIfComplete`).

**Process note worth keeping:** the first version of the Fix-1 regression test passed against the
STILL-BROKEN prefab (false positive) because it measured a ball's position against the tube's own
transform, and the tube's pivot is center — not bottom — so "close to zero" looked fine but was
actually "close to the bug." Caught by treating an unexpectedly-green suite as suspicious rather
than trusting it. See memory `unity-anchor-fraction-center-pivot-trap`.

**Post-implementation review also caught:** the hand-rolled jump tween (`DOVirtual.Float`) had no
`SetTarget`/`SetLink`, unlike the `DOAnchorPos` it replaced — meaning it couldn't be found by
`rect.DOKill()` and wasn't auto-killed if its GameObject was destroyed mid-flight. Fixed.

177/177 EditMode green (176 baseline + 1 new). No logic-assembly changes.

### 2026-08-02 — ColorStackSort Phase 6: juice (UIEffect + UIParticle)

**Dependency added:** `com.coffee.ui-particle` pinned `#4.13.0` (bare git URL, no `?path=` segment
— unlike UIEffect). Needed because the board's canvas is `ScreenSpaceOverlay`, which composites
last, so a plain `ParticleSystem` always draws behind the entire UI.

**Added:** `BoardInputRouter` (behaviour-neutral extraction — BoardView was 204 LOC and had to
shrink before it grew), `TubeFeedback`, `JuiceBurstEmitter`, `ButtonPressPunch`,
`ColorStackSortJuicePrefabParts`.

**Modified:** `BallView` (+`ResetTransformTweens`, +`PlayLandingImpact`), `TubeView`,
`BoardMoveAnimator`, `BoardRenderer` (+`CelebrateIfComplete`), `BoardView` (`mayWin` →
`isForwardMove`, now takes indices not TubeViews), `ColorStackSortWinView` (confetti), both prefab
builders, both asmdefs.

Four effects: ball landing squash, tube-completion sweep + colour-matched burst, win confetti,
button punch + rejected-tap red flash. **SFX deliberately out of scope** (user scoped this phase to
"UIEffect, particles").

Non-obvious constraints now encoded in the code:

- **`DOKill(complete:false)` does not restore.** Adding a scale punch retroactively turned every
  bare `DOKill` on a ball into a permanent stuck-scale bug. All four sites now route through
  `BallView.ResetTransformTweens()` (kill **and** restore). The fourth site was non-obvious:
  `SetParent(overlay, worldPositionStays: true)` recomputes `localScale` from the world matrix, so
  the reset has to come *after* the reparent.
- **Celebration is gated on `isForwardMove`** — an undo can leave its destination full and
  monochrome, and celebrating a move being taken back reads as a bug.
- **`UIParticle.Play()` wipes live particles** (`Simulate(0,false,restart:true)`), so bursts use
  `ParticleSystem.Emit()`; `Clear()`/`Stop()` latch `isPaused` permanently, so `OnDisable`→Clear is
  paired with `OnEnable`→Resume; and a Stopped system is not simulated, so `ParticleSystem.Play()`
  runs once on enable. `positionMode = Absolute` is pinned so hand-tuning `scale` cannot start
  dragging live particles.

**Verified:** 176/176 EditMode unchanged; `compilationFailed=False` by reflection over built DLLs;
all three regenerated prefabs carry the expected components. Prefab regeneration was safe to
automate because `Tube.prefab`'s serialized fields and `BoardView.prefab`'s palette were confirmed
identical to their code defaults first. **Play Mode visual QA still outstanding.**

### 2026-08-02 — ColorStackSort Phase 5: progression persisted via `ISaveService`

**Added:** `ColorStackSortSaveData` (persisted POCO), a rewritten `LevelProgressService`
(load/save/clamp), `FakeSaveService` + `LevelProgressServiceTests` (17 tests).
**Modified:** `ColorStackSortBootstrap` (awaits the load *before* the first state change),
`ColorStackSortGameplayState` (reads the level per entry), `ColorStackSortWinViewModel`,
`UIFramework.ColorStackSort.Tests.asmdef` (+`Sinkii09.UIFramework`, `UniTask`, `R3.Unity`).

The reached level now survives a restart. Only an `int` is stored — a level's identity is its seed,
so the board is regenerated, never serialized.

**Rules this phase established, each of which was a real defect first:**
- **`LoadAsync` never throws except `OperationCanceledException`.** `ColorStackSortBootstrap`
  catches only OCE, so any other escaping exception skips the state change and leaves a blank
  screen — silently failing the "corrupt save → start at level 1" requirement the phase exists for.
- **Saving is fail-closed.** `_savingDisabled` starts `true` and opens only after a load actually
  completes. A `SaveSchemaVersionException` (save written by a *newer* build) leaves it closed for
  the whole session, so real progress is never overwritten. Cancellation leaves it closed too.
- **Save with `CancellationToken.None`, deliberately.** A lifetime-bound token
  (`Application.exitCancellationToken`) would cancel the write exactly at quit, when it most needs
  to land. `JsonSaveService.Dispose` declines to abort in-flight saves for the same reason.
- **One shared mutable save object, and `LoadAsync` adopts the deserialized instance.**
  `JsonSaveService` serializes *inside* its per-key semaphore and `SemaphoreSlim` release order is
  not FIFO, so per-write snapshots could persist level 2 after level 3. Adoption (rather than
  copying `CurrentLevel` out) is what stops a future second field being read, dropped, and
  overwritten with its default.
- **The level is clamped on every set.** Not just anti-tamper: `SaveEnvelopeCodec` cannot detect an
  envelope holding a *different* type and binds it to an all-default instance — i.e. level 0, which
  throws in `DifficultyCurve.ForLevel` inside a state's `OnEnterAsync`.
- **`SaveKey` is pinned and must match `^[A-Za-z0-9_-]+$`.** `JsonSaveService.ValidateKey` runs
  before any await; a bad key lands in the blanket catch, leaves saving *enabled*, and then faults
  every write — so progress silently never persists. A test pins the key against the regex.
- **Advancing and rebuilding are separate.** The level is banked to disk the moment `Advance()`
  runs, so the win panel advances at most once per show but may retry the rebuild; a failed rebuild
  re-arms Next instead of leaving a dead popup.

**Verification:** 173/173 EditMode green *before* the post-implementation review. The review's fixes
(whole-object adoption, the fake honouring `ct` and the key regex, the win-panel advance/retry
split) plus 3 further tests are **confirmed compiled** — verified by reflecting over
`Library/ScriptAssemblies/UIFramework.ColorStackSort.Tests.dll` for the new symbols, not by trusting
the console, which was serving latched `CS0103` errors from a mid-edit save at the time. Those 3
tests have **not been executed yet**; expected total is 176.

### 2026-08-02 — UIFramework v1.3.0: animation/transition hardening cluster (1 CRITICAL + 2 WARNING, tagged and pushed)

Targeted audit of `Runtime/Core/Animation/*` and the lifecycle/navigation code that drives it —
separate from the v1.2.0/v1.2.1 consolidated-audit clusters above. Root cause, plan, reviewer
approval, implementation, 6 new/modified regression tests (61/61 PlayMode green), all 3 fixes
revert-and-confirm-red spot-checked (one revert during Finding 2's check surfaced a genuine second
gap — a fresh `CanvasGroup` defaults `interactable=true`, Unity's own default, which the initial
fix missed for the null-transition path — caught and fixed before commit), post-implementation
review (Approve with changes, 1 non-blocking WARNING deferred to the vault as a known limitation),
vault synced. 3 commits in `com.sinkii09.uiframework` (`8e37734`, `63f2d46`, `2c3af11`), tagged
`v1.3.0`, pushed. TheEnd stayed on `file:../../com.sinkii09.uiframework` until after ColorStackSort
Phase 5, then repinned to `...git#v1.3.0` (an earlier revision of this entry claimed the repin had
already happened — it had not). Plan:
`plans/260802-1358-animation-transition-hardening/plan.md` in that repo. Full detail in the
"Known Issues" section above.

### 2026-08-02 — UIFramework v1.2.1: Phase 2 hardening cluster (3 findings fixed + 1 bonus fix, tagged and pushed)

Follow-up to the v1.2.0 correctness cluster — the smaller "crash / data-loss prevention" bucket
deferred at the time (C4, C5, and a `UIViewRegistry` reflection-swallow finding), all backwards
compatible. Plan: `plans/260802-1122-hardening-cluster/plan.md` in
`e:\Hoc_2025\1_1_2025\com.sinkii09.uiframework` (commits `f3c8855`..`3250d46`, tagged `v1.2.1`,
pushed to `origin/main`). TheEnd's `Packages/manifest.json` repinned from the temporary
`file:../../com.sinkii09.uiframework` compile-verification dependency to
`...git#v1.2.1` and re-verified compiling clean against the real tag (56/56 PlayMode +
4/4 EditMode).

**What broke / root cause / fix**, one line each:
- Wizard silently destroyed existing files on regen → `File.WriteAllText` with no existence check
  → now checks both target paths before writing either, confirms via dialog.
- `ISafeAreaProvider` crashed DI when unregistered → `UIFrameworkLifetimeScope` only registered it
  when a `SafeAreaProvider` existed in hierarchy, else registered nothing (VContainer field
  injection throws before `SetValue` on a missing registration) → added `NullSafeAreaProvider`
  Null-Object fallback, same pattern as the existing `ITransitionOverlay`/`NullTransitionOverlay`.
- `UIViewRegistry` silently dropped a whole assembly's views on any reflection hiccup → bare
  `catch { continue; }` around `Assembly.GetTypes()` → narrowed to `ReflectionTypeLoadException`,
  recovers the types that loaded fine via `ex.Types.Where(t => t != null)` instead of discarding
  all of them.
- **Bonus, found mid-implementation:** `Editor.Tools` assembly (both setup wizards, the View/
  ViewModel generator, the custom inspector, all menu items) never compiled in any consuming
  project, ever, silently — its asmdef had `defineConstraints` for 3 `SINKII09_*` symbols but no
  matching `versionDefines` to actually define them (per-assembly, not global — same class of bug
  previously seen in a test asmdef, see [[unity-test-assembly-and-bee-gotchas]] memory). Fixed by
  copying the working `versionDefines` block from the runtime/test asmdefs.

**Verification:** compiled clean project-wide; 56/56 PlayMode tests + 4/4 EditMode tests green
(new `Tests/Editor/` assembly for the wizard test — it depends on the Editor-only `Editor.Tools`
assembly, which the unrestricted `Tests/Runtime` assembly can't reference); revert-and-confirm-red
spot-check on the wizard and SafeAreaProvider fixes. Also hit and worked around a genuine Unity
Editor session quirk: this project's `Sinkii09.UIFramework.Tests` (Tests/Runtime) assembly's
EditMode test discovery got stuck mid-session (compiled fine, zero tests enumerated under
`testMode: EditMode` by any filter) — PlayMode discovery for the same assembly worked throughout
and was used instead once identified; likely needs an Editor restart to fully resolve, not
project-code-related.

**Status:** committed, NOT tagged/pushed — same STOP-gate as v1.2.0, awaiting user confirmation.
Two smaller Editor-tooling risks remain deliberately deferred (two divergent setup wizards, no
path-traversal validation on the wizard's name field) — see Known Issues above.

### 2026-08-02 — ColorStackSort Phase 4: level flow + HUD (moves, undo, restart, next)

Turned the single generated board into a loop. **Compiles clean; 159/159 EditMode tests green**
(0 failed, 0 skipped), verified against UIFramework v1.3.0 after the post-review fixes.

**Added (Logic assembly, engine-free):**
- `MoveRecord` — From/To/Count/Color. Unlike `Move` it *does* carry the run length, because after a
  move the board can no longer tell how much of the destination's top run arrived in that step.
- `BoardState.UndoMove(MoveRecord)` — the board's **second write path**, six ordered checks, not
  interchangeable with `Apply`/`IsLegal`. Third occurrence of the "two similar-looking rules that
  must not be merged" trap in this feature.
- `DifficultyCurve` — `ForLevel(int)` (3→8 colours, spare containers 2→1 at level 10) and
  `SeedForLevel(levelIndex, baseSeed)`, so level N is always the same board and Restart reproduces
  the puzzle the player just failed.
- `BoardInteraction` — LIFO history, `CanUndo`, `TryUndo`. Unlimited depth.

**Added (Unity assembly):** `LevelProgressService` (Singleton — the one piece of state that outlives
a view), `BoardRenderer` and `BoardAnimationScope` (both extracted from `BoardView`),
`BoardControlBar`, `ColorStackSortWinView` + ViewModel + Args, `ColorStackSortPanelPrefabBuilder`.

**Key decisions:**
- **Restart and Next are lifecycle operations, not ViewModel operations.** `BoardView` rebuilds its
  tubes and clears its solved latch only in `OnShowAsync`, so regenerating the board in the
  ViewModel would leave it rendering the old board and permanently input-locked. Both route through
  `GameLifecycleManager.RestartCurrentStateAsync()` — the feature's only board-rebuild path.
- `ChangeStateAsync<T>` into the state you are already in silently no-ops (v1.2.0 same-state guard),
  so `RestartCurrentStateAsync` is the only correct API here.
- The control bar lives **inside** `BoardView`, not on `UILayer.HUD` — HUD sorts at 0 and Screen at
  100, so a HUD-layer view would render underneath the board it controls.
- `ColorStackSortSettings`'s fixed knobs became a debug override (off by default) for reproducing a
  reported board; the curve drives normal play.
- `BoardView.prefab` was **deleted and regenerated** (the builder is create-if-missing, so the
  control bar would never have reached the existing prefab and `_controlBar` would be null at
  runtime). Safe because it is fully code-generated and referenced only by a Resources *path*.

**Caught in review, would have shipped otherwise:**
- **Win panel soft-lock.** Next was tappable during the panel's own 0.4s entrance, while
  `UINavigator._isTransitioning` was still held — so `RestartCurrentStateAsync`'s `CloseAllAsync`
  *and* its `ShowAsync` were both silently dropped. Level advanced, board never rebuilt, popup
  stranded over a board whose controls were already disabled. Two warnings, no error, no way out.
  Fixed by arming the button only after a successful entrance, plus an `IsTransitioning` check.
- Undo lowered the wrong tube (`record.To` instead of the selected one), and lowered it after the
  board had already mutated, so the lift animation could strand balls in the air.
- `..._GeneratesASolvableBoard` asserted only `DoesNotThrow` — a test whose name promised more than
  its body checked, the same defect as the generator work.

**Known gaps:** `BoardView.cs` is 204 lines and `ColorStackSortSceneBuilder.cs` 203, both over the
200-line guideline; further trimming would have meant deleting comments that document real traps.

### 2026-08-02 — ColorStackSort Phase 3: DI wiring, game state, playable scene
**Added:** `ColorStackSortSettings` (SO), `ColorStackSortLifetimeScope` (root scope),
`ColorStackSortGameplayState` (`IGameState`), `ColorStackSortBootstrap`
(`IInitializable` + `IAsyncStartable`), `ColorStackSortAssetBuilder` + `ColorStackSortSceneBuilder`
(editor, create-if-missing), `ColorStackSortSettingsTests` (6 tests). 124/124 EditMode green.

**Moved:** `BoardView.prefab` → `Resources/ColorStackSort/` (GUID preserved) and added
`[UIViewKey("ColorStackSort/BoardView")]`. It was previously in neither `Resources/` nor
Addressables, so `ShowAsync<BoardView>` would have thrown — latent because nothing had called it.

**Bugs found and fixed during the phase, all of which compiled clean:**
- `EditorSceneManager.NewScene(..., Single)` unloads assets the new scene doesn't reference yet.
  Config/settings references captured *before* it became "fake null" and serialized as
  `{fileID: 0}` — a scene that looked built and would fail on Play. Fix: load assets *after*
  `NewScene`. (First diagnosis blamed `AssetDatabase.Refresh` and was wrong; a null-guard log
  identified the real boundary.)
- `CloseAllAsync` inside `OnEnterAsync` is dead code — `UINavigator` exempts `ShowAsync` from the
  transition guard via `_stateTransitionActive` but not `CloseAllAsync`, and the stack is already
  cleared before the state is entered. Removed.
- `[Preserve]` is ambiguous (CS0104) wherever `using VContainer;` is present — VContainer ships its
  own `PreserveAttribute`. Fully qualified.
- `SaveCurrentModifiedScenesIfUserWantsTo()` opens a modal dialog that deadlocks Unity's main
  thread under MCP automation. Replaced with a non-blocking `scene.isDirty` refusal.

### 2026-08-01 — UIFramework v1.2.0: Phase 1 correctness cluster (5 findings fixed, committed not yet tagged)
4-way parallel adversarial audit (101 files) re-verified the June 2026 review's "fixed" claims and
found 4 of 9 only partially fixed, plus 5 new CRITICALs. This closes the "Phase 1 — correctness"
cluster (5 findings) with a reviewed plan, faithful implementation, 20 new regression tests
(50/50 green), and a full revert-and-confirm-red spot-check on all 5 fixes before committing.

**What broke:** (1) `UIViewFactory`'s concurrent-creation dedup guard only covered the manual
`Register<>()` path — `UINavigator`'s default auto-registration path had no guard at all, so two
concurrent requests for the same view type could instantiate two GameObjects. (2)
`GameLifecycleManager` bypassed `UINavigator` internally, calling the state machine directly —
this made the navigator's nav-stack-clearing dead code for every GLM transition and, because
`UINavigator.ChangeStateAsync` used to call `IUIStateMachine.ResetState()` before every
transition, silently skipped every state's `OnExitAsync` (timeScale restore, subscription
disposal, spawned-object teardown never ran). Confirmed live: `MemoryGame` and `AircraftStriker`
were on incompatible navigation paths in this same repo. (3) `TweenExtensions.AwaitAsync`'s
`OnComplete`/`OnKill` wiring silently overwrote every built-in transition's own `.OnKill(...)`
restore-on-cancel callback — those callbacks *never once ran*, despite passing code review in
June 2026 (DOTween's setters replace, not chain). (4) `UIViewBase.ShowAsync` swallowed
`OperationCanceledException` instead of propagating it, so a cancelled show still got pushed onto
the nav stack as a hidden phantom entry. (5) `UIStateMachine`'s cancellation-branch rollback still
unconditionally restored the previous state as current even after its `OnExitAsync` had already
run, double-executing exit cleanup on the next transition — the June fix only covered the
general-exception branch.

**Root cause pattern across (1)/(3):** a fix applied to one of two parallel code paths, or to one
of two chained callback sites, is not a fix — always grep for every caller/every path before
declaring a bug closed.

**Fix:** collapsed `UIViewFactory` to one `CreateCoreAsync` behind all 3 public overloads;
`GameLifecycleManager` now takes the concrete `UINavigator` and routes through its (now-`internal`)
`ChangeStateAsync`, with `ResetState()` no longer auto-invoked so `OnExitAsync` genuinely runs;
added `UITransition.RestoreOnCancel(view)` called from `DOTweenUIAnimator`'s catch blocks instead
of any tween callback; `ShowAsync` now rethrows on cancel (`HideAsync` deliberately still doesn't —
documented asymmetry); `UIStateMachine`'s two exception branches now share one rollback rule.
`IUINavigator.ChangeStateAsync` removed from the public interface entirely (breaking change,
v1.1.0→**v1.2.0**) — `MemoryGame`'s `MainMenuViewModel`/`WinViewModel` migrated to
`GameLifecycleManager.ChangeStateAsync`/`RestartCurrentStateAsync` in the same session.

**Status:** framework repo (`e:\Hoc_2025\1_1_2025\com.sinkii09.uiframework`) has 7 commits through
`03bc885`, CHANGELOG + 5 Obsidian vault notes updated. **Not yet tagged or pushed** — TheEnd is
temporarily on a `file:../../com.sinkii09.uiframework` dependency in `Packages/manifest.json`
pending user confirmation to tag `v1.2.0` and repin to the git URL.

### 2026-08-01 — Color Stack Sort: Phase 2 board presentation (118 EditMode tests)

**Added:** the view layer — tubes of stacked balls, tap-to-select (top run lifts), tap-to-move
(whole run travels). Prefabs are generated from code (`Tools/ColorStackSort/Build Prefabs`) using
Unity's built-in sprites, so the feature has no binary art dependency. **Create-if-missing** —
existing prefabs are skipped, deliberately unlike `AircraftStrikerSetupWizard`, which rebuilds from
hardcoded values and silently reverts manual edits.

Tap rules live in the engine-free Logic assembly (`BoardInteraction`), not the ViewModel: selection
state is game logic, and it keeps the whole rule set EditMode-testable. `BoardInteraction` owns the
board's **only** write path.

**Two CRITICAL animation defects found in review, both invisible to tests:**

1. `Shake()` read `anchoredPosition` *before* `DOKill(true)`, so a second rejected tap within the
   0.3s shake duration captured a mid-shake offset — and that stale value got written back on kill,
   leaving the tube column permanently off-centre.
2. The 0.14s selection-lift tween survived the reparent onto the travel overlay, where its
   tube-local target meant somewhere else entirely. Being the older tween it wrote first each frame,
   so the travel tweens captured the corrupted position as their start and balls visibly warped.

Also: `_animationCts` is recreated in `OnEnable` when already cancelled (a GameObject toggled
outside the framework's Show/Hide path would otherwise silently kill every later move while taps
stayed live); `PopTop`/`Attach` log instead of silently clamping a view/model desync.

**Verification:** 160 generated levels driven to solved through `Tap()` calls alone (6,738 taps),
asserting *each* move tap returns `Moved` — endpoint-only assertions would let a wrong rule
reinterpret the following tap and still stumble into a win.

### 2026-08-01 — Color Stack Sort: Phase 1 pure logic core + 105 EditMode tests

**Added:** new game feature `Assets/UIFramework/Features/ColorStackSort/` — board model, player
move rule, and a procedural level generator, all engine-free. First feature in the project split
across a `noEngineReferences` logic assembly plus a test assembly.

**Three defects caught and fixed after the code was written and fully green:**

1. **Dead difficulty knob.** `ScrambleSteps` did nothing past ~12 moves on a 4-colour board —
   11.7 average at budgets of 20, 60, 200 *and* 1000, identical. Root cause: the immediate-undo
   filter emptied the candidate list and the `break` treated that as terminal, when near
   saturation the only surviving candidate is often just the undo of the last step. Fix: on a
   stall, re-collect without the filter before giving up. 11.7 → 43.6 average, zero solvability
   loss across 200 replayed levels per setting.
2. **Crash on valid input.** `ScrambleSteps = 1` threw for ~0.4% of seeds (233/80000): a one-step
   scramble often lands back on a solved board and 8 full restarts weren't enough. Would have
   crashed specific tutorial levels once Phase 4 maps level index to seed. Fix: keep stepping past
   the budget while the board still reads solved. 0/80000.
3. **Non-deterministic level identity.** `System.Random` gives no cross-runtime guarantee, so a
   Unity upgrade could have silently rewritten every level. Replaced with in-repo PCG32 + golden
   values, verified identical across .NET 10 and Unity Mono.

**Method note:** the invertibility rule was wrong on its first draft (`j == A.Count` instead of
`runLength == A.Count` — for `[Red, Red, Blue]` the top run is 1 but Count is 3). Mutation testing
then showed the generate-then-replay test is *blind* to that specific bug, because
`CollectCandidates`' `amount <= runLength` loop bound never offers the bad candidate — the loop
bound was doing work credited to the constraint. Only the direct rule tests catch it; the
reachable clause is caught 200/200.

### 2026-07-20 — Aircraft Striker: main menu layout redesign (casual portrait hierarchy)
**Goal:** `AircraftMainMenuView` didn't read as a casual portrait-mobile shooter menu — the `Title` GameObject had *zero* visual content (bare `RectTransform`, no `TMP_Text`, despite being show-animated), and `PlayButton`/`ShopButton` were near-identical size/position (both 60%w×13%h, same label font size 18) with no primary/secondary hierarchy.
**Changed** (prefab-only, zero C# behavior change to `AircraftMainMenuView.cs`/`AircraftMainMenuViewModel.cs`, applied via Unity-MCP tools, no raw YAML edits):
- `Title` — added a `TMPro.TextMeshProUGUI` (text "AIRCRAFT STRIKER", fontSize 64, Bold), anchors moved from full-stretch `(0,0)-(1,1)` to top band `(0.05,0.80)-(0.95,0.94)`.
- `BestScoreLabel` — anchors `(0.10,0.75)-(0.90,0.85)` → `(0.30,0.72)-(0.70,0.79)`, tucked directly under the title.
- `PlayButton` — anchors `(0.20,0.45)-(0.80,0.58)` → `(0.22,0.36)-(0.78,0.55)` (13%h→19%h, moved to vertical-mid/lower band); its `Label` fontSize 18→40, Bold — now the visually dominant action.
- `ShopButton` — anchors `(0.20,0.28)-(0.80,0.41)` → `(0.32,0.22)-(0.68,0.32)` (60%w×13%h → 36%w×10%h); label font size left at 18 — secondary/smaller by design.
- `AircraftStrikerSetupWizard.cs` (`CreateViewPrefabs`, the `AircraftMainMenuView` block) — updated in lockstep with the same anchors/font values, since this wizard programmatically rebuilds the same prefab from scratch and is marked "safe to re-run"; left un-synced it would have silently reverted the whole redesign on next run with no compile error (caught by plan review).
**Verified live in Editor:** Play Mode + Game View screenshot confirmed the intended hierarchy (title biggest/top, small best-score, big Play, smaller Shop); zero compile errors after the wizard edit; `git diff` confirmed the two view/viewmodel scripts are byte-identical to before.
**Plan + reviews:** `plans/260720-2147-aircraft-mainmenu-layout-redesign/` — plan review (1 CRITICAL: wizard drift, resolved) + post-implementation review (Approved, no new findings).

### 2026-08-01 — UIFramework v1.1.0: save/load persistence hardened + first test assembly

**Repin:** `manifest.json` `#v1.0.0` → `#v1.1.0`, plus a new `"testables"` entry.

**What broke:** the persistence system shipped 2026-07-20 but had never been executed, and
`LoadAsync` could **silently destroy a save**. It relied on `JsonConvert.DeserializeObject` throwing
to detect corruption, but Newtonsoft only throws for *malformed* JSON — a present-but-wrong-shaped
file (`{}`, `"null"`, empty, or a newer schema) deserialized to a null payload, which the service
reported as "no save yet". The caller would start fresh and the next `SaveAsync` rotated the last
good backup out. No log, no exception.

**Root cause:** `null` is only a safe "absent" sentinel if nothing *except* absence can produce it —
and a deserializer can. A second instance of the same bug was caught in post-implementation review:
the version check initially ran *after* the payload-null check, so a v2 file whose `Data` was
reshaped still fell through to backup recovery and restored the older save over the newer one.

**Fix:** a present file that is not a valid envelope is corruption — try the `.bak`, else throw.
Only a missing file returns `null`. `SchemaVersion` is now enforced, read from a shape-agnostic
`JToken` parse so the check cannot be masked by a payload that no longer binds; newer-than-build
throws `SaveSchemaVersionException` and deliberately does *not* consume the backup. Also:
`SaveAsync(null)` throws, cancellation now fires `OnSaveFailed` (a "Saving…" spinner could hang
forever), key-less `ExistsAsync<T>()`/`DeleteAsync<T>()` overloads added, `JsonSaveService` is
`IDisposable`, and backend file I/O is uniformly off-thread with cancellation observed.

**Tests:** 30 PlayMode tests, the framework's first — verified green against the published git tag,
not just a local checkout. The two schema regression tests were proven non-vacuous by reverting the
check order and confirming they fail.

### 2026-07-19 — Aircraft Striker: UIEffect hover sweep on main menu buttons
**Goal:** `PlayButton`/`ShopButton` had no hover feedback. Wired `com.coffee.ui-effect`'s Shiny
transition onto both, driven by a new `UIEffectHoverTrigger` (`Scripts/Views/UIEffectHoverTrigger.cs`)
— `IPointerEnterHandler`/`IPointerExitHandler` that calls `UIEffectTweener.PlayForward(true)` plus a
`DOScale` punch (1.08x, `SetLink(gameObject)`) on enter/exit.
**Prefab wiring:** each button carries `Coffee.UIEffects.UIEffect` + `UIEffectTweener` +
`UIEffectHoverTrigger`. Sweep config: `TransitionFilter.Shiny`, `m_TransitionTex` = package's
`Transition-Horizontal.png` (must be assigned — shape-based filters flash instead of sweeping
without it), width 0.25, softness 0.5, rotation 135°, gray tint, `m_TransitionRate` reset to 0 so
it's idle at rest. Tweener: `WrapMode.Once`, duration 0.6s, not auto-looping.
**Side effects committed alongside:** `UIEffectProjectSettings.asset` now registers the
Shiny/Pattern shader variants (auto-added by the Editor the first time the effect renders).
Full gotchas (context/* vs direct fields, flash-bug root cause): [[uieffect-sweep-button-setup]] memory.

### 2026-07-18 — UIFramework: framework-level transition overlay system
**Goal:** Games had no full-screen loading/transition overlay to hide the blank-screen gap between `UINavigator.CloseAllAsync()` and a new view's factory-load+`ShowAsync` finishing. `UITransition` only animates a single view's own `CanvasGroup` — it cannot cover the whole screen. The framework's `Overlay` canvas layer (`UIRootLayerRefs.cs`, sortOrder 300) and two `LoadingState.cs` TODOs had been scaffolded for exactly this and never finished.
**New files** (`Packages/com.sinkii09.uiframework/Runtime/Core/`):
- `Interfaces/ITransitionOverlay.cs` — `ShowAsync`/`HideAsync`/`IsShown` contract.
- `Lifecycle/TransitionOverlayView.cs` — resident overlay extending `UIViewBase` (not `UIView<T>`, so `UIViewRegistry.AutoRegister` never picks it up — stays off the nav stack, never factory-loaded). Lives on the `Overlay` layer. Min-display-duration guard (`_minDisplaySeconds`, default 0.3s, unscaled time) prevents flicker on fast transitions. `InitializeNonGenericAsync` throws `NotSupportedException` as a second guard against accidental factory use.
- `Lifecycle/NullTransitionOverlay.cs` — no-op default when no overlay exists in a game's scene; keeps `GameLifecycleManager` null-check-free.
**Modified files:**
- `MVVM/UIViewBase.cs` — `HideAsync` gained a catch-all mirroring `ShowAsync`'s existing one (previously only caught `OperationCanceledException`; any other exception left `IsVisible` stuck `true` and the GameObject active — a pre-existing bug surfaced by code review, fixed because the overlay's "never stuck visible" guarantee depends on it).
- `DI/UIFrameworkLifetimeScope.cs` — scene-wide `FindAnyObjectByType<TransitionOverlayView>(FindObjectsInactive.Include)` check registers the real overlay `.As<ITransitionOverlay>()` if present anywhere in the scene, else `NullTransitionOverlay` (scene-wide search deliberately matches `RegisterComponentInHierarchy`'s own resolution scope, not just this LifetimeScope's subtree).
- `Lifecycle/GameLifecycleManager.cs` — gained `ITransitionOverlay` constructor dependency; `ChangeStateAsync<T>` and `RestartCurrentStateAsync` both wrap Show/transition/Hide in one try/finally (Show inside the try, Hide + `_isTransitioning = false` in finally) so the overlay can never get stuck showing even if Show itself throws. `ShowOverlaySafeAsync`/`HideOverlaySafeAsync` catch-and-log any exception — the overlay is decorative and must never fail a transition.
- `Lifecycle/States/LoadingState.cs` — removed the two TODOs; overlay is GLM's responsibility now, not this state's.
- `Assets/UIFramework/Features/AircraftStriker/Scripts/States/AircraftGameplayState.cs` — gained `ITransitionOverlay` constructor param; explicitly awaits `_overlay.HideAsync(ct)` (wrapped in try/catch, same decorative-never-fails principle) immediately before `_gameplay.StartGame()`. User-requested reliability guarantee: gameplay must never start while the overlay is still up — this was NOT previously guaranteed structurally, only incidentally by `WaveConfig.PreWaveDelay` timing.
**Aircraft Striker scene wiring:** added a `TransitionOverlay` GameObject (full-screen `CanvasGroup` + `TransitionOverlayView` + child raycast-blocking `Image` background) under the `Overlay` layer in `AS_Bootstrap.unity`, with a `FadeTransition` asset (`AssetBundles/AircraftStriker/OverlayFade.asset`, 0.2s) assigned to `_showTransition`/`_hideTransition`. Zero new gameplay C# beyond the one-line `AircraftGameplayState` change.
**Verified live in Editor:** compiled clean; Play-mode boot showed no errors; triggered a real `ChangeStateAsync<AircraftGameplayState>()` through the actual `GameLifecycleManager` — overlay showed/hid correctly, `UIStateMachine.CurrentState` only promotes after `OnEnterAsync` returns without throwing (confirmed `StartGame()` ran only after the overlay-hide completed).
**Plan + reviews:** `plans/260716-2151-transition-overlay-system/` — 2 plan-review rounds + 1 post-implementation review round, all findings resolved.

### 2026-07-05 — UIFramework: wired Addressables load/release lifecycle in UIViewFactory
**Goal:** `IUILoader.UnloadAsync` (releases an Addressables asset handle) existed but was never called anywhere — every successful `LoadAsync` was a permanent leak of that key's ref-count, both at `UIViewFactory.Dispose()` and whenever a freshly-loaded prefab was discarded after failing a type-check or DI/init step.
**Fix (`UIViewFactory.cs`):**
- Added `_cacheKeys: Dictionary<Type, string>` alongside `_cache`, storing the load key for each cached view type (written only on a fresh load, read via `TryGetValue`).
- Added `await _loader.UnloadAsync(key, CancellationToken.None)` at every point a freshly-loaded prefab's GameObject is destroyed without entering `_cache`: both `CreateAsync` overloads' type-mismatch throws, both overloads' failure catch blocks, and `InstantiateViewAsync`'s type-mismatch throw. `CancellationToken.None` deliberately used (not the ambient `ct`) — this is must-complete cleanup, not cancellable work.
- `Dispose()` now releases each cached view's loader handle (`GetAwaiter().GetResult()` — safe today since both loader impls are fully synchronous internally; flagged in a comment as a future deadlock risk if a loader ever does real async I/O) after destroying its GameObject.
- Extracted key derivation (`[UIViewKey]` attribute or type name fallback) into a shared `GetKey(Type)` helper — was previously duplicated/only available inside `InstantiateViewAsync`.
- Added an explicit contract doc comment on `IUILoader.LoadAsync` (`IUILoader.cs`): implementations must self-release any partially-acquired handle on throw/cancel — `UIViewFactory` relies on this and does not compensate for a `LoadAsync` failure itself (verified true for `AddressablesUILoader`, vacuously true for `ResourcesUILoader`).
**Not done (by design, per reviewer sign-off):** no defensive try/catch around the cleanup `UnloadAsync` calls themselves — neither current loader can throw from `UnloadAsync`, so this would be speculative; revisit if a future `IUILoader` implementation performs failable I/O in `UnloadAsync`.

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
