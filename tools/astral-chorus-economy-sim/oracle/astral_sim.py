"""AstralChorus economy Monte-Carlo.

Resolves the four blanks left open in the GDD:
  ASCENT_SHARD_PER_FLOOR, ASCENT_FLOORS, Catalyst price, Dust income rate.

Deliberately models PER-CHARACTER fragments. The GDD cascade tables work on tier
TOTALS, which silently assumes fragments inside a tier are freely movable. They are
not -- moving one costs 5:1 + a Catalyst. That gap is the main thing this sim tests.
"""
import random, statistics
from collections import defaultdict

# ---- design constants (GDD sec.4, 9, 11) ----
ROSTER          = {3: 6, 4: 5, 5: 3}
P5, P4          = 0.02, 0.18
SOFT_PITY       = 10      # 10th pull with no 4+ -> guaranteed *4 (exactly *4)
HARD_PITY       = 40      # 40th pull with no *5 -> guaranteed *5
DUP_FRAGMENTS   = 2
AFFINITY_FRAGS  = 2       # per owned character, final milestone only
MAX_COST        = 10      # 1+2+3+4 skill nodes
RESONANCE_IN    = 5       # 5 same-tier fragments + 1 Catalyst -> 1 targeted fragment
FOCUS_WEIGHT    = 3

CAMPAIGN_SHARD  = 180


def simulate_player(max_pulls, rng, checkpoints, focus=None):
    """One player pulling max_pulls times; snapshots fragment state at checkpoints."""
    since4 = since5 = 0
    owned = {t: set() for t in (3, 4, 5)}
    frags = defaultdict(int)
    hits = {3: 0, 4: 0, 5: 0}
    focus_hits = focus_chances = 0
    snaps = {}
    cp = set(checkpoints)

    for n in range(1, max_pulls + 1):
        since4 += 1
        since5 += 1
        if since5 >= HARD_PITY:
            tier = 5
        elif since4 >= SOFT_PITY:
            tier = 4                      # exactly *4, never *5 -- keeps the *4 floor above 0
        else:
            r = rng.random()
            tier = 5 if r < P5 else (4 if r < P5 + P4 else 3)
        hits[tier] += 1
        if tier >= 4:
            since4 = 0
        if tier == 5:
            since5 = 0

        size = ROSTER[tier]
        unowned = [i for i in range(size) if i not in owned[tier]]
        pool = unowned if unowned else list(range(size))
        f = focus.get(tier) if focus else None
        if f is not None and f in pool and len(pool) > 1:
            w = [FOCUS_WEIGHT if i == f else 1 for i in pool]
            pick = rng.choices(pool, w)[0]
            focus_chances += 1
            if pick == f:
                focus_hits += 1
        else:
            pick = rng.choice(pool)

        if pick in owned[tier]:
            frags[(tier, pick)] += DUP_FRAGMENTS
        else:
            owned[tier].add(pick)

        if n in cp:
            snaps[n] = (dict(frags), {t: set(s) for t, s in owned.items()}, dict(hits))
    return snaps, focus_hits, focus_chances


def cascade(frags, owned):
    """Bottom-up Resonance cascade. Returns (maxed_per_tier, catalyst, leftover_per_tier)."""
    catalyst = 0
    inflow = 0                      # targeted fragments arriving from the tier below (1:1)
    maxed, leftover = {}, {}

    for tier in (3, 4, 5):
        size = ROSTER[tier]
        have = [frags.get((tier, i), 0) + (AFFINITY_FRAGS if i in owned[tier] else 0)
                for i in range(size)]

        # 1. targeted inflow fills deficits 1:1, smallest deficit first (maximises count maxed)
        free = inflow
        while free > 0:
            deficits = sorted((MAX_COST - have[i], i) for i in range(size) if have[i] < MAX_COST)
            if not deficits:
                break
            d, i = deficits[0]
            take = min(d, free)
            have[i] += take
            free -= take

        # 2. same-tier surplus repairs remaining deficits at 5:1 + 1 Catalyst each
        surplus = sum(max(0, h - MAX_COST) for h in have) + free
        while surplus >= RESONANCE_IN:
            deficits = sorted((MAX_COST - have[i], i) for i in range(size) if have[i] < MAX_COST)
            if not deficits:
                break
            _, i = deficits[0]
            have[i] += 1
            surplus -= RESONANCE_IN
            catalyst += 1

        maxed[tier] = sum(1 for h in have if h >= MAX_COST)
        leftover[tier] = surplus

        # 3. only push upward once this tier is fully maxed
        if maxed[tier] == size and tier < 5:
            up = surplus // RESONANCE_IN
            catalyst += up
            surplus -= up * RESONANCE_IN
            leftover[tier] = surplus
            inflow = up
        else:
            inflow = 0
    return maxed, catalyst, leftover


def run(budgets, n_players=20000, seed=20260920, focus=None):
    rng = random.Random(seed)
    top = max(budgets)
    acc = {b: {"maxed": [], "cat": [], "own14": 0, "h5": [], "h4": [], "h3": [],
               "m3": [], "m4": [], "m5": []} for b in budgets}
    fh = fc = 0
    for _ in range(n_players):
        snaps, a, b = simulate_player(top, rng, budgets, focus)
        fh += a
        fc += b
        for bud in budgets:
            frags, owned, hits = snaps[bud]
            m, cat, _ = cascade(frags, owned)
            r = acc[bud]
            r["maxed"].append(sum(m.values()))
            r["m3"].append(m[3]); r["m4"].append(m[4]); r["m5"].append(m[5])
            r["cat"].append(cat)
            r["own14"] += 1 if sum(len(s) for s in owned.values()) == 14 else 0
            r["h5"].append(hits[5]); r["h4"].append(hits[4]); r["h3"].append(hits[3])
    return acc, (fh, fc)


def pct(xs, q):
    xs = sorted(xs)
    return xs[min(len(xs) - 1, int(q * len(xs)))]


if __name__ == "__main__":
    BUDGETS = [180, 220, 260, 300, 340, 360, 380, 400, 440, 480, 520]
    N = 20000
    acc, _ = run(BUDGETS, N)

    print("AstralChorus economy sim -- %d players/budget, per-character fragments\n" % N)
    print("budget | own14 |  *5 hits |  *4 hits | maxed chars (of 14)           | Catalyst")
    print("       |       |   mean   |   mean   |  p10   p50   p90   all-14     | p50   p90")
    print("-" * 88)
    for b in BUDGETS:
        r = acc[b]
        allmax = 100.0 * sum(1 for m in r["maxed"] if m == 14) / N
        print("%6d | %5.1f%% | %8.2f | %8.2f | %4d  %4d  %4d   %6.1f%%     | %4d  %4d" % (
            b, 100.0 * r["own14"] / N, statistics.mean(r["h5"]), statistics.mean(r["h4"]),
            pct(r["maxed"], .10), pct(r["maxed"], .50), pct(r["maxed"], .90), allmax,
            pct(r["cat"], .50), pct(r["cat"], .90)))

    print("\nper-tier maxed (p50) -- where the wall actually is")
    print("budget |  *3 (of 6)  *4 (of 5)  *5 (of 3)")
    print("-" * 46)
    for b in BUDGETS:
        r = acc[b]
        print("%6d |     %d          %d          %d" % (
            b, pct(r["m3"], .50), pct(r["m4"], .50), pct(r["m5"], .50)))
