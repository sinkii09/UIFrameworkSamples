# Post-Task Protocol — Mandatory After Every Task

Applies to: every completed task, improvement, bug fix, or refactor — including trivial ones.
**NO EXCEPTIONS. NO SHORTCUTS.**

---

## What "done" means

A task is NOT done until steps 1–3 below are complete.

---

## Step 1 — Update Memory (always)

Write or update memory files in `C:\Users\user\.claude\projects\e--Hoc-2025-1-1-2025-TheEnd\memory\`:

- **Save** any non-obvious finding, decision, pattern, or gotcha that a future session would benefit from knowing.
- **Update** existing memory if a prior entry is now outdated or corrected.
- **Skip** if the task produced zero new knowledge (e.g., pure text/copy change).

Common triggers:
- A bug whose root cause was surprising
- A framework quirk or Unity-specific behavior discovered
- A design decision made and why
- A pattern proven to work (or not work) in this codebase

---

## Step 2 — Update Codebase Summary (`docs/codebase-summary.md`)

After any change that affects the project's observable state:

- **New feature added** → add to relevant component section
- **Bug fixed** → update "Recent Changes" section with: what broke, root cause, fix
- **File/class added or removed** → update folder structure
- **Architecture changed** → update architecture description

Keep `docs/codebase-summary.md` as the single source of truth for "what is this project and what does it currently contain." A new session reading only this file should understand the full current state.

---

## Step 3 — Update Project Roadmap (`docs/development-roadmap.md`) — when applicable

Trigger only when:
- A milestone or phase changes status (e.g., In Progress → Complete)
- A new goal or phase is added
- Timeline or scope changes

Skip for routine bug fixes that don't affect milestone status.

---

## Scope by task size

| Task type | Memory | Codebase Summary | Roadmap |
|-----------|--------|-----------------|---------|
| New feature / system | ✓ if non-obvious | ✓ always | ✓ if milestone changed |
| Bug fix | ✓ if root cause surprising | ✓ update Recent Changes | ✗ |
| Trivial fix (typo, text) | ✗ | ✗ | ✗ |
| Refactor (no behavior change) | ✓ if pattern changes | ✓ if structure changed | ✗ |

---

## Execution

Do these updates **inline** (no subagent needed for small tasks). For larger session completions, the `docs-manager` agent can be delegated.

The updates happen BEFORE the git commit message is written — so the commit is the final step, not the doc updates.
