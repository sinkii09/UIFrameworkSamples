# Aircraft Striker 2D Game — Implementation Complete, Framework Matured

**Date**: 2026-06-20 11:47  
**Severity**: Medium  
**Component**: Aircraft Striker game, UIFramework (IUIViewFactory, GameLifecycleManager)  
**Status**: Resolved

## What Happened

Shipped the full Aircraft Striker 2D game (casual top-down portrait aircraft shooter) as an isolated feature inside the UIFramework, plus 5 critical framework enhancements to support it. Game is fully playable: 10 waves, boss every 5 waves, 3 weapon levels, progression via PlayerPrefs. Discovered and fixed 4 significant bugs during development, each exposing real architectural gaps.

## The Brutal Truth

We built Aircraft Striker exactly as planned. Zero scope creep, zero "just add feature X." That felt almost suspicious until the bugs came. Four separate issues hit us during gameplay testing — each one was a *framework* gap we didn't anticipate, not a game logic problem. The real frustration: three of these bugs *silently failed*. No exceptions, no error logs. Just players hitting pause, then restarting, and the game respawning at wave 1 instead of continuing. That's a soft failure that would tank user trust in production.

The framework modifications (SetScopeContainer, RestartCurrentStateAsync) feel necessary in retrospect. They unlock proper DI isolation for game-specific services, which we need for future feature games too.

## Technical Details

### What Was Built

**Aircraft Striker Assembly** (`UIFramework.AircraftStriker`):
- `AircraftLifetimeScope` — extends `UIFrameworkLifetimeScope`, calls `SetScopeContainer(Container)` to expose game services to ViewModels
- `GameplayController` — pure C# game logic (Player, enemies, collision, scoring)
- `AircraftPoolManager` — wraps `UnityEngine.Pool.ObjectPool<T>` for bullets, enemies, pickups (zero-allocation during gameplay)
- `GameLifecycleManager` — state machine with registered states (MainMenu, Gameplay)
- 5 Views/ViewModels: MainMenu, HUD, Pause, GameOver, Victory
- `AircraftInputHandler` — `IDragHandler` for touch input
- `WaveManager` — MonoBehaviour coroutines for wave/boss spawning
- `ProgressionService` — PlayerPrefs-backed unlock and coin tracking (prefixed `aircraft_`)

**UIFramework Enhancements** (`Packages/com.sinkii09.uiframework/`):
1. **IUIViewFactory.SetScopeContainer() / ResetScopeContainer()** — Allows game-specific child scope. ViewFactory reads `_scopeContainer` (volatile) in both Create paths, falls back to `_container` if null. Enables Aircraft game to inject `IProgressionService`, `WaveManager`, etc. visible only to game ViewModels, not framework ones.
2. **Early cancellation token check** — `ct.ThrowIfCancellationRequested()` before cache lookup prevents resource allocation on cancelled operations.
3. **UIViewKeyAttribute.Key support** — Fixed addressable loading to read full key path (e.g., `"AircraftStriker/AircraftHUDView"`) instead of fallback to type name.
4. **GameLifecycleManager.RestartCurrentStateAsync()** — Exits and re-enters current state, bypassing same-state guard. Used by Pause Restart and retry paths.

### Bugs Discovered & Fixed

**Bug 1: Two lives decrease on one hit** (Multi-hit from burst/spread bullets)
- **Root cause**: `GameplayController.OnPlayerHit()` had no guard against multiple bullets hitting in the same physics step. The `_isGameActive = false` guard only fired on death, so a burst of 3 bullets = -3 lives instantly.
- **Fix**: Introduced `_invincibleUntil` timestamp (1.5s i-frame window) + `IsInvincible` property. Each hit outside i-frames triggers damage + invincibility window. On death, i-frames are NOT granted (prevents cheese invincibility on last life).
- **Visual feedback**: `PlayerController.Update()` blinks sprite at 10 Hz during i-frames (runs outside `IsGameActive` guard, so sprite restores to visible when game stops).
- **Pain point**: This took 2 hours to debug because the multi-hit felt random. Spawning 3 enemies in burst, hit once, lose 3 lives. "Did I get hit three times?" — no, burst fire. Once we saw the second enemy projectile fly through the player sprite before collision detection cleared, root cause was obvious. Should have added collision filtering or temporal guards earlier.

**Bug 2: Significant delay before GameOver buttons appear** (~2s wait after player death)
- **Root cause**: `AircraftGameOverView.OnShowAsync` had _retryButton appearing at timestamp `1.9f`, _menuButton at `2.0f`, after a 1.1s score count-up sequence. Players died, then waited 2.3 seconds before buttons appeared.
- **Fix**: Parallelized button entrance with count-up (buttons at `0.80f` / `0.90f`), shortened count-up from 1.1s to 0.7s. Total GameOver sequence: ~1.42s instead of ~2.3s.
- **Pain point**: This was a usability bug, not a crash. Players kept tapping the screen waiting for buttons. No error, just slow UX. Caught during internal gameplay test runs.

**Bug 3: Game not pausing + restart broken** (Time.timeScale deadlock in PauseView)
- **Root cause**: Pause needed `Time.timeScale = 0f` to freeze enemies. But `UIViewBase.OnShowAsync` runs enter animation (DOTween) BEFORE calling `OnShowAsync()` hook. If we set timeScale=0 in hook, DOTween freezes mid-animation, navigator can't proceed. Timeline: enter animation starts → set timeScale=0 → animation frame-locks → navigator waits for animation → deadlock.
- **Fix**: **New pause pattern** — `OnShowAsync()` sets `Time.timeScale = 0f` (after entrance animation via `await UniTask.Delay()`). Each ViewModel exit handler (Resume/Restart/Menu) restores `Time.timeScale = 1f` **synchronously** BEFORE the async call. Timeline: pause enters → animation finishes → timeScale freezes → user taps resume → timeScale restores → hide animation plays at normal speed.
- **Pain point**: This hung the game for 10 seconds. Pressing pause, the pause menu appeared but the game kept running, and tapping Restart did nothing. Tracked this to "UINavigator seems broken" before realizing `await Sequence()` was blocking on a frozen animation. Root cause: **time-dependent async code (DOTween) doesn't mix with frozen timeScale**. Solution: reorder lifecycle hooks, keep timeScale modifications outside animation windows. This pattern should be documented in UIFramework guidelines.

**Bug 4: Restart via pause used raw navigator call** (State machine bypass)
- **Root cause**: `AircraftPauseViewModel.GoToMainMenuAsync()` called `_navigator.CloseAllAsync()` + `ShowAsync<AircraftMainMenuView>()` directly, skipping `GameLifecycleManager.ChangeStateAsync<>()`. Navigator closed views, but `GameLifecycleManager._currentState` remained `AircraftGameplayState`. Next time navigation happens, lifecycle check "Already in Gameplay state, skip" → state didn't exit → enemies spawned in menu.
- **Fix**: All state transitions route through `_lifecycle.ChangeStateAsync<T>()`. Exit old state, update `_currentState`, enter new state atomically. New `RestartCurrentStateAsync()` handles replay scenarios (exit + re-enter same state).
- **Pain point**: This was silent too. No error. Pause menu appeared, tapped "Menu", landed on MainMenu view, tapped "Play", enemies spawned correctly, but internal state machinery was out of sync. If player paused again, the old state was still "Gameplay" internally. Subtle, footgun-level bug that would have caused cascading failures in longer play sessions.

## What We Tried

1. **Initial i-frame attempt** — added boolean `_isInvincible` flag in `OnPlayerHit()` callback, reset after 1s via `Invoke()`. This worked for single hits but couldn't handle burst fire because multiple callbacks fired in the same frame before Invoke registered the reset. Switched to timestamp-based (`_invincibleUntil`) which is immune to callback ordering.

2. **Animation wait pattern for pause** — tried `await UniTask.Delay(500)` after setting timeScale=0, but if animation is 0.4s and we wait 0.5s, we're trying to set timeScale=0 DURING the hide animation of whatever was showing. Moved the wait to AFTER OnShowAsync completes (via task chaining), not within the hook.

3. **Custom state machine gate** — briefly considered adding a "state change lock" to GameLifecycleManager to prevent reentrancy. Rejected. The real issue was bypassing the state machine entirely. Fix: require all transitions to use the machine, not work around it.

## Root Cause Analysis

### Why These Bugs Existed

1. **No integration test coverage** — We had unit tests for `GameplayController`, but no end-to-end test that: pauses mid-wave, hits pause, restarts. Would have caught bugs 3 and 4 immediately.

2. **Lifecycle hook assumptions** — The UIViewBase lifecycle is: enter animation → `OnShowAsync()` hook → wait for completion. We assumed `OnShowAsync()` is "after animation," but it's actually "before the task completes." This is a subtle contract. DOTween operations inside it block animations.

3. **State machine trust violation** — We had `GameLifecycleManager` designed to be the single source of truth for state transitions, but nothing enforced it. ViewModels could call navigator directly. That's a discipline issue, not a design issue. In retrospect, should have made it impossible to call navigator without going through `ChangeStateAsync<>()`.

4. **No spec for multi-hit scenarios** — Game spec said "enemies deal 1 damage on contact." Didn't explicitly say "multiple enemies can't hit in one frame." Burst fire and collision detection created that scenario naturally. Should have anticipated it.

## Lessons Learned

1. **Timestamp-based guard logic is more robust than boolean state.** If you're guarding against repeated rapid events (hits, taps, submissions), use `_nextAllowedTime = Time.realtimeSinceStartup + delay` and check `Time.realtimeSinceStartup >= _nextAllowedTime`. Boolean state gets wedged by callback ordering or frame skips.

2. **Time.timeScale and DOTween don't mix casually.** If you're using DOTween animations and need to freeze time:
   - Pause AFTER animation completes, not during
   - Restore time BEFORE animations resume
   - Use `Time.realtimeSinceStartup` for UI effects that must run during pause
   - Document the pause contract so future features don't assume timeScale changes are safe everywhere

3. **Silent failures are worse than crashes.** The three state machine and pause bugs had no exception, no warning. They just... didn't work. A crashing bug gets noticed in 30 seconds. A silent bug gets shipped, and players blame the game, not us. Add assertions: `Debug.Assert(_currentState == expected, "State mismatch")` at critical junctions.

4. **State machines must be the only path to state change.** Not a suggestion, a rule. Make it architecture-level: `GameLifecycleManager` is the only thing that can change `_currentState`. ViewModels don't call navigator, they call `_lifecycle.ChangeStateAsync<>()`. Prevent the bypass at design time, not at runtime.

5. **SetScopeContainer pattern is now essential for multi-game projects.** Framework services (IUIViewFactory, UINavigator) and game services (ProgressionService, WaveManager) must be isolated. Child scope + SetScopeContainer allows ViewModels to resolve both without coupling. This pattern will repeat for future games — document it.

## Next Steps

1. **Add pause/restart integration tests** — `GameplayController` → pause → change state to pause → verify timeScale freezes → change state back → verify timeScale unfrozen. Catch ordering bugs in CI.

2. **Document pause pattern in UIFramework** — TimeScale changes, hook ordering, realtimeSinceStartup usage. Future features will need this.

3. **Add state change assertions** — `GameLifecycleManager._currentState` should assert it only changes via `ChangeStateAsync<>()`. Add telemetry or debug logs: `[Lifecycle] Transitioning Gameplay → Pause`.

4. **Consider blocking navigator on gameobject** — Instead of "trust ViewModels to use ChangeStateAsync," make direct navigator calls from ViewModels throw. Enforce architecture at compile time (or init time via DI interception).

5. **Expand Aircraft Striker with new waves/skins** — Game is solid now. Future work: add 5 more waves, 2 more skins, persistent leaderboard via cloud save. No framework changes needed.

---

## Unresolved Questions

- Should UIViewBase lifecycle be documented more explicitly? The hook ordering (enter animation → OnShowAsync → OnHideAsync → hide animation) surprised us during pause implementation.
- Is RestartCurrentStateAsync too specialized, or is this a common pattern we'll see again? Keep watch for other games needing "replay this level."
- PlayerPrefs for progression is fine for now, but should we have a formal PersistenceService abstraction? Probably overkill until we have cloud save.
