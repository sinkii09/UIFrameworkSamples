# Git URL Stale Entries Break Dependency Resolution

**Date**: 2026-05-31 13:12  
**Severity**: High  
**Component**: Unity 6 UIFramework Package (Packages/manifest.json, UIFrameworkInstallerWizardSteps.cs)  
**Status**: Resolved  

## What Happened

Packages/manifest.json contained stale git URL entries for UniTask and VContainer from a failed previous install attempt, and R3 was completely missing. No OpenUPM scoped registry existed. Git URLs bypass OpenUPM's transitive dependency resolution, causing R3 installation to fail with missing dependencies.

## The Brutal Truth

This was maddening because the wizard code was *already correct* — the problem was upstream in the manifest state. Spent initial cycles debugging wizard logic before realizing the manifest was poisoned from a pre-wizard manual install. Classic case of blaming the wrong layer.

## Technical Details

**Error:** R3 installation failed due to missing Newtonsoft.Json implicit dependency.  
**Root cause:** Git URLs don't resolve transitive deps; R3 requires OpenUPM registry.  
**Manifest poison:** UniTask (git), VContainer (git), R3 (absent), no scoped registry.

## What We Tried

1. Assumed wizard Step1_InstallDeps logic was broken — debugged path logic, IndexOf bounds
2. Realized manifest structure was stale from pre-wizard manual attempt
3. Rebuilt manifest.json from scratch with proper OpenUPM entries

## Fix Applied

**Packages/manifest.json:**
- Replaced git URLs with OpenUPM versions (UniTask 2.5.11, VContainer 1.18.0, R3 1.3.1)
- Added OpenUPM scoped registries (com.cysharp, jp.hadashikick)

**UIFrameworkInstallerWizardSteps.cs:**
- Hardened Step1: try/catch around File.ReadAllText/WriteAllText
- Bounded InsertScopedRegistry IndexOf('[') to 40-char window (prevent mis-insertion)
- InsertScopedRegistry now returns null on malformed manifest (explicit failure signal)
- Expanded R3 Newtonsoft.Json dependency comment

## Lessons Learned

**Don't assume the code is guilty first.** Check state before debugging behavior. A poisoned manifest masquerading as a wizard bug wastes debugging cycles.

**Scoped registries matter.** Git URLs are poison for transitive dependency chains in OpenUPM projects.

## Next Steps

1. Test full wizard flow end-to-end with clean manifest
2. Document manifest structure expectations in CLAUDE.md or code comments
3. Add manifest validation check to wizard (detect stale git URLs, missing scopes)
