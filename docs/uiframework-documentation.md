# Sinkii09 UI Framework — Complete Documentation

**Package:** `com.sinkii09.uiframework` v1.0.0  
**Unity:** 6000.0+  
**Platform:** iOS, Android, PC  
**Stack:** UniTask 2.5 · R3 1.3 · VContainer 1.18 · DOTween

---

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [System Structure](#3-system-structure)
4. [Installation & Setup](#4-installation--setup)
5. [Core Concepts](#5-core-concepts)
6. [Usage Guide](#6-usage-guide)
7. [Animation System](#7-animation-system)
8. [Game Lifecycle](#8-game-lifecycle)
9. [Back-Button Handling](#9-back-button-handling)
10. [API Reference](#10-api-reference)

---

## 1. Overview

Sinkii09 UI Framework is a Unity 6 framework for building reactive, testable UI using MVVM, async navigation, and VContainer dependency injection. It replaces ad-hoc UI manager singletons with a structured pipeline:

```
VContainer DI
  └── UIViewFactory   — loads prefab, injects ViewModel, creates isolated scope
  └── UINavigator     — async stack navigation + state transitions
  └── UIStateMachine  — manages game states (Boot, Loading, Gameplay, ...)
  └── GameLifecycleManager — top-level IAsyncStartable entry point
```

**Key guarantees:**
- Every view has exactly one ViewModel; scopes are torn down on hide.
- All navigation operations are async-safe — concurrent calls are dropped, not queued.
- Back button dispatches to the highest-priority registered handler, not hardcoded logic.
- Transitions are DOTween-based ScriptableObjects — swap animations without touching code.

---

## 2. Architecture

### Layer diagram

```
┌─────────────────────────────────────────────────────────┐
│                   Game Developer Code                     │
│  BootState / GameplayState / custom views + viewmodels   │
└───────────────────────┬─────────────────────────────────┘
                        │ registers states, calls navigator
┌───────────────────────▼─────────────────────────────────┐
│              GameLifecycleManager  (IAsyncStartable)     │
│   Boot ──► Loading ──► Gameplay   (state machine)        │
└───────────────────────┬─────────────────────────────────┘
                        │ ChangeStateAsync / ShowAsync
┌───────────────────────▼─────────────────────────────────┐
│                     UINavigator                          │
│         ┌──────────────┴────────────────┐               │
│   NavigationStack              UIStateMachine            │
│   LIFO push/pop                game state registry       │
└───────────┬──────────────────────┬──────────────────────┘
            │                      │
┌───────────▼──────┐   ┌──────────▼──────────────────────┐
│  UIViewFactory   │   │  DOTweenUIAnimator               │
│  load → inject   │   │  show/hide tweens (SO config)    │
└───────────┬──────┘   └─────────────────────────────────┘
            │
┌───────────▼──────────────────────────────────────────────┐
│  UIView<TViewModel>  ◄──── ViewModelBase                  │
│  BindViewModel()            ReactiveProperty<T>           │
│  UIBindingExtensions        DisposableBag (lifetime)      │
│  (one-way + two-way R3)     DisposableBag (per-show)      │
└──────────────────────────────────────────────────────────┘
```

### Data flow: ShowAsync

```
navigator.ShowAsync<HUDView>()
  → _creators[HUDView] → factory.CreateAsync<HUDView, HUDViewModel>()
      → IUILoader.LoadAsync("HUDView")          // Resources or Addressables
      → container.CreateScope()                  // isolated VContainer scope
      → scope.Resolve<HUDViewModel>()
      → view.InitializeAsync(vm, scope, ct)      // BindViewModel called once
  → stack.PushAsync(view, ct)
      → view.ShowAsync(ct)                        // animator.ShowAsync()
```

### Lifetime model

| Lifetime | When created | When disposed |
|---|---|---|
| **ViewModel._disposables** | On first resolve | scope.Dispose() (on pool return or Cleanup()) |
| **ViewModel._showDisposables** | On every OnShow() | OnHide() — before next show |
| **View._showDisposables** | On every ShowAsync() | HideAsync() — after animation |
| **IObjectResolver scope** | UIViewFactory.CreateAsync | UIView.Cleanup() |

---

## 3. System Structure

```
Packages/com.sinkii09.uiframework/
├── package.json
└── Runtime/
    ├── com.sinkii09.uiframework.asmdef
    └── Core/
        ├── Interfaces/          ← all contracts (interfaces + enums)
        │   ├── IUIView.cs
        │   ├── IViewModel.cs
        │   ├── IViewState.cs
        │   ├── IGameState.cs       (extends IViewState)
        │   ├── ILoadingContext.cs
        │   ├── IUINavigator.cs
        │   ├── INavigationStack.cs
        │   ├── IUIStateMachine.cs
        │   ├── IUIViewFactory.cs
        │   ├── IUILoader.cs
        │   ├── IUIAnimator.cs
        │   ├── ISceneLoader.cs
        │   ├── IBackButtonRouter.cs
        │   ├── IBackButtonHandler.cs
        │   ├── ISafeAreaProvider.cs
        │   ├── IUIEvent.cs
        │   ├── IUIEventBus.cs
        │   └── IViewArgs.cs
        │
        ├── MVVM/
        │   ├── UIViewBase.cs        ← MonoBehaviour; CanvasGroup, RectTransform, layers
        │   ├── UIView.cs            ← UIView<TViewModel>; BindViewModel, ShowAsync, HideAsync
        │   ├── ViewModelBase.cs     ← IViewModel; _disposables, _showDisposables lifecycle
        │   ├── UIViewFactory.cs     ← loads prefab, injects VM, creates child scope
        │   ├── UIRootLayerRefs.cs   ← Inspector refs: Background, Main, Overlay, System layers
        │   ├── UILayer.cs           ← UILayer enum: Background, Main, Overlay, System
        │   └── UIBindingExtensions.cs ← R3 binding helpers (one-way + two-way)
        │
        ├── Navigation/
        │   ├── UINavigator.cs       ← facade; Register<>, ShowAsync, HideAsync, ChangeStateAsync
        │   ├── NavigationStack.cs   ← LIFO async stack
        │   ├── UIStateMachine.cs    ← registers + transitions IViewState/IGameState
        │   ├── NavigationContext.cs ← per-operation context (view + args)
        │   └── BackButtonRouter.cs  ← IInitializable; Escape → highest-priority handler
        │
        ├── Animation/
        │   ├── UITransition.cs      ← ScriptableObject abstract base
        │   ├── DOTweenUIAnimator.cs ← executes tweens; CanvasGroup management
        │   ├── FadeTransition.cs
        │   ├── ScaleTransition.cs
        │   ├── SlideTransition.cs
        │   └── SequenceTransition.cs
        │
        ├── Lifecycle/
        │   ├── GameLifecycleManager.cs  ← IAsyncStartable; state orchestration
        │   ├── SceneLoader.cs           ← allowSceneActivation=false pattern
        │   ├── ILoadingContext.cs
        │   ├── LoadingContext.cs        ← mutable; Set(scene, onLoaded), Reset()
        │   └── States/
        │       ├── BootState.cs         ← virtual no-op; subclass for splash screen
        │       ├── LoadingState.cs      ← loads scene via SceneLoader, calls onLoaded
        │       └── GameplayState.cs     ← stub; subclass for back-button + pause wiring
        │
        ├── DI/
        │   ├── UIFrameworkLifetimeScope.cs ← root VContainer LifetimeScope (MonoBehaviour)
        │   ├── UIViewRegistry.cs           ← reflection-scan → auto-register ViewModels
        │   ├── UIViewKeyAttribute.cs       ← [UIViewKey("key")] override for addressable path
        │   └── DOTweenBootstrap.cs         ← DOTween.Init() on domain reload
        │
        ├── Config/
        │   └── UIFrameworkConfig.cs  ← ScriptableObject; LoaderMode, MaxNavigationDepth
        │
        ├── Loading/
        │   ├── ResourcesUILoader.cs    ← default; Resources.LoadAsync
        │   └── AddressablesUILoader.cs ← conditional compile: #if ADDRESSABLES
        │
        └── Events/
            └── SafeAreaProvider.cs  ← MonoBehaviour; Rect SafeArea, Observable<Rect> OnChanged
```

---

## 4. Installation & Setup

### Step 1 — Install dependencies via OpenUPM

Add the OpenUPM scoped registry to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "jp.hadashikick",
        "com.github-glitchenzo"
      ]
    }
  ],
  "dependencies": {
    "com.cysharp.unitask":  "2.5.11",
    "com.cysharp.r3":       "1.3.1",
    "jp.hadashikick.vcontainer": "1.18.0"
  }
}
```

### Step 2 — Install NuGetForUnity (for R3 native DLL)

Add to `manifest.json` dependencies:
```json
"com.github-glitchenzo.nugetforunity": "4.4.2"
```

After Unity reimports, create `Assets/packages.config`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="R3" version="1.3.1" />
</packages>
```

NuGetForUnity restores `R3.dll` into `Assets/Packages/R3/`. Verify the DLL appears before continuing.

### Step 3 — Install DOTween

Download DOTween from the Asset Store. Run **DOTween Setup** from the toolbar (`Tools > Demigiant > DOTween Utility Panel`). Ensure `DOTween.asmdef` is available.

### Step 4 — Add scripting define symbols

In **Project Settings > Player > Scripting Define Symbols**, add:
```
SINKII09_UNITASK;SINKII09_R3;SINKII09_VCONTAINER
```

For UniTask + DOTween integration also add:
```
UNITASK_DOTWEEN_SUPPORT
```

### Step 5 — Create the UIRoot prefab

1. Create a new GameObject named `UIRoot` in your bootstrap scene.
2. Add `UIFrameworkLifetimeScope` component.
3. Create four Canvas children and assign them to `UIRootLayerRefs`:
   - `BackgroundLayer` (sort order 0)
   - `MainLayer` (sort order 10)
   - `OverlayLayer` (sort order 20)
   - `SystemLayer` (sort order 30)
4. Add a `SafeAreaProvider` MonoBehaviour anywhere in the hierarchy.
5. Create `UIFrameworkConfig` asset: **Assets > Create > UIFramework > Config**, assign to the `_config` field.
6. Mark the `UIRoot` GameObject as `DontDestroyOnLoad` (or keep it only in the bootstrap scene).

### Step 6 — Create UIFrameworkConfig

Right-click in Project: **Create > UIFramework > Config**

| Field | Default | Purpose |
|---|---|---|
| `LoaderMode` | `Resources` | Switch to `Addressables` after setup |
| `MaxNavigationDepth` | `10` | Stack depth cap |

### Step 7 — Verify

Open Unity console. Zero compile errors means the framework is ready.

---

## 5. Core Concepts

### MVVM

Each screen is split into two classes:

| Class | Role |
|---|---|
| `UIView<TViewModel>` | MonoBehaviour; displays state; calls `BindViewModel()` once |
| `ViewModelBase` | Plain C# class; owns reactive state via `ReactiveProperty<T>`; no Unity deps |

Views never read ViewModel state directly — they subscribe via `UIBindingExtensions` in `BindViewModel()`. The ViewModel never references the View.

### Reactive properties

Use R3 `ReactiveProperty<T>` in the ViewModel:
```csharp
public ReactiveProperty<int> Score { get; } = new(0);
public ReactiveProperty<bool> IsPaused { get; } = new(false);
```

Subscribe in the View's `BindViewModel`:
```csharp
vm.Score.BindToText(_scoreLabel, v => $"Score: {v:N0}")
    .AddTo(ref _showDisposables);
```

### Disposal scopes

- `_disposables` — lives as long as the ViewModel (until scope is disposed on pool return)
- `_showDisposables` — torn down on every `HideAsync()`, rebuilt on next `OnShow()`

Use `_showDisposables` for subscriptions that should reset between shows (timers, per-show UI state). Use `_disposables` for subscriptions that persist across multiple show/hide cycles.

### Navigation stack vs state machine

| Mechanism | When to use |
|---|---|
| `navigator.ShowAsync<T>()` | Push a new screen on top (HUD, dialogs, popups) |
| `navigator.PopAsync()` | Go back one screen |
| `navigator.CloseAllAsync()` | Clear entire stack |
| `navigator.ChangeStateAsync<T>()` | **Full screen switch** — clears stack then enters state |
| `lifecycle.ChangeStateAsync<T>()` | Transition between game phases (Boot → Loading → Gameplay) |

---

## 6. Usage Guide

### Creating a view

**1. ViewModel** — `Assets/_Game/UI/HUD/HUDViewModel.cs`
```csharp
using R3;
using Sinkii09.UIFramework;

public class HUDViewModel : ViewModelBase
{
    public ReactiveProperty<int> Score { get; } = new(0);
    public ReactiveProperty<float> Health { get; } = new(1f);

    public override void OnShow()
    {
        // subscribe to game events here; add to _showDisposables
    }
}
```

**2. View** — `Assets/_Game/UI/HUD/HUDView.cs`
```csharp
using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDView : UIView<HUDViewModel>
{
    [SerializeField] private TMP_Text _scoreLabel;
    [SerializeField] private Image _healthBar;

    protected override void BindViewModel(HUDViewModel vm)
    {
        vm.Score
            .BindToText(_scoreLabel, v => $"{v:N0}")
            .AddTo(ref _showDisposables);

        vm.Health
            .BindToFillAmount(_healthBar)
            .AddTo(ref _showDisposables);
    }
}
```

**3. Prefab** — Create a prefab named `HUDView` (matches class name) in `Resources/` (or Addressables if configured). Add the `HUDView` component to the root.

**4. Register in your installer**
```csharp
// In a LifetimeScope or IInstaller:
navigator.Register<HUDView, HUDViewModel>();
```

**5. Show the view**
```csharp
await _navigator.ShowAsync<HUDView>(ct);
```

---

### View with arguments

```csharp
// Args
public class ItemDetailArgs : IViewArgs
{
    public string ItemId;
    public int Quantity;
}

// ViewModel
public class ItemDetailViewModel : ViewModelBase, IViewModel<ItemDetailArgs>
{
    public ReactiveProperty<string> Title { get; } = new();

    public void Initialize(ItemDetailArgs args)
    {
        Title.Value = $"Item: {args.ItemId} x{args.Quantity}";
    }
}

// View
public class ItemDetailView : UIView<ItemDetailViewModel>
{
    [SerializeField] private TMP_Text _titleLabel;

    protected override void BindViewModel(ItemDetailViewModel vm)
    {
        vm.Title.BindToText(_titleLabel).AddTo(ref _showDisposables);
    }
}

// Register
navigator.Register<ItemDetailView, ItemDetailViewModel, ItemDetailArgs>();

// Show
await _navigator.ShowAsync<ItemDetailView, ItemDetailArgs>(
    new ItemDetailArgs { ItemId = "sword_01", Quantity = 1 }, ct);
```

---

### Custom BootState with splash screen

```csharp
public class GameBootState : BootState
{
    private readonly IUINavigator _navigator;
    private readonly GameLifecycleManager _lifecycle;
    private readonly ILoadingContext _loadingContext;

    [Inject]
    public GameBootState(IUINavigator navigator, GameLifecycleManager lifecycle,
        ILoadingContext loadingContext)
    {
        _navigator = navigator;
        _lifecycle = lifecycle;
        _loadingContext = loadingContext;
    }

    public override async UniTask OnEnterAsync(CancellationToken ct = default)
    {
        // Show splash, play logo animation, etc.
        await _navigator.ShowAsync<SplashView>(ct);
        await UniTask.Delay(2000, cancellationToken: ct);

        // Transition to Loading → then to Gameplay
        _loadingContext.Set("GameplayScene",
            async innerCt => await _lifecycle.ChangeStateAsync<GameplayState>(innerCt));

        await _lifecycle.ChangeStateAsync<LoadingState>(ct);
    }
}
```

Register your custom state in a game-side `IInitializable`:
```csharp
public class GameBootstrap : IInitializable
{
    private readonly GameLifecycleManager _lifecycle;
    private readonly GameBootState _bootState;

    [Inject]
    public GameBootstrap(GameLifecycleManager lifecycle, GameBootState bootState)
    {
        _lifecycle = lifecycle;
        _bootState = bootState;
    }

    public void Initialize()
    {
        // Runs before GameLifecycleManager.StartAsync — safe registration window
        _lifecycle.RegisterState(_bootState);
    }
}
```

---

### Two-way binding

```csharp
// ViewModel
public class SettingsViewModel : ViewModelBase
{
    public ReactiveProperty<bool> SoundEnabled { get; } = new(true);
    public ReactiveProperty<string> PlayerName { get; } = new(string.Empty);
}

// View
protected override void BindViewModel(SettingsViewModel vm)
{
    _soundToggle.BindTwoWay(vm.SoundEnabled, ref _showDisposables);
    _nameInput.BindTwoWay(vm.PlayerName, ref _showDisposables);
}
```

---

### Changing loader to Addressables

1. Install `com.unity.addressables` 2.6.0+ via Package Manager.
2. Add `ADDRESSABLES` to Scripting Define Symbols.
3. In `UIFrameworkConfig`, set `LoaderMode = Addressables`.
4. Mark view prefabs as Addressable assets; key = class name (or override with `[UIViewKey("custom-key")]`).

---

## 7. Animation System

Animations are DOTween-based `ScriptableObject` assets assigned per-view in the Inspector.

### Built-in transitions

| Type | ScriptableObject | Effect |
|---|---|---|
| `FadeTransition` | Fade in/out | CanvasGroup alpha 0→1 / 1→0 |
| `ScaleTransition` | Pop in/out | Transform scale 0→1 / 1→0 |
| `SlideTransition` | Slide from edge | AnchoredPosition offset |
| `SequenceTransition` | Chained | Combine any transitions in order |

### Creating a transition asset

Right-click in Project: **Create > UIFramework > Transitions > Fade** (or Scale, Slide, Sequence).

Assign to a `UIViewBase` field in the Inspector:
- `_showTransition` — plays on `ShowAsync()`
- `_hideTransition` — plays on `HideAsync()`

Both fields are optional — null means instant show/hide.

### DOTween SetUpdate(true)

All tweens use `SetUpdate(true)` so they play during `Time.timeScale = 0` (pause screens, loading screens).

---

## 8. Game Lifecycle

### State machine

`GameLifecycleManager` starts as a VContainer `IAsyncStartable`. On `StartAsync()`, it enters `BootState` immediately.

```
BootState  →  LoadingState  →  GameplayState (or any custom state)
```

States are stored in `UIStateMachine`. Transitions call `OnExitAsync()` on the current state before `OnEnterAsync()` on the next.

### Scene loading pattern

```
// Caller sets context before transitioning
_loadingContext.Set(
    "GameplayScene",                                   // scene to load
    async ct => await _lifecycle.ChangeStateAsync<GameplayState>(ct) // callback after load
);
await _lifecycle.ChangeStateAsync<LoadingState>(ct);

// LoadingState.OnEnterAsync:
//   1. validates TargetScene
//   2. TODO: show loading screen
//   3. SceneLoader.LoadAsync (progress available via IProgress<float>)
//   4. _loadingContext.Reset() — consume to prevent stale re-entry
//   5. onLoaded(ct)            — triggers next state transition
// LoadingState.OnExitAsync:
//   TODO: hide loading screen (fires before next state's OnEnterAsync)
```

### SceneLoader — allowSceneActivation pattern

`SceneLoader` halts at 90% (`allowSceneActivation = false`) to give you a window for "Tap to continue" UX or minimum display time. Set `allowSceneActivation = true` when ready; the load completes synchronously from there. Cancellation is honoured up to the 90% point; after activation is committed, the load runs to completion regardless.

### Adding custom game states

```csharp
// 1. Define the state
public class PauseState : IGameState
{
    public string SceneName => null;       // no scene load
    public bool PausesGameTime => true;

    public async UniTask OnEnterAsync(CancellationToken ct = default)
    {
        Time.timeScale = 0f;
        // show pause menu
    }

    public UniTask OnExitAsync(CancellationToken ct = default)
    {
        Time.timeScale = 1f;
        return UniTask.CompletedTask;
    }
}

// 2. Register before StartAsync (in IInitializable.Initialize())
_lifecycle.RegisterState(container.Resolve<PauseState>());

// 3. Transition
await _lifecycle.ChangeStateAsync<PauseState>(ct);
```

---

## 9. Back-Button Handling

`BackButtonRouter` listens for `KeyCode.Escape` each frame (Android back button). It dispatches to the handler with the **highest priority** among all registered handlers.

```csharp
public class HUDView : UIView<HUDViewModel>, IBackButtonHandler
{
    public int Priority => 10;

    public async UniTask HandleBackAsync(CancellationToken ct)
    {
        await _navigator.PopAsync(ct);
    }

    public override async UniTask ShowAsync(CancellationToken ct = default)
    {
        _backButtonRouter.Register(this);
        await base.ShowAsync(ct);
    }

    public override async UniTask HideAsync(CancellationToken ct = default)
    {
        _backButtonRouter.Unregister(this);
        await base.HideAsync(ct);
    }
}
```

Higher priority number wins. If two handlers share the same priority, the one registered most recently wins.

---

## 10. API Reference

### UIViewBase

| Member | Type | Description |
|---|---|---|
| `ViewId` | `string` | Class name of the view |
| `IsVisible` | `bool` | True when fully shown |
| `Layer` | `UILayer` | Background / Main / Overlay / System |
| `CanvasGroup` | `CanvasGroup` | Cached ref |
| `RectTransform` | `RectTransform` | Cached ref |
| `ShowAsync(ct)` | `UniTask` | Plays show transition |
| `HideAsync(ct)` | `UniTask` | Plays hide transition |
| `InitializeAsync(ct)` | `UniTask` | Framework-internal init |

### UIView\<TViewModel\>

| Member | Description |
|---|---|
| `BindViewModel(vm)` | **Abstract.** Set up R3 bindings here. Called once per lifetime. |
| `_showDisposables` | `DisposableBag` — add per-show bindings here; auto-reset on `HideAsync` |
| `ViewModel` | Protected getter |
| `InitializeAsync(vm, scope, ct)` | Called by factory — do not call manually |
| `Cleanup()` | Called on pool return — disposes scope + showDisposables |

### ViewModelBase

| Member | Description |
|---|---|
| `_disposables` | `DisposableBag` — lifetime bindings; disposed with scope |
| `_showDisposables` | `DisposableBag` — per-show bindings; disposed in `OnHide()` |
| `OnShow()` | Virtual. Called by View's `ShowAsync`. |
| `OnHide()` | Virtual. Called by View's `HideAsync` after animation. Resets `_showDisposables`. |
| `Dispose()` | Idempotent. Called by VContainer scope teardown. |

### IUINavigator / UINavigator

| Method | Description |
|---|---|
| `Register<TView, TViewModel>()` | Map view type to factory closure |
| `Register<TView, TViewModel, TArgs>()` | Map view type + args |
| `ShowAsync<T>(ct)` | Push view onto stack |
| `ShowAsync<T, TArgs>(args, ct)` | Push view with args |
| `HideAsync<T>(ct)` | Pop if T is top of stack |
| `PopAsync(ct)` | Pop top of stack |
| `CloseAllAsync(ct)` | Clear entire stack |
| `ChangeStateAsync<TState>(ct)` | Clear stack + enter state |
| `Current` | Top-of-stack view |
| `IsTransitioning` | Guard flag |

### GameLifecycleManager

| Method | Description |
|---|---|
| `RegisterState<T>(state)` | Register custom IGameState before StartAsync |
| `ChangeStateAsync<T>(ct)` | Transition to registered state |
| `StartAsync(ct)` | VContainer entry point — enters BootState |

### ILoadingContext / LoadingContext

| Member | Description |
|---|---|
| `TargetScene` | Scene name to load |
| `OnLoaded` | `Func<CancellationToken, UniTask>` — invoked after scene load |
| `Set(sceneName, onLoaded)` | Configure before transitioning to LoadingState |
| `Reset()` | Called internally by LoadingState after consuming |

### UIBindingExtensions

| Method | Description |
|---|---|
| `BindTo<TValue, TTarget>(target, setter)` | Generic one-way |
| `BindToText<TValue>(label, formatter?)` | Observable → TMP_Text |
| `BindToActive(gameObject)` | bool → SetActive |
| `BindToFillAmount(image)` | float → Image.fillAmount (clamped) |
| `BindToInteractable(button)` | bool → Button.interactable |
| `BindToAlpha(canvasGroup)` | float → CanvasGroup.alpha (clamped) |
| `BindTwoWay(toggle, property, ref bag)` | Toggle ↔ ReactiveProperty\<bool\> |
| `BindTwoWay(inputField, property, ref bag)` | TMP_InputField ↔ ReactiveProperty\<string\> |

### IBackButtonHandler

| Member | Description |
|---|---|
| `Priority` | `int` — higher wins |
| `HandleBackAsync(ct)` | Called when this handler wins dispatch |

### BackButtonRouter

| Method | Description |
|---|---|
| `Register(handler)` | Add handler |
| `Unregister(handler)` | Remove handler |

---

## Common Pitfalls

| Mistake | Fix |
|---|---|
| Calling `Register<>()` after `StartAsync` has run | Call in `IInitializable.Initialize()` — guaranteed before `IAsyncStartable.StartAsync()` |
| Binding in `OnShow()` without using `_showDisposables` | Subscriptions will leak. Always `AddTo(ref _showDisposables)` for per-show bindings. |
| Calling `HideAsync<T>()` on a view that isn't top-of-stack | Only top-of-stack hide is supported. Use `PopAsync()` or restructure navigation. |
| Forgetting `Reset()` guard on LoadingContext | Framework calls it automatically — do not call `Set()` again before the transition completes. |
| Registering GameplayState in UIFrameworkLifetimeScope | Game-specific states belong in game-side code. Register via `_lifecycle.RegisterState()`. |
| `ChangeStateAsync` during an active transition | Calls are silently dropped. Check `IsTransitioning` first or use a queuing pattern. |
| DOTween `SetUpdate(false)` on a pause overlay | Overlays during pause need `SetUpdate(true)`. This is set automatically by the framework. |
