# AstralChorus economy simulator

Runs the gacha/Resonance/Dust model from `docs/astral-chorus-gdd.md` §9–§11 **without opening Unity**.

Every number published in the GDD comes from here. When a design number changes, re-run this — do not
re-derive it by hand.

```bash
# print the anchor tables, the distribution and the grind figure
dotnet run --project tools/astral-chorus-economy-sim/AstralChorus.Sim -c Release

# run the same test suite Unity runs, outside Unity
dotnet test tools/astral-chorus-economy-sim/AstralChorus.Tests -c Release
```

## Why three projects

| Project | Target | Why it exists |
|---|---|---|
| `AstralChorus.Logic` | `netstandard2.1`, C# 9 | Compiles the **Unity assembly's own sources** (`Assets/UIFramework/Features/AstralChorus/Scripts/Logic/`). The target framework is deliberately Unity's, not the SDK's latest: `record`, `init` and `System.HashCode` fail here in seconds instead of the next time somebody opens the Editor. |
| `AstralChorus.Sim` | `net10.0` | The console entry point. |
| `AstralChorus.Tests` | `net10.0` | Compiles and runs the **same** `Tests/Editor/*.cs` files Unity runs, with NUnit from NuGet instead of from `UnityEngine.TestRunner`. Without it the suite would only ever be built by an Editor, which is how a test suite ends up green in principle and unbuilt in practice. |

There is no `.sln`; each project is built by path. Nothing here ships in a player build — the Unity
assembly does, this scaffolding does not.

## `oracle/`

The frozen Python prototype the C# port was checked against. **Do not edit it to match the port.** It is
the reference: if the two disagree, that disagreement is the finding, and it gets adjudicated by the
structural invariants in `EconomyInvariantTests` — which depend on neither implementation.

It lives here rather than beside its plan because `.gitignore` carries `plans/**/*`, so an oracle left
in `plans/` would have been exactly as unrecoverable as one left in a temp directory.

```bash
python tools/astral-chorus-economy-sim/oracle/astral_sim_part3.py   # the two anchor tables
```

## Why the ceiling is 420 and not 380

Share of players who fail to max the whole roster, 100,000 players x 3 seeds:

| Shard budget | 340 | 360 | 380 | 400 | **420** |
|---|---|---|---|---|---|
| fail to max all 14 | 2.3% | 0.19% | 0.018% | 0.002% | **0.000%** |

380 leaves roughly one player in 5,500 short. A sweep run at 8,000 players reports 380 as "everyone"
a fair fraction of the time, because 0.018% of 8,000 is 1.4 expected failures — which is exactly how a
zero-failure query misleads when N is chosen for convenience rather than for the resolution needed.

## What the numbers are sensitive to

- **Cross-rarity conversion cost** decides how much payout The Ascent has room for. At a flat 5:1 the
  whole tower is worth 80 Shard; at 10:1 it is worth 240. `RejectedModel_FlatFiveToOneRateGutsTheAscent`
  guards that.
- **Dust per run** sets the grind. The Catalyst *price* barely moves it — halving the income costs 22.2 h,
  cutting the price by a quarter saves 1.9 h.
- **Mode-dropped Catalyst** destroys the shop: at 0.25/run the price's effect on the grind collapses 57×.
  This is why `IModeRewardSink` has no `GrantCatalyst`.
