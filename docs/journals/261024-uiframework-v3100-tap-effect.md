# UIFramework v3.10.0: Tap Effect (Spawner + Ripple) Shipped, Mouse Tests Lied for a Frame

**Date**: 2026-10-24 (time not recorded)
**Severity**: Medium
**Component**: UIFramework (Runtime tap effect: TapEffectSpawner, TapRipple; menu GameObject > UI > UIFramework > Tap Effect)
**Status**: Resolved (shipped), one manual device check open

## What Happened

Added a tap-ripple feedback effect to the framework. One InputAction on `<Pointer>/press` drives a ring of up to 8 pooled ripple instances. Restart = `SetActive(false)` → move → `SetActive(true)`. Each ripple grows 0.3 → 1 with a CanvasGroup fade over 0.35 s unscaled, then switches itself off. A menu item builds a root overlay canvas (sortingOrder 500, no GraphicRaycaster) with a built-in Knob.psd sprite.

Framework commit c650c18, tag v3.10.0. TheEnd repinned (cef8839) and docs updated (fcea4d1). Not wired into any TheEnd game; user decision.

Standing rule from the user: simplest readable design. Rejected multi-touch, trails, filters, UIRoot-layer/DI integration, and auto-spawn.

## The Brutal Truth

The feature is tiny. The debugging was not. Two mouse-input PlayMode tests returned 0 ripple instances while touch passed, and the first hypothesis (Game view focus) was a waste of twenty minutes. The real cause was a one-frame timing gap we only saw by instrumenting it.

## Technical Details

- Mouse `InputTestFixture.Press` takes effect on the NEXT frame in PlayMode. `BeginTouch` is immediate. Tests asserting right after Press were checking a frame too early.
- Fix: yield one frame after input injection before asserting.
- Plan review (Opus) approved with 4 test-quality corrections: tests that could not fail, a `timeScale = 0` hang, and wrong input-fixture setup order.
- Mutants caught: base-scale capture bug, `anchoredPosition` bug.
- PlayMode suite 553/553, including 10 tap tests.
- QA found a real bug: the active scene template played the ripple once, kept its end scale, and copies inherited it. Fixed with a `Start` reset (reviewed).

## Tooling Pain

- Unity-MCP plugin update cut the tool list to 18 (RQL replaces the editor-/assets-/gameobject-* tools). It looked like a dead bridge.
- `tests-run` tracker wedged after a 5-minute timeout and reported "another run in progress" indefinitely. Worked around by calling `TestRunnerApi` through script-execute and reading `TestResults.xml`.

## Known Limitations (kept on purpose)

- Replay after a parent toggle does not retrigger.
- Primary pointer only (no multi-touch).
- Two spawners produce two ripples.

## Root Cause Analysis

Input tests assumed all pointer types resolve within the same frame. Mouse does not. The framework's input fixture behaves differently per device, and nothing in our helper documented that.

## Lessons Learned

- When a device-specific test passes and its twin fails, diff the timing before diffing the environment.
- Any input test helper must yield a frame after injection. Document this at the helper, not in someone's memory.
- A "works in the scene" check must cover the first play of a freshly loaded template, not a warmed-up instance.

## Next Steps

- Manual device check (owner: user): touch double-fire on WebGL and touch laptops. Automated tests cannot cover this.
- Tracker wedge: report to the Unity-MCP maintainers with the 5-minute timeout repro.
- Wire into a TheEnd game only when the user asks.
