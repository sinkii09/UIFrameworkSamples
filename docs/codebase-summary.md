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
- `UNITASK_DOTWEEN_SUPPORT` — **NOT defined**; DOTween↔UniTask bridged manually via `UniTaskCompletionSource` in `DOTweenUIAnimator.AwaitTween`

---

## UIFramework Package (`Packages/com.sinkii09.uiframework/`)

### Core Systems
| File | Purpose |
|------|---------|
| `Runtime/Core/MVVM/UIView<T>.cs` | Base view — binds ViewModel, exposes `OnShowAsync`/`OnHideAsync` |
| `Runtime/Core/MVVM/UIViewBase.cs` | Caches `CanvasGroup`, `RectTransform`; drives show/hide lifecycle |
| `Runtime/Core/Navigation/UINavigator.cs` | Stack-based screen navigation |
| `Runtime/Core/Animation/DOTweenUIAnimator.cs` | `IUIAnimator` impl; fade/scale transitions via DOTween. Bridges `Tween→UniTask` without `UNITASK_DOTWEEN_SUPPORT` using `UniTaskCompletionSource` + `CancellationTokenRegistration` (disposed on complete/kill to prevent stale callbacks on pooled tweens) |
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

## Recent Changes

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

### 2026-06-07 — MainMenuView juice animations
Added DOTween entrance/exit to `MainMenuView`: title scale-punch (`OutBack`), buttons stagger scale-in (`OutBack`, 80ms apart), hide collapses everything (`InBack`). All run with `SetUpdate(true)`.

---

## Code Standards
- DOTween↔UniTask bridge: always use `UniTaskCompletionSource` pattern (no `UNITASK_DOTWEEN_SUPPORT`); always dispose `CancellationTokenRegistration`
- DOTween tweens: always `SetUpdate(true)` so they survive `Time.timeScale = 0`
- Animations inside LayoutGroup: use `DOScale`, never `DOAnchorPosY`/`DOMove`
- Pre-hide animated views in `Awake` (`localScale = Vector3.zero`) to prevent first-frame flash
