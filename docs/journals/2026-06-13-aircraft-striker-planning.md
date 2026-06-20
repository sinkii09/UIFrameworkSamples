# Aircraft Striker 2D Game — Planning Session Complete

**Date**: 2026-06-13 14:43  
**Severity**: Informational  
**Component**: Game Architecture / Planning  
**Status**: Completed

## What Happened

Completed comprehensive 8-phase implementation plan for a standalone casual 2D top-down portrait aircraft striker game integrated into the existing Unity 6 project (alongside Memory Flip Card Game). Plan document created at `plans/260613-1443-aircraft-striker-2d-game/` with full phase breakdowns, architecture diagrams, and technical specifications. **Code implementation has not started** — this was planning and research only.

## The Brutal Truth

This plan exists because we learned the hard way from the Memory Game that jumping into UI implementation without a clear architecture leads to tangled ViewModels, event bus regrets, and rework cycles. This time, we're investing 1-2 hours upfront to avoid 8 hours of refactoring later. The decision to keep this game completely isolated (its own assembly, scene, lifetime scope) came from realizing that coupling to the MemoryGame would be a debt dragon. Better to pay the composition cost now.

## Technical Details

**Architecture decisions locked in:**

- **Isolation**: Own assembly (`UIFramework.AircraftStriker.asmdef`), own scene (`AircraftGame.unity`), own `AircraftLifetimeScope extends UIFrameworkLifetimeScope`. Zero coupling to MemoryGame.
- **Input**: `IDragHandler` on transparent canvas panel — simple, no New Input System package needed, plays nicely with Unity EventSystem.
- **Game Logic**: Pure C# `GameplayController` (not MonoBehaviour) owns `PlayerData`, pushes directly into reactive `AircraftHUDViewModel` properties.
- **Wave/Boss Timing**: `WaveManager` IS a MonoBehaviour (needs coroutines), registered via `RegisterInstance` in lifetime scope.
- **Pooling**: `UnityEngine.Pool.ObjectPool<T>` — allocation-free, no custom pool boilerplate.
- **Progression**: `ProgressionService` backed by PlayerPrefs, keys prefixed `aircraft_` to avoid collisions.
- **Views**: 7 total (MainMenu, HUD, Pause, GameOver, Victory, Shop, SkinSelection) all extending `UIView<TViewModel>` on Sinkii09 UIFramework.
- **VFX**: Existing MoreMountains.Tools (MMFeedbacks) for boss entrance camera shake + player hit vignette.
- **Features**: 10 enemy waves, boss every 5 waves, 3 weapon levels (Single/Double/Spread), 3 ship skins, shop with coin progression (1 coin per 50 score).

**Phase breakdown:**
1. Setup & Architecture (1d) — DI, lifetime scope, bootstrap
2. Game Logic Pure C# (1d) — Player, Enemy, Wave, Boss classes
3. Object Pooling & Input (0.5d) — Pool manager, drag handler
4. UI Views & ViewModels (2d) — HUD, Shop, Pause, menus
5. Gameplay Systems (2d) — Weapon upgrades, collision, scoring
6. Audio & Progression (0.5d) — SFX, coin save, unlocks
7. Visual Polish (1d) — Animations, screen effects, transitions
8. Integration & Testing (0.5d) — Full-feature test, ship it

**Total estimate: 8.5 days.** No new packages required — all dependencies already in project.

## What We Tried

Nothing yet — this is pre-implementation. But we deliberately chose paths based on lessons from prior systems:
- Memory Game's event bus was over-engineered → this time, push directly into ViewModels via reactive properties
- MonoBehaviour proliferation caused test friction → `GameplayController` is pure C#, only `WaveManager` is MonoBehaviour
- Input System integration delayed first release → this time, simple `IDragHandler` on canvas

## Root Cause Analysis

Why plan comprehensively now? Because shipping without architecture clarity has historically cost 3+ days of rework per system in this project. The Memory Game's view lifecycle bugs, UINavigator transition guards, and UIViewFactory auto-registration all traced back to "I'll figure it out during implementation." This plan locks design decisions upfront so implementation is boring and predictable.

## Lessons Learned

1. **Isolation is worth the composition cost.** Temptation to share code between MemoryGame and AircraftStriker is strong. Resist it. Two games, two assemblies, zero shared logic = cleaner, easier to test, easier to ship independently.

2. **Pure C# game logic is non-negotiable.** MonoBehaviours are controllers, not logic holders. `GameplayController` being pure C# means it's unit-testable, doesn't need scenes, and doesn't leak Unity lifecycle into game math.

3. **Choose boring over clever.** `IDragHandler` instead of New Input System. `PlayerPrefs` instead of cloud save. `UnityEngine.Pool` instead of custom allocation strategy. Ship faster, support better.

4. **ViewModels are a buffer, not a tunnel.** Don't make ViewModel properties complex. Push data in (from GameplayController), expose clean properties (to Views). No business logic in ViewModels.

5. **MonoBehaviours for timing, pure C# for logic.** `WaveManager` needs coroutines to delay waves naturally. Forcing it to pure C# would mean manual time tracking. MonoBehaviour + registered as instance is the right call.

## Next Steps

1. **Phase 1 (Setup & Architecture)** — Create folder structure, `AircraftLifetimeScope`, `AircraftGameBootstrap`, bootstrap DI container. Estimated 1 day. Owner: TBD (next task assignment).

2. **Validation gate** — Before Phase 2, confirm folder structure + bootstrap compiles + scene loads without errors. Non-negotiable.

3. **Parallel research** — While Phase 1 is in progress, research Wave/Boss AI tuning parameters (spawn rates, enemy damage scaling, boss patterns).

4. **Code review on plan** — (Optional but recommended) Have reviewer check plan.md and phase files for missed edge cases, architectural risks, or breaking changes to UIFramework assumptions.

**Plan is LOCKED** — no implementation until this is written. Phase 1 can begin immediately after.

---

## Unresolved Questions

None at planning stage. All technical choices made with rationale. Ready to ship Phase 1.
