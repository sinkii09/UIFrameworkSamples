"""Part 3 -- produce the exact GDD anchor tables under same-tier 5:1 / cross-tier 10:1.

Prints both the MEAN decomposition (the shape the GDD tables use) and the real
per-character Monte-Carlo outcome, so the published table can be checked against it.
"""
import random, statistics
from astral_sim import ROSTER, MAX_COST, AFFINITY_FRAGS, simulate_player, pct

def cascade_rates(frags, owned, same_rate=5, cross_rate=5):
    """Same-tier rate erases variance (cheap); cross-tier rate drains overflow (dear)."""
    catalyst, inflow, maxed = 0, 0, {}
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
            catalyst += 1
        maxed[tier] = sum(1 for h in have if h >= MAX_COST)
        if maxed[tier] == size and tier < 5:
            up = surplus // cross_rate
            catalyst += up
            surplus -= up * cross_rate
            inflow = up
        else:
            inflow = 0
    return maxed, catalyst


SAME, CROSS = 5, 10
FLOOR, CEILING = 180, 420
N = 30000


def mean_hits(budgets, n, seed=31337):
    rng = random.Random(seed)
    top = max(budgets)
    acc = {b: {3: [], 4: [], 5: []} for b in budgets}
    real = {b: {"all14": 0, "maxed": [], "cat": [], "m5": []} for b in budgets}
    for _ in range(n):
        snaps, _, _ = simulate_player(top, rng, budgets)
        for b in budgets:
            frags, owned, hits = snaps[b]
            for t in (3, 4, 5):
                acc[b][t].append(hits[t])
            m, cat = cascade_rates(frags, owned, SAME, CROSS)
            r = real[b]
            r["maxed"].append(sum(m.values()))
            r["m5"].append(m[5])
            r["cat"].append(cat)
            if sum(m.values()) == 14:
                r["all14"] += 1
    return acc, real


def table(h3, h4, h5, cross):
    """Mean decomposition, exactly the columns the GDD table uses."""
    rows, catalyst, inflow = [], 0, 0
    hits = {3: h3, 4: h4, 5: h5}
    aff = {3: 12, 4: 10, 5: 6}
    need = {3: 60, 4: 50, 5: 30}
    for t in (3, 4, 5):
        dup = (hits[t] - ROSTER[t]) * 2
        total = dup + aff[t] + inflow
        surplus = total - need[t]
        up = int(surplus // cross) if (surplus > 0 and t < 5) else 0
        catalyst += up
        left = surplus - up * cross
        rows.append((t, dup, aff[t], inflow, total, need[t], up, left))
        inflow = up
    return rows, catalyst


acc, real = mean_hits([FLOOR, CEILING], N)

print("AstralChorus anchor tables -- same-tier 5:1, cross-tier %d:1, N=%d\n" % (CROSS, N))
for b in (FLOOR, CEILING):
    h3 = statistics.mean(acc[b][3])
    h4 = statistics.mean(acc[b][4])
    h5 = statistics.mean(acc[b][5])
    rows, cat = table(h3, h4, h5, CROSS)
    print("=" * 78)
    print("BUDGET %d pulls    hits: %.2f *5 | %.2f *4 | %.2f *3" % (b, h5, h4, h3))
    print("-" * 78)
    print("tier |  dup x2 | affinity | inflow |  total | need | convert up | leftover")
    for (t, dup, a, inf, tot, nd, up, left) in rows:
        print(" *%d  | %7.1f | %8d | %6d | %6.1f | %4d | %10d | %7.1f"
              % (t, dup, a, inf, tot, nd, up, left))
    print("Catalyst spent (mean decomposition): %d" % cat)
    r = real[b]
    print("-- real per-character Monte-Carlo --")
    print("   all 14 maxed: %.1f%% | maxed p10/p50/p90: %d/%d/%d | *5 maxed p50: %d | Catalyst p50/p90: %d/%d"
          % (100.0 * r["all14"] / N, pct(r["maxed"], .10), pct(r["maxed"], .50),
             pct(r["maxed"], .90), pct(r["m5"], .50), pct(r["cat"], .50), pct(r["cat"], .90)))
    print()

# Dust solve at the ceiling Catalyst figure
LEVEL_DUST, CAMPAIGN_DUST, RUN_MIN = 215_000, 40_000, 3
C = pct(real[CEILING]["cat"], .90)
print("=== Dust solve at ceiling (Catalyst p90 = %d, NO mode-dropped Catalyst) ===" % C)
for R in (300, 450, 600, 750):
    for P in (900, 1200, 1500):
        k = 0
        while k * R + CAMPAIGN_DUST < LEVEL_DUST + C * P:
            k += 1
        print("  R=%4d Dust/run, P=%4d Dust/Catalyst -> %5d runs = %5.1f h  (total %sk Dust)"
              % (R, P, k, k * RUN_MIN / 60.0, (LEVEL_DUST + C * P) // 1000))
