# Codebase Summary

## Project Overview
This is a Unity 6 UIFramework package project with a dependency management installer wizard that sets up required packages via OpenUPM (Open Unity Package Manager).

## Key Components

### Package Dependencies (Packages/manifest.json)
- **UniTask 2.5.11** (com.cysharp.unitask) - Async/await support via OpenUPM
- **R3 1.3.1** (com.cysharp.r3) - Reactive extensions library via OpenUPM
- **VContainer 1.18.0** (jp.hadashikick.vcontainer) - Dependency injection via OpenUPM
- **OpenUPM Registry** - Scoped registry for `com.cysharp` and `jp.hadashikick` packages

All three packages now resolve through a single OpenUPM scoped registry, with transitive dependencies handled automatically.

### UIFramework Installer Wizard
Located in: `Packages/com.sinkii09.uiframework/Editor/Installer/UIFrameworkInstallerWizardSteps.cs`

The installer is a 6-step setup wizard that runs within the Unity Editor:

1. **Step 1 - Install dependencies** - Adds OpenUPM registry and three core packages to manifest.json
2. **Step 2 - Add VCONTAINER_UNITASK_INTEGRATION define** - Enables VContainer-UniTask integration
3. **Step 3 - Validate DOTween Pro** - Confirms DOTween Pro from Asset Store is installed
4. **Step 4 - Create UIRoot prefab** - Generates the root UI canvas with standard configuration
5. **Step 5 - Create UIFrameworkConfig asset** - Generates ScriptableObject configuration
6. **Step 6 - Create folder structure** - Sets up project asset folders (_Project, Resources, etc.)

## Recent Changes (Bug Fix)

### Issue
The UIFramework installer's dependency installation (Step 1) had two critical issues:
- No error handling for file I/O operations (read/write failures silent or unhandled)
- Array boundary search for `scopedRegistries` block could mis-fire on malformed JSON

### Fix Applied
File: `Packages/com.sinkii09.uiframework/Editor/Installer/UIFrameworkInstallerWizardSteps.cs`

**Step 1_InstallDeps hardening:**
- Wrapped `File.ReadAllText()` in try/catch with proper error reporting
- Wrapped `File.WriteAllText()` in try/catch with proper error reporting
- Added bounded search window (40 chars) in `InsertScopedRegistry()` to prevent false matches

**Manifest.json structure update:**
- Replaced Git URL entries with OpenUPM version strings
- Added OpenUPM scoped registry configuration block
- All dependencies now resolve through `https://package.openupm.com`

**R3 implicit dependency note:**
- Added developer comment documenting R3 1.3.1's implicit runtime dependency on `com.unity.nuget.newtonsoft-json`
- Not declared in lock file; consumers may need manual addition if Newtonsoft types are missing

### Impact
- Installer Step 1 now handles file I/O errors gracefully with clear error messages
- Manifest.json parsing is more robust against malformed JSON
- All three packages (UniTask, R3, VContainer) resolve correctly from OpenUPM with automatic transitive dependency resolution
- Installation process is now reliable and repeatable

## Code Standards
- Error handling: all file I/O operations wrapped in try/catch with user-facing error logs
- String searching: bounded window searches to prevent false matches in JSON parsing
- Comments: critical behavior documented inline (e.g., R3's Newtonsoft.Json dependency)
