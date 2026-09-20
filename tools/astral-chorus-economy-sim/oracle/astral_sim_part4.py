# -*- coding: utf-8 -*-
"""Part 4 -- answer the review's open questions with data.

(1) At the floor, what is the p10/p01 outcome for *3 and *4 specifically?
    The doc calls that row a "sàn bảo đảm"; a mean cannot support that word.
(2) How many SAME-TIER 5:1 repairs actually happen? The published table shows none.
    If they are rare, the pooled presentation is defensible; if not, it is wrong.
"""
import random, statistics
from astral_sim import ROSTER, MAX_COST, AFFINITY_FRAGS, simulate_player, pct

SAME, CROSS = 5, 10


def cascade_instrumented(frags, owned, same_rate=SAME, cross_rate=CROSS):
    """Same as the design cascade, but counts same-tier vs cross-tier conversions separately."""
    same_conv = cross_conv = 0
    inflow, maxed = 0, {}
    for tier in (3, 4, 5):
        size = ROSTER[tier]
        have = [frags.get((tier, i), 0) + (AFFINITY_FRAGS if i in owned[tier] else 0)
                for i in range(size)]
        free = inflow
        while free > 0:
            d = sorted((MAX_COST - have[i], i) for i in range(size) if have[i] < MAX_COST)
            if not d:
                break
            need, i = d[0]
            take = min(need, free)
            have[i] += take
            free -= take
        surplus = sum(max(0, h - MAX_COST) for h in have) + free
        while surplus >= same_rate:
            d = sorted((MAX_COST - have[i], i) for i in range(size) if have[i] < MAX_COST)
            if not d:
                break
            have[d[0][1]] += 1
            surplus -= same_rate
            same_conv += 1
        maxed[tier] = sum(1 for h in have if h >= MAX_COST)
        if maxed[tier] == size and tier < 5:
            up = surplus // cross_rate
            cross_conv += up
            surplus -= up * cross_rate
            inflow = up
        else:
            inflow = 0
    return maxed, same_conv, cross_conv


N = 30000
BUDGETS = [180, 420]
rng = random.Random(90210)
acc = {b: {"m3": [], "m4": [], "m5": [], "same": [], "cross": [], "cat": []} for b in BUDGETS}

for _ in range(N):
    snaps, _, _ = simulate_player(max(BUDGETS), rng, BUDGETS)
    for b in BUDGETS:
        frags, owned, _ = snaps[b]
        m, sc, cc = cascade_instrumented(frags, owned)
        r = acc[b]
        r["m3"].append(m[3]); r["m4"].append(m[4]); r["m5"].append(m[5])
        r["same"].append(sc); r["cross"].append(cc); r["cat"].append(sc + cc)

for b in BUDGETS:
    r = acc[b]
    print("=" * 74)
    print("BUDGET %d  (N=%d, same-tier 5:1, cross-tier 10:1)" % (b, N))
    print("  *3 maxed of 6: p01=%d p10=%d p50=%d   | %% with ALL 6 maxed: %.2f%%"
          % (pct(r["m3"], .01), pct(r["m3"], .10), pct(r["m3"], .50),
             100.0 * sum(1 for x in r["m3"] if x == 6) / N))
    print("  *4 maxed of 5: p01=%d p10=%d p50=%d   | %% with ALL 5 maxed: %.2f%%"
          % (pct(r["m4"], .01), pct(r["m4"], .10), pct(r["m4"], .50),
             100.0 * sum(1 for x in r["m4"] if x == 5) / N))
    print("  *5 maxed of 3: p01=%d p10=%d p50=%d   | %% with 0 maxed:     %.2f%%"
          % (pct(r["m5"], .01), pct(r["m5"], .10), pct(r["m5"], .50),
             100.0 * sum(1 for x in r["m5"] if x == 0) / N))
    print("  Catalyst: same-tier repairs mean=%.2f (p90=%d) | cross-tier mean=%.2f | total p50=%d p90=%d"
          % (statistics.mean(r["same"]), pct(r["same"], .90),
             statistics.mean(r["cross"]), pct(r["cat"], .50), pct(r["cat"], .90)))
    print("  runs with ZERO same-tier repair: %.1f%%"
          % (100.0 * sum(1 for x in r["same"] if x == 0) / N))
