# UIFramework v3.9.1: Resident Views Switched Themselves Off, Boot Was Rejected Every Play

**Date**: 2026-10-19
**Severity**: High (no curtain, invisible first toasts, every Play booted outside the state machine)
**Component**: UIFramework (UIViewBase, TransitionOverlayView, NotificationHostView, TooltipViewBase), AircraftStriker bootstrap
**Status**: Resolved (shipped)

## What Happened

Fixed the two defects deferred out of v3.9.0. Package commit 6d9abaf, tag v3.9.1. TheEnd: c39071f "build(deps): pin UIFramework to v3.9.1" and ef4b946 "fix(aircraft-striker): enter the main menu through the lifecycle state machine".

## The Brutal Truth

Both bugs were silent. The first one raised no exception and no warning. The boot rejection had been logged on every Play since 3d61ece, and nobody looked until v3.9.0 QA. All 805 package tests (265 EditMode + 540 PlayMode) were green the whole time, because none of them started from a view saved inactive or a fresh Play session.

## Technical Details

**Bug 1: resident pre-hide undoes its own first show.** `TransitionOverlayView`, `NotificationHostView` and `TooltipViewBase` call `SetActive(false)` in Awake so a scene-saved-active view never flashes. A view saved inactive has not awoken yet, so Awake runs inside the `SetActive(true)` that shows it and switches it straight back off. Result: no curtain on the first transition, first toasts drawn on an inactive host, first tooltip measured while inactive. Pre-fix probe: `overlayActive=False` for the entire wipe.

Fix: `UIViewBase.ActivateForShow` (internal flag wrapped around `SetActive(true)`) plus `PreHide` (`private protected`, returns early while the flag is set).

**Bug 2: raw navigator call races boot.** `AircraftGameBootstrap.Initialize` (IInitializable, runs before GLM's `IAsyncStartable.StartAsync`) did a raw navigator `ShowAsync` of the menu. GLM's boot `ChangeStateAsync<BootState>` was then refused, `CurrentState` stayed null. Fix: `EnqueueStateChange<AircraftMainMenuState>()`. The user chose the boot curtain wipe over adding a new package API.

## Reviewer Catch

The Opus plan review noticed that CanvasGroup is cached in Awake, so on a never-woken view it is null. The old "set alpha before activate" was a silent no-op. Callers now write alpha after activation. It also killed a vacuous test: asserting alpha 1 proves nothing because a fresh CanvasGroup defaults to 1.

## Found While Verifying

- Post-fix boot logged "CloseAllAsync ignored". Three dead `CloseAllAsync` calls in AircraftMainMenuState/AircraftGameplayState were always refused inside state transitions (navigator already cleared the stack). They were hidden because the old path skipped them. Removed with user approval.
- Diff review: a Play click during the curtain reveal is dropped by GLM's `_isTransitioning` guard. The menu Play button now enqueues. Probe: click at reveal 0.20 enters gameplay, console clean.

## Verification

EditMode 265/265, PlayMode 544/544 on the git package. 3 regression tests mutation-checked: removing PreHide's early return fails exactly those 3. AS_Bootstrap probes before and after.

## Lessons Learned

- Probe the FIRST transition of a fresh Play session. This is the lesson v3.9.0 taught; this time the before/after probe was built for it from the start.
- Fixing a race can surface latent warnings on a path that used to be skipped. Expect them, do not blame the fix.
- Writes before activation on a never-woken MonoBehaviour hit null cached fields. Order matters.
- Check that a test can fail at its default value before trusting it.

## Next Steps

- Known and out of scope: a resident view whose PARENT is inactive when shown still pre-hides later in Awake. Separate trigger, unplanned.
- Rule now in the vault (GameLifecycleManager): buttons on a state's first screen enqueue, since GLM is still transitioning during the reveal.
- Vault notes updated: Known Gotchas, Transition Overlay System, GameLifecycleManager, Core View Layer, Notification System.
