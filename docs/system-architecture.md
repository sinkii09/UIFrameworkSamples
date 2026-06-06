# System Architecture

## High-Level Overview

The UIFramework is a Unity package that provides a declarative UI system built on reactive programming (R3), dependency injection (VContainer), and async support (UniTask). The Memory Flip Card Game is a sample implementation demonstrating full MVVM + state machine integration.

## Dependency Resolution Architecture

### OpenUPM Scoped Registry
```
manifest.json (scopedRegistries block)
    └── https://package.openupm.com
            ├── com.cysharp.* (UniTask, R3)
            └── jp.hadashikick.* (VContainer)
```

All packages resolve from a single OpenUPM registry with scoped lookups:
- `com.cysharp.*` → UniTask 2.5.11, R3 1.3.1
- `jp.hadashikick.*` → VContainer 1.18.0

Transitive dependencies are resolved automatically by Unity Package Manager.

### Dependency Graph
```
UIFramework (this package)
    ├── UniTask 2.5.11 (async/await support)
    ├── R3 1.3.1 (reactive patterns)
    │   └── [implicit] com.unity.nuget.newtonsoft-json (runtime dep, not in lock)
    └── VContainer 1.18.0 (dependency injection)
        └── [requires define] VCONTAINER_UNITASK_INTEGRATION (Step 2)
```

## Installer Wizard Flow

### Step 1: Install Dependencies
**Process:**
1. Read `Packages/manifest.json`
2. Check if OpenUPM registry exists; if not, insert it at the start of scopedRegistries
3. Check if each package (UniTask, R3, VContainer) is already in dependencies
4. Insert any missing packages with OpenUPM version strings
5. Write updated manifest.json and trigger `Client.Resolve()`

**Error Handling:**
- File read/write failures → caught, logged, return false
- Malformed scopedRegistries → detect via bounded search (40-char window), return null
- Missing dependencies key → detect and return null

**Critical Detail:**
- R3 1.3.1 has an implicit runtime dependency on `com.unity.nuget.newtonsoft-json` (not declared in lock file)
- If consumers see missing Newtonsoft types at runtime, they must add the dependency manually

### Step 2: Add Scripting Define
**Process:**
1. Check if `VCONTAINER_UNITASK_INTEGRATION` define exists
2. If not, append to current build target's scripting defines
3. Mark as done

**Purpose:** Enables VContainer's UniTask integration hooks for lifecycle management.

### Step 3: Validate DOTween
**Process:**
1. Scan loaded assemblies for `DG.Tweening.DOTween` type
2. If found, mark as done
3. If not found, mark as failed (user must install from Asset Store)

**Note:** This step does not auto-install — DOTween Pro is a third-party Asset Store package.

### Step 4: Create UIRoot Prefab
**Process:**
1. Check if `Assets/_Project/Prefabs/UIRoot.prefab` exists
2. If not, create a GameObject with:
   - Canvas (Screen Space Overlay mode)
   - CanvasScaler (1080x1920 reference resolution, Scale With Screen Size)
   - GraphicRaycaster
   - Five sorted child canvases: HUD (0), Screen (100), Popup (200), Overlay (300), Debug (400)
3. Save as prefab and clean up

**Sorting Order:** Canvases are separated by layer order to enable depth-based UI composition.

### Step 5: Create Config Asset
**Process:**
1. Check if `Assets/Resources/UIFramework/UIFrameworkConfig.asset` exists
2. If not, create via `ScriptableObject.CreateInstance()`
3. Save to database

**Timing:** This step may skip if runtime code isn't loaded yet; safe to rerun.

### Step 6: Create Folder Structure
**Process:**
1. Ensure folders exist:
   - `Assets/_Project/` (game-specific code)
   - `Assets/_Project/Features/` (feature modules)
   - `Assets/_Project/Prefabs/` (game prefabs)
   - `Assets/Resources/` (runtime-loaded assets)
   - `Assets/Resources/UIFramework/` (framework configs)

## Failure Modes & Recovery

| Step | Failure | Recovery |
|------|---------|----------|
| 1 | manifest.json unreadable | Check file permissions, close Unity, retry |
| 1 | scopedRegistries malformed | Manually fix JSON structure, rerun wizard |
| 1 | dependencies block missing | Invalid manifest — manually restore or delete and reimport package |
| 2 | Cannot set defines | Check build target is selected in Build Settings |
| 3 | DOTween not found | Install DOTween Pro from Asset Store, then refresh |
| 4 | Cannot create prefab | Check Assets/_Project/Prefabs/ folder has write permissions |
| 5 | UIFrameworkConfig type not found | Rerun after scripts compile |
| 6 | Cannot create folders | Check Assets/ has write permissions |

## Performance & Reliability Notes

- **Step 1 retry behavior:** Sets `PendingKey` flag and triggers `Client.Resolve()`, then returns false to allow Unity to reload before continuing
- **Type lookups:** Use assembly scanning (AppDomain) to detect DOTween and UIFrameworkConfig types
- **Array searches:** Bounded to 40-char window to prevent mis-parsing malformed JSON
- **Folder creation:** Idempotent — safe to call multiple times

## Memory Flip Card Game State Integration

The Memory Game demonstrates full UIFramework integration as a game state within the state machine lifecycle.

### State Flow

```
GameLifecycleManager (IAsyncStartable entry point)
    │
    ├─ BootState → LoadingState → MemoryGameState (registered in UIStateMachine)
    │
MemoryGameState (IGameState)
    │
    ├─ OnEnterAsync() → UINavigator.ShowAsync<GameplayView>()
    │   └─ Creates GameplayViewModel scope
    │   └─ Loads & initializes CardView prefab instances
    │   └─ DOTween animates cards into view
    │
    ├─ GameplayView interaction → GameplayViewModel ReactiveProperties update
    │   └─ Flips tracked in MemoryCardGame (pure logic)
    │   └─ R3 bindings update CardView visual state
    │
    ├─ Win condition detected (IsComplete)
    │   └─ GameplayViewModel signals win
    │   └─ UINavigator navigates: GameplayView → WinView
    │   └─ WinView shows final stats (moves, time)
    │
    └─ OnExitAsync() → UINavigator.CloseAllAsync()
        └─ Teardown all views; dispose scopes

```

### Key Components

| Component | Type | Responsibility |
|---|---|---|
| `MemoryGameState` | IGameState | Entry/exit point; manages show/hide of gameplay |
| `MemoryCardGame` | C# class (no MonoBehaviour) | Pure game rules: shuffle, flip, match detection |
| `GameplayViewModel` | ViewModelBase | Reactive state: card visibility, game progress |
| `GameplayView` | UIView<GameplayViewModel> | Board layout; spawns CardView instances |
| `CardView` | UIView (child item) | Single card; handles tap input, flip animation |
| `WinViewModel` | ViewModelBase | Win screen state: move count, elapsed time |
| `WinView` | UIView<WinViewModel> | Win screen UI |

### Dependency Injection

MemoryGameState is registered via `SampleLifetimeScope.ConfigureContainer()`:
```csharp
builder.Register<IGameState, MemoryGameState>();
```

On state transition, VContainer resolves and injects `IUINavigator` dependency.

### Flow Example: User Taps Card

1. **CardView.OnCardTapped()** → calls `MemoryCardGame.TryFlip(cardId)`
2. **MemoryCardGame** evaluates flip (lock, matched, already flipped) → returns FlipResult
3. **GameplayViewModel** listens to `MemoryCardGame.OnMatchFound` event
4. **ReactiveProperty<bool>** updates (card visibility toggle)
5. **R3 binding** in CardView observes change → calls `CardView.ShowFlip()`
6. **DOTween animation** (flip sequence) plays
7. If win condition (`IsComplete`) → **GameplayViewModel** triggers next state show

This pattern separates concerns: game logic in pure C#, state in ViewModel, animations in View.
