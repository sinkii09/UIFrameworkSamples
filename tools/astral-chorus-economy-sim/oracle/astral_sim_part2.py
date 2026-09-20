"""Part 2 -- sensitivity on the Resonance conversion rate, Focus effect, and the
coupled Dust/Catalyst solve (including mode-dropped Catalyst, review finding W4)."""
import math, random, statistics
from astral_sim import (ROSTER, MAX_COST, AFFINITY_FRAGS, RESONANCE_IN,
                        simulate_player, pct)


def cascade_rates(frags, owned, same_rate=5, cross_rate=5):
    """Cascade with SEPARATE same-tier and cross-tier conversion rates.

    Same-tier conversion is the variance eraser (GDD sec.11 reason 2) -- keep it cheap.
    Cross-tier conversion is the *3 overflow drain (reason 3) -- this is the leak."""
    catalyst = 0
    inflow = 0
    maxed = {}
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


def sweep(cross_rates, budgets, n=8000, seed=777):
    rng = random.Random(seed)
    top = max(budgets)
    res = {(cr, b): {"all14": 0, "maxed": [], "cat": []} for cr in cross_rates for b in budgets}
    for _ in range(n):
        snaps, _, _ = simulate_player(top, rng, budgets)
        for b in budgets:
            frags, owned, _ = snaps[b]
            for cr in cross_rates:
                m, cat = cascade_rates(frags, owned, 5, cr)
                r = res[(cr, b)]
                tot = sum(m.values())
                r["maxed"].append(tot)
                r["cat"].append(cat)
                if tot == 14:
                    r["all14"] += 1
    return res, n


CROSS = [5, 8, 10, 15, 20]
BUDGETS = [180, 220, 260, 300, 340, 380, 420, 460, 500, 560, 620, 700]
res, N = sweep(CROSS, BUDGETS)

print("=== SENSITIVITY: cross-tier Resonance rate (same-tier fixed at 5:1) ===")
print("%% of players with ALL 14 maxed.  N=%d\n" % N)
hdr = "cross |" + "".join("%7d" % b for b in BUDGETS)
print(hdr)
print("-" * len(hdr))
for cr in CROSS:
    print("%4d:1 |" % cr + "".join("%6.0f%%" % (100.0 * res[(cr, b)]["all14"] / N) for b in BUDGETS))

print("\nAscent Shard budget needed (total campaign+Ascent) for 100%% of players to max all 14:")
for cr in CROSS:
    hit = next((b for b in BUDGETS if res[(cr, b)]["all14"] == N), None)
    if hit:
        print("  cross %2d:1 -> %4d total  =>  Ascent must pay %4d Shard   (Catalyst p90: %d)"
              % (cr, hit, hit - 180, pct(res[(cr, hit)]["cat"], .90)))
    else:
        print("  cross %2d:1 -> >%d total (off the sweep)" % (cr, max(BUDGETS)))

# ---- Focus ----
print("\n=== FOCUS (risk 9): does x3 flatten the pull? ===")
rng = random.Random(4242)
hits = chances = 0
for _ in range(4000):
    _, fh, fc = simulate_player(400, rng, [400], focus={5: 0})
    hits += fh
    chances += fc
print("  *5 tier = 3 chars, Focus x3 -> focused char wins %.1f%% of *5 pulls "
      "(analytic 3/5 = 60.0%%)" % (100.0 * hits / max(1, chances)))
print("  a 2-char banner tier would be 3/4 = 75.0%")

# ---- Coupled Dust / Catalyst solve (W4: mode-dropped Catalyst is an input) ----
LEVEL_DUST    = 215_000    # 14 chars, level 1->60
CAMPAIGN_DUST = 40_000
RUN_MIN       = 3


def runs_needed(catalyst_need, dust_per_run, catalyst_price, catalyst_per_run):
    """Minimal repeatable-node runs to both max every level AND buy every Catalyst."""
    best = None
    for k in range(0, 40001):
        dust = k * dust_per_run + CAMPAIGN_DUST
        short = max(0, catalyst_need - k * catalyst_per_run)
        if dust >= LEVEL_DUST + short * catalyst_price:
            best = k
            break
    return best


print("\n=== COUPLED SOLVE: Dust rate x Catalyst price x mode-dropped Catalyst ===")
print("Levels alone cost %sk Dust; campaign gifts %sk." % (LEVEL_DUST // 1000, CAMPAIGN_DUST // 1000))
for C_NEED in (56, 92, 137):
    print("\n-- Catalyst needed = %d --" % C_NEED)
    print("  dust/run |" + "".join("  P=%-6d" % p for p in (600, 900, 1200, 1500)))
    print("  " + "-" * 50)
    for c_per_run in (0.0, 0.1, 0.25):
        print("  mode drops %.2f Catalyst/run:" % c_per_run)
        for R in (300, 450, 600, 800):
            row = ""
            for P in (600, 900, 1200, 1500):
                k = runs_needed(C_NEED, R, P, c_per_run)
                row += "  %5.1fh  " % (k * RUN_MIN / 60.0)
            print("     R=%4d |%s" % (R, row))
