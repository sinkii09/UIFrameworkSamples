# Memory Flip Card Game on UIFramework — Architecture & Review Learnings

**Date**: 2026-06-06
**Severity**: Medium
**Component**: UIFramework.MemoryGame (new assembly) + SampleLifetimeScope
**Status**: Complete — pending prefab setup

---

## What Happened

Implemented a full 4×4 memory matching game (8 pairs) as an MVVM feature on top of the UIFramework. Game logic is pure C# with zero Unity dependencies; presentation is DOTween animations + R3 reactivity. The feature required two full review cycles before implementation could begin — the first review caught API misunderstandings that would have blocked the feature entirely.

---

## The Brutal Truth

This implementation exposed a painful gap in my understanding of the UIFramework's public API surface. I drafted code using methods and properties that simply don't exist (`ContinueWith`, `Navigator` property on UIViewBase). The pre-implementation review saved hours of debugging by catching these **before** I wrote a single line. 

The frustrating part: I had the APIDocumentation in front of me but skimmed it instead of reading carefully. The lesson stung — architectural reviews are not optional gatekeeping, they're **insurance against shipping code that can't compile**. And worse: I nearly broke the pattern twice more after fixes (dead code, missing braces, undisposed subjects) because I wasn't reviewing my own work closely enough before submitting.

---

## Technical Details

### Architecture Built

**Game Logic Layer** (`MemoryCardGame.cs`, `CardData.cs`, `FlipResult.cs`):
- Pure state machine: tracks matched pairs, turn count, game won state
- No MonoBehaviour, no View dependencies
- `TryFlip()` returns `FlipResult` enum (ValidFlip/Mismatch/AlreadyMatched/GameWon)

**Presentation Layer** (MVVM):
- `GameplayView` (MonoBehaviour) + `GameplayViewModel` (IViewModel)
- `WinView` + `WinViewModel` + `WinArgs`
- `MemoryGameState` (IGameState) registered in VContainer scope
- New assembly `UIFramework.MemoryGame` (asmdef with VContainer + R3 references)

**Animation & Reactivity**:
- `CardView`: DOTween `ScaleX` half-flip pattern (flip=0.5 scale when face-down)
- Subjects for View-to-ViewModel communication: `FlipToFront`, `FlipToBack`, `MatchConfirmed`, `GameWon`
- ViewModel owns mismatch delay loop (800ms UniTask)
- Timer loop: `UniTask.Delay(100ms)` in ViewModel, cancelled via CTS in `OnHide()`

### Key Decisions & Why

| Decision | Alternative | Trade-off |
|----------|-------------|-----------|
| ViewModel owns mismatch delay, not View | Delay in CardView | ViewModel has game state context to decide when/how to reveal |
| R3 Subjects for animation triggers | Direct View method calls | Decouples animation from logic; Subjects can buffer/replay if needed |
| Grid spawn in `OnShowAsync`, not `BindViewModel` | Spawn in constructor | `vm.OnShow()` runs before `OnShowAsync` — `_game` would be null |
| Direct `IUINavigator` injection in View | Auto-discovered via UIViewBase | UIViewBase doesn't expose Navigator as a property |
| `async UniTaskVoid` sequential awaits | Hypothetical `ContinueWith` | UniTask has no `ContinueWith(Func<UniTask>)` overload; sequential awaits are idiomatic |

---

## What We Tried

1. **First draft plan**: Used `ContinueWith(Func<UniTask>)` on UniTask result → Reviewer: method doesn't exist
2. **First draft plan**: Accessed `Navigator` property on UIViewBase → Reviewer: property doesn't exist on base class
3. **During first post-impl review**: Subject fields created but not disposed → added to `_disposables`
4. **During first post-impl review**: Button listeners added to `_showDisposables` but not via `Disposable.Create()` → reviewer caught missing cleanup
5. **During first post-impl review**: `CardView.AnimateFlip()` called `DOScale(0.5f)` without killing prior tweens → fixed with `transform.DOKill()` guard

---

## Root Cause Analysis

**Why the initial API misunderstandings?**
- Assumption-driven coding: I assumed common async patterns (ContinueWith) would exist in UniTask without verifying
- Skimmed documentation instead of reading method signatures carefully
- Didn't test the plan code mentally against actual API surface before submitting

**Why the animation and listener cleanup issues?**
- Post-implementation review (second cycle) caught what pre-review missed
- Reviewer read actual implementation, not plan — differences surfaced
- I was moving fast and trusting my implementation quality; didn't self-review before submitting

---

## Lessons Learned

1. **APIs are written in code, not assumptions**: Read the actual method signatures. If a library doesn't have `ContinueWith`, use what it does have (sequential awaits, `AndThen`, etc.)

2. **Pre-implementation review isn't a gate, it's insurance**: Catching broken plans before coding saves rework. The Approved verdict means the architecture is sound.

3. **Post-implementation review is mandatory, not optional**: Differences always exist between plan and code. Reviewers see what I missed: undisposed objects, missing guards, leaked listeners.

4. **Disposal and cleanup are load-bearing**: R3 Subjects and DOTween tweens don't magically clean themselves. Track them in `_disposables` and `_showDisposables`. `DOKill()` before reusing tweens.

5. **Grid spawning timing matters**: `BindViewModel` runs before `OnShowAsync` — if you need initialized state, spawn in `OnShowAsync`, not constructor or BindViewModel.

6. **Review-Approved-then-Implement is not slowing you down**: Two review cycles + implementation took less time than one incorrect implementation + debugging. The process works.

---

## Next Steps

1. **Manual prefab setup** (cannot be scripted):
   - Create `Assets/Resources/Card.prefab` with `CardView` component
   - Create `Assets/Resources/GameplayView.prefab` with `GameplayView` component and grid container
   - Create `Assets/Resources/WinView.prefab` with `WinView` component and buttons
   - Assign 8 card face sprites to `GameplayView._cardFaceSprites[]` in Inspector

2. **Integration test**:
   - Boot game in Editor, navigate to Play
   - Verify 4×4 grid spawns with correct card layout
   - Test mismatch reveal (2 different cards flip back after 800ms)
   - Test win state triggers when all pairs matched
   - Verify animations (scale flip, smooth tween) play without stuttering

3. **CI/CD**: Ensure `UIFramework.MemoryGame` assembly builds without errors in IL2CPP and Mono

4. **Documentation**: Update `docs/system-architecture.md` to include MemoryGame feature overview + MVVM flow diagram

---

## Emotional Reality

Relieved and a bit humbled. The two-cycle review process felt slow at first — "just implement already!" — but it prevented a complete rewrite. Shipping code that doesn't compile is worse than shipping slightly later. The hardest part was resisting the urge to "just code" when I had incomplete information. Next time, I'll trust the process earlier.

Also learned that I need to read more carefully. Assumptions about how libraries work are expensive. The three minutes I should have spent reading UniTask docs saved me two hours of debugging.
