# Code Standards

## C# Coding Conventions

### Naming
- **Classes & Methods:** PascalCase (e.g., `UIFrameworkInstallerWizard`, `Step1_InstallDeps`)
- **Properties & Public Fields:** PascalCase (e.g., `OpenUpmDeps`)
- **Private Fields & Local Variables:** camelCase with underscore prefix for fields (e.g., `_content`, `_path`)
- **Constants:** UPPER_SNAKE_CASE (e.g., `OpenUpmUrl`, `OpenUpmScopes`)
- **Parameters:** camelCase (e.g., `content`, `path`)

### File Organization
- One class per file; filename matches class name exactly
- Use namespaces matching folder structure (e.g., `Sinkii09.UIFramework.Editor`)
- Regions are discouraged; use partial classes or separate files instead

### Error Handling

**All file I/O must be wrapped in try/catch:**
```csharp
string content;
try { content = File.ReadAllText(path); }
catch (Exception ex) { 
    Mark(0, Status.Failed); 
    Log($"Step 1 FAILED: Cannot read manifest.json — {ex.Message}"); 
    return false; 
}
```

**Always provide user-facing error messages with context:**
- Include what failed (filename, operation)
- Include why (exception message, constraint violated)
- Indicate next steps (manual action required, retry, etc.)

### String Operations

**Use bounded searches to prevent false matches in structured text:**
```csharp
const string key = "\"scopedRegistries\"";
int keyIdx = content.IndexOf(key, StringComparison.Ordinal);
if (keyIdx >= 0)
{
    // Bounded search (40 chars) so a '[' in an unrelated key never mis-fires
    int searchFrom = keyIdx + key.Length;
    int window = Math.Min(40, content.Length - searchFrom);
    int arrStart = window > 0 ? content.IndexOf('[', searchFrom, window) : -1;
    if (arrStart < 0) return null; // malformed: scopedRegistries exists but no array start
}
```

**Rationale:** JSON parsing via string operations is fragile; explicitly limit search scope to prevent false positives.

### Comments

**Comment the WHY, not the WHAT:**
```csharp
// GOOD: Explains a non-obvious constraint
// R3 1.3.1 has an implicit runtime dep on com.unity.nuget.newtonsoft-json (not declared in the lock file).
// If consumers see missing Newtonsoft types, add com.unity.nuget.newtonsoft-json to their manifest manually.

// BAD: Restates what the code does
// Check if content contains the key
if (!content.Contains(dep.Key))
```

**Document critical behavior at the point of decision:**
- Transitive dependencies that may be implicit
- Array search bounds and why they're needed
- Exception handling rationale
- Retry/recovery strategies

### Collections & Data

**Prefer readonly where possible:**
```csharp
internal static readonly Dictionary<string, string> OpenUpmDeps = new()
{
    ["com.cysharp.unitask"]       = "2.5.11",
    ["com.cysharp.r3"]            = "1.3.1",
    ["jp.hadashikick.vcontainer"] = "1.18.0"
};
```

**Use Array.ConvertAll for functional transformations:**
```csharp
var scopesJson = string.Join(",\n        ", Array.ConvertAll(OpenUpmScopes, s => $"\"{s}\""));
```

## JSON Structure Conventions (Packages/manifest.json)

### scopedRegistries Block
Place at the start of the manifest (before dependencies):
```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "jp.hadashikick"
      ]
    }
  ],
  "dependencies": { ... }
}
```

### Dependency Versions
Use semantic versioning from the package source:
- **UniTask:** `"com.cysharp.unitask": "2.5.11"`
- **R3:** `"com.cysharp.r3": "1.3.1"`
- **VContainer:** `"jp.hadashikick.vcontainer": "1.18.0"`

## Editor GUI Conventions (Installer Wizard)

### Step Labeling
Each wizard step has a label displayed in the UI. Format: `"#. Description (details)"`

Example:
```csharp
private static readonly string[] StepLabels =
{
    "1. Install dependencies (VContainer, R3, UniTask)",
    "2. Add VCONTAINER_UNITASK_INTEGRATION define",
    "3. Validate DOTween Pro",
    "4. Create UIRoot prefab",
    "5. Create UIFrameworkConfig asset",
    "6. Create _Project/ folder structure"
};
```

### Status Reporting
Use `Mark(stepIndex, Status)` and `Log(message)` for feedback:
- `Mark(0, Status.Done)` — step succeeded
- `Mark(0, Status.Failed)` — step failed; include reason in Log message
- Log messages use format: `"Step N: message"` or `"Step N FAILED: reason"`

## Testing & Validation

### Manual Testing Checklist
- [ ] Run wizard on fresh project
- [ ] Run wizard on project with partial dependencies already installed
- [ ] Run wizard on project with DOTween missing
- [ ] Run wizard on project with all steps already completed
- [ ] Verify manifest.json is valid JSON after each step
- [ ] Verify scripting defines are set correctly

### Edge Cases
- Empty scopedRegistries array → ensure insert position is correct
- Empty dependencies block → ensure trailing comma handling is correct
- Missing dependencies key entirely → return null and log error
- File permission denied during read/write → catch and report

## Documentation Standards

### Code Comments
- **Implicit dependencies:** Always document (e.g., R3 → Newtonsoft.Json)
- **Bounded search windows:** Explain the boundary and why it's needed
- **Error recovery:** Note what user should do (retry, manual fix, reinstall)

### Commit Messages
- Use conventional format: `fix: hardened Step1_InstallDeps error handling`
- Include what changed and why (not just what)
- Reference specific issues if applicable

### Example
```
fix: harden UIFramework installer dependency resolution

- Wrap file I/O in try/catch with user-facing error messages
- Bound scopedRegistries array search to 40 chars to prevent false matches
- Document R3's implicit Newtonsoft.Json transitive dependency
- Add inline check for malformed manifest.json

This ensures Step 1 fails gracefully with clear error messages instead of
silently corrupting the manifest or mis-parsing malformed JSON.
```
