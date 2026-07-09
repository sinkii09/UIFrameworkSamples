# Aircraft Striker: Hit Effect VFX & Bullet Pattern Expansion

**Date**: 2026-06-23
**Severity**: Medium
**Component**: Aircraft Striker / VFX System, Bullet Patterns
**Status**: Resolved

## What Happened

Implemented two feature batches: (1) Hit effect particle system with color-coded burst at bullet impact, (2) 4 new bullet pattern types + config overhauls.

Hit effects use pooled particle bursts spawned on collision. Colors by type: Enemy=orange, Boss=gold, Player=cyan. Auto-return via `UniTask.Delay` after lifetime; double-release guard prevents corruption.

Expanded `BulletPatternType` enum: `TripleSpiral`, `CrossSpiral`, `Pincer`, `OscillatingWall`. Rewrote pattern executor with 4 new switch cases. Tuned all 9 existing configs: faster burst delays, more bullets, shorter intervals (BossDual8: 3s → 1.5s).

## The Brutal Truth

Particle texture generation was a friction point. Unity 2022+ returns null for `Default-Particle.png`, so added procedural gaussian soft-circle texture generation (saved as asset). The workaround works but it's another editor-time step the team has to remember.

Pattern expansion felt straightforward until C# switch scoping bit us. Declaring variables in cases without braces causes ALL cases to share scope — adding `float ts` in TripleSpiral would conflict with existing `float dual` in DualSpiral. Wrapping each new case in `{ }` solved it, but this is a sneaky footgun for future pattern additions.

## Technical Details

- **Switch scoping bug**: C# allows variable redeclaration across case blocks only if each is wrapped in braces. Missing braces = `CS0136 local variable shadowing` errors.
- **Trig math error**: Applied `Mathf.Deg2Rad` to frequency scalars (wrong); must apply only to degree values before sin/cos.
- **Texture generation**: `BulletVFXBuilder.cs` creates runtime gaussian texture via `SetPixels32` + gaussian kernel, saved to Assets.
- **OscillatingWall design**: Reuses `SpiralStepDegrees` as oscillation frequency, `StartAngle` as swing amplitude — unconventional but avoids new enum fields.
- **Pincer design**: `SpreadAngle` = arm separation, `StartAngle` = per-arm arc width.

## Root Cause Analysis

Texture issue: Unity removed built-in particle texture in 2022+. Should have documented this during Aircraft Striker initial setup.

Switch case scoping: Copy-pasted DualSpiral block without reading C# scoping rules. This is a pattern trap for future contributors.

## Lessons Learned

1. **Procedural textures in editor tools** — Document the why (built-in removed) in code comments; future developers won't know this is a workaround.
2. **Switch case best practice** — Wrap ALL cases with new local variables in braces, even if only one case has them. Consistency prevents merge/extension bugs.
3. **Config design flexibility** — Reusing existing fields (SpiralStepDegrees, StartAngle) beats adding new enum values, but document the semantic mapping clearly.

## Next Steps

- Manual Unity step: Run `AircraftStriker → Setup Wizard → Build Hit Effect Prefab` to generate prefab
- QA: Verify hit effect colors render correctly; check pooling doesn't leak under rapid fire
- Future: Consider a base `PatternConfig` validator in editor to warn on unused/misused fields
