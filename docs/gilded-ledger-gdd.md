# GildedLedger — Game Design Document

**Status:** design approved, implementation blocked on the persistence redesign
**Platform:** WebGL · **Pacing:** turn-based by day · **Framework:** com.sinkii09.uiframework

---

## 1. Premise

You inherit an adventurer's supply shop, and the debt that came with it. Every 7 days the lender
collects, and the amount grows geometrically. There is no winning ending. The only question is
**how long you last**.

The player does exactly three things: **what to stock**, **what to price it at**, and **whether
profit goes into the shop or into the debt**.

## 2. The day loop

**MORNING — Restock.** The wholesale market regenerates from the day's seed. Price per item is
`round(BaseValue × wholesaleMult)` with `wholesaleMult` in `[0.55, 0.95]`; availability is 0–4 units,
rarer for expensive goods. Constrained by **cash** and **shelf space**.

**MIDDAY — Pricing.** Drag stock from the storeroom onto the shelf, then set **one fixed price** per
item. Anything not on the shelf cannot be bought — the shelf is the real bottleneck. Prices are
**locked for the day**.

**AFTERNOON — Open.** Customers arrive one at a time, look at the shelf, and pick the item that best
fits their need within their price range. They buy iff `listedPrice <= WTP` **and**
`listedPrice <= Budget` **and** it is in stock. A customer who leaves says why: *Too expensive* /
*Not what I need* / *Out of stock*.

> That line of dialogue is the **only feedback channel** the player has for learning prices. It must
> distinguish all three reasons. Collapsing them into one message removes the game's teaching signal.

**EVENING — Ledger.** Revenue, cost of goods sold, profit, remaining stock, and **tomorrow's rumour**.
If `day % 7 == 0`, the lender collects.

## 3. Why single-shot pricing still has depth

Because of **information asymmetry**. Sold out instantly? Possibly priced too low. Sat there all day?
Priced too high. That is a **learning loop** standing in for a haggling loop — far cheaper in content
(no negotiation dialogue, no bargaining AI) while still delivering the feeling of reading a customer
correctly.

The *Appraisal Ledger* upgrade later converts guesswork into an estimated range — which means the
game **sells information to the player**. That is its second commodity.

## 4. Items

`Bulk` is shelf slots consumed. Starting shelf: **12 slots**.

| Id | Name | Category | BaseValue | Bulk |
|---|---|---|---|---|
| `rusty_sword` | Rusty Sword | Weapon | 20 | 2 |
| `iron_sword` | Iron Sword | Weapon | 55 | 2 |
| `silver_blade` | Silver Blade | Weapon | 140 | 2 |
| `leather_vest` | Leather Vest | Armor | 30 | 3 |
| `chain_mail` | Chain Mail | Armor | 85 | 4 |
| `tower_shield` | Tower Shield | Armor | 160 | 5 |
| `minor_potion` | Minor Potion | Potion | 12 | 1 |
| `healing_draught` | Healing Draught | Potion | 35 | 1 |
| `elixir` | Elixir | Potion | 110 | 1 |
| `lucky_charm` | Lucky Charm | Trinket | 25 | 1 |
| `rune_stone` | Rune Stone | Trinket | 70 | 1 |
| `relic_shard` | Relic Shard | Trinket | 200 | 1 |

Deliberate: **armour is bulky** (3–5 slots) so it pays well but eats the shelf; **trinkets are
compact** (1 slot) so they turn over fast. The trade-off is **shelf space**, not merely margin.

## 5. Customers

| Id | Name | Wealth | Prefers | PrefBonus | Weight |
|---|---|---|---|---|---|
| `farmer` | Farmer | 0.55 | Potion | +0.15 | 30 |
| `hunter` | Hunter | 0.85 | Weapon | +0.25 | 25 |
| `knight` | Knight | 1.25 | Armor | +0.30 | 20 |
| `mage` | Mage | 1.15 | Trinket | +0.35 | 15 |
| `collector` | Collector | 1.80 | Trinket | +0.50 | 10 |

## 6. Economy

```
WTP    = round(BaseValue * Wealth * (1 + PrefBonus if category matches) * dailyTrend[category] * noise)
Budget = round(Wealth * (120 + day * 4))

dailyTrend[category] in [0.85, 1.20]   fixed for the whole day, Market stream
noise                in [0.92, 1.08]   per customer, CustomerRoll stream
```

**Money is always `int`.** Only WTP uses floating point, and it rounds immediately.

### The consequence that makes the game

A hunter buying a potion: `WTP ~= 0.85 * Base` — barely above the average wholesale multiplier of
0.75, so almost no margin. A knight buying armour: `WTP ~= 1.62 * Base` — a large one.

So the game is **matching stock to the customers who will actually walk in**, not buying low and
selling high in the abstract. `Budget` adds a second, independent gate: a farmer on day 1 carries
about 68 gold and **cannot** afford a Silver Blade no matter how far the price drops.

## 7. Debt, and why an endless game still ends

`installment(n) = round(100 * 1.5^(n-1))`

| Instalment | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Day | 7 | 14 | 21 | 28 | 35 | 42 | 49 |
| Owed | 100 | 150 | 225 | 338 | 506 | 759 | 1,139 |

Income grows **linearly** — capped by shelf slots and customers per day, both of which upgrade in
discrete steps. Debt grows **geometrically**. The two curves **always cross**. The player's job is to
push the crossing as far out as possible. Expected run: **5–8 instalments, 35–56 days**.

**Emergency liquidation.** On a collection evening, if short, the player may **once** dump all
remaining stock at **50% of cost**. Still short means game over. Cheap to build, expensive
emotionally: it turns the moment of death into a final decision, and it punishes exactly the mistake
that caused it — holding too much stock.

## 8. Upgrades — the heart of an endless run

All of them spend **the same money that pays the debt**.

| Upgrade | Cost | Effect |
|---|---|---|
| Extra shelving | `120 * 1.6^n` | +6 shelf slots |
| Shop sign | `200 * 1.8^n` | +1 customer/day (cap 10) |
| Wholesale licence | `250 * 2^n` | Unlocks higher-tier goods |
| Appraisal ledger | `300` once | Shows estimated WTP range for items sold 3+ times |

Every instalment is a gamble: **invest now to earn more later, or pay safely and fall permanently
behind**. Without upgrades the income curve is flat and you die at instalment 4. Over-invest and you
die this instalment.

## 9. Information

Each evening, a rumour for tomorrow: **one category will rise or fall**. Correct about direction,
silent about magnitude. Deliberately no false-rumour mechanic in Phase 1 — the uncertainty already
lives in `noise` and in not knowing which customers will arrive.

## 10. Customers per day and scoring

`customers(day) = min(10, 4 + floor(day / 7) + signBoardLevel)`

`Score = days survived`, tie-broken by **peak net worth** (cash + cost basis of stock, high-water
mark). The best score must outlive both the run and a page reload — which is exactly why the
persistence work comes first.

## 11. Screens

| Screen | Contents |
|---|---|
| **ShopHud** (HUD) | Cash · Day · Days until collection · Next amount due |
| **Restock** (Screen) | Wholesale list: icon, name, price, stock, buy. Tooltip shows BaseValue and past purchase prices. Shelf meter |
| **PriceBoard** (Screen) | Storeroom: icon, name, cost basis, price entry field, shelf toggle. With the appraisal ledger, an estimated WTP range |
| **Counter** (Screen) | Playback: customers arrive in turn, buy or leave with a reason. Toast per sale. Skip button |
| **Ledger** (Screen) | Revenue, COGS, profit, transactions, remaining stock, tomorrow's rumour |
| **DebtDue** (Popup) | Owed / on hand / Pay / Liquidate |
| **Upgrade** (Popup) | The four upgrades at current prices |
| **GameOver** (Popup) | Days survived, peak net worth, best score, replay |

## 12. Phase 2 — the trade route (designed, not built)

Replace the single fixed market with **choosing which market to visit** — each with its own price
table, costing 0–2 days of travel. A travel day is a day the shop is shut: no revenue, **but the debt
clock keeps running**.

Nothing in Phase 1 is disturbed: `MarketPriceTable` is already per-market. Phase 2 only adds a source
selection layer above it.

## 13. Out of scope for Phase 1

Audio · localisation · multi-round haggling · spoilage or depreciation · partial debt payment ·
reputation · multiple markets · collection sets · authenticating fakes.

## 14. Assumptions

1. **Prices are set before customers are seen.** This is what makes whole-day resolution valid;
   changing it changes the engine architecture.
2. Unsold stock carries over unchanged.
3. Debt is paid in full or the run ends; emergency liquidation is the only valve.
4. **Every number here is a starting point for tuning**, which is why they all live in
   ScriptableObjects rather than in code.
