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

## Step 2 — Update the docs (two files, split 2026-09-20)

`docs/codebase-summary.md` is **the current state**; `docs/changelog-summary.md` is **the history**.
They were one file until it reached 1,867 lines against the 800-line cap with 78% of it being log,
which buried the part a new session actually needs.

After any change that affects the project's observable state:

| Change | Goes in |
|---|---|
| New feature added | **summary** — the relevant component section |
| File/class added or removed | **summary** — the folder structure |
| Architecture changed | **summary** — the architecture description |
| Bug fixed | **changelog** — what broke, root cause, fix |
| Anything worth a dated entry | **changelog**, newest first |

A new session reading only `codebase-summary.md` should understand the full current state **without**
reading the changelog. If an entry only makes sense as history, it belongs in the changelog; if it
changes what the project *is*, the summary has to say so in its own words rather than link to a log
entry. Keep the summary under the 800-line cap — when it drifts over, split again rather than trim.

---

## Step 3 — Update Project Roadmap (`docs/development-roadmap.md`) — when applicable

Trigger only when:
- A milestone or phase changes status (e.g., In Progress → Complete)
- A new goal or phase is added
- Timeline or scope changes

Skip for routine bug fixes that don't affect milestone status.

---

## Scope by task size

| Task type | Memory | Codebase Summary | Changelog | Roadmap |
|-----------|--------|-----------------|-----------|---------|
| New feature / system | ✓ if non-obvious | ✓ always | ✓ always | ✓ if milestone changed |
| Bug fix | ✓ if root cause surprising | ✗ unless it changed the shape | ✓ always | ✗ |
| Trivial fix (typo, text) | ✗ | ✗ | ✗ | ✗ |
| Refactor (no behavior change) | ✓ if pattern changes | ✓ if structure changed | ✓ always | ✗ |

---

## Execution

Do these updates **inline** (no subagent needed for small tasks). For larger session completions, the `docs-manager` agent can be delegated.

The updates happen BEFORE the git commit message is written — so the commit is the final step, not the doc updates.
