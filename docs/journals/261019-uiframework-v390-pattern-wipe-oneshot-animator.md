# UIFramework v3.9.0: Pattern Wipe + OneShotAnimator Moved Into the Package

**Date**: 2026-10-19
**Severity**: Medium
**Component**: UIFramework (Runtime/Core/Animation/PatternWipe, Runtime/Juice), AircraftStriker transition overlay
**Status**: Resolved (shipped), two defects found and deferred to next patch

## What Happened

Moved TheEnd's game-level Pattern Wipe (PatternWipeTransition, PatternWipeSurface, shader renamed "UIFramework/UI/Pattern Wipe", 5 MIT textures from Coffee.UIEffect) into the package. Generalised ShineRevealPlayer into OneShotAnimator (Play / Play(state) / PlayAsync / Complete / IsPlaying; Animator off at rest). Shine clip/controller/prefab/scene became the package sample "Shine Reveal" (needs Coffee.UIEffect; Feel's paid art swapped for a built-in sprite since the repo is public). TheEnd keeps only the wipe presets.
Package 55a46cb + tag v3.9.0. TheEnd 19603dd (pin, index-only so the user's unrelated com.ivanmurzak.unity.mcp 1.2.0 manifest change stayed unstaged) and 01a479c (presets + docs). All pushed.

## The Brutal Truth

The move itself was clean. What stings is the bug we found afterwards: an earlier "verified in real flow" check was wrong. It passed only because it ran after a manual show had already woken the overlay. Our verification warmed up the exact thing it was meant to test.

## Technical Details

- Files moved WITH .meta (same GUIDs), so every scene/prefab/asset reference survived. Script rename = give OneShotAnimator.cs the GUID from ShineRevealPlayer.cs.meta.
- Delete project copies BEFORE resolving the local package; duplicate GUIDs get silently re-assigned.
- Samples~ with GUID-linked assets must ship .meta. A UnityEvent's `m_TargetAssemblyTypeName` needed a hand rewrite. ForceReserializeAssets leaves stale `m_EditorClassIdentifier` (harmless).
- Diff review W1: a never-activated, destroyed object could strand a PlayAsync awaiter (`isActiveAndEnabled` throws before the previous run completes). Fixed with `this == null` before any field write.
- W2: a test passed because a 0.1 s clip simply ran out. Fixed with a 10 s state. Both destroy-ordering tests mutation-checked (fail when fix reverted).
- Shine clip is really 0.417 s, not the 0.8 s earlier notes claimed.
- Gates: Opus plan review (6 corrections folded in), Opus diff review, QA 9 pass / 0 fail / 1 untestable. EditMode 265/265, PlayMode 540/540 (40 new).

## Found, deferred (user decision)

1. FIRST transition of every Play session shows no curtain. `TransitionOverlayView.Awake` does SetActive(false); AS_Bootstrap has saved the overlay inactive since 2097be8, so the first ShowAsync wakes it and Awake hides it again.
2. Every Play logs "Boot transition was Rejected": AircraftGameBootstrap shows the main menu via raw navigator ShowAsync, so GLM's boot `ChangeStateAsync<BootState>` arrives mid-transition (same family as the known navigator-bypass bug).

## Friction

Unity Editor hung at licensing for ~20 min mid-cutover; the phase was paused before any deletion, so nothing was half-moved. Bash-tool heredocs containing apostrophes fail to parse.

## Lessons Learned

- Probe the FIRST transition of a fresh Play session. A check run after a manual warm-up can hide exactly the bug you are hunting.
- Pause destructive cutover steps at a safe boundary; the licensing hang cost time, not state.
- A test that can pass by timeout is not a test; use a state far longer than the assertion window.

## Next Steps

- Next package patch (owner: user/Claude): fix overlay first-show (Awake vs saved-inactive overlay) and route boot menu through ChangeStateAsync<T>. Add a fresh-Play first-transition PlayMode test.
- Update vault notes for Pattern Wipe / Shine Reveal to point at the package locations.
