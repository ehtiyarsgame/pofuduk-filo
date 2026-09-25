# Economy: Power Coefficient and Coin Tiers

> Status: implemented 2026-09-25 · Related: `meta-economy.md`, `ad-rewards.md`, `threat.md`

## 1. Overview

Device feedback on 2026-09-25 asked for two things:

- A power coefficient (*güç katsayımız*) that grows as the player develops, with gold arriving in proportion to it.
- Coins of different sizes, not only small ones. In the player's words: *"her zaman aynı oranda para ile
  gelişemezler"* ("they can't always develop at the same rate of money").

Ball Blast works the same way. As the cannon grows, so does the value of what drops. Upgrade costs grow
geometrically, so without this, income would fall further and further behind them.

## 2. Player Fantasy

"I raised my Forge, and now gold stacks and bars rain down instead of single coins."

## 3. Detailed Rules

### 3.1 Güç Katsayısı (power coefficient)

- **What it is built from:** Forge power and speed levels, total workshop levels, total weapon-mastery levels,
  and the selected pilot's level (`MetaProgressionService.PowerRating`).
- **Where it shows:** on the main menu as **GÜÇ ×N.NN** ("POWER ×N.NN"), next to an Ar-Ge button. Tapping it
  opens Ar-Ge.
- **When it applies:** it is set at run start as `PickupSystem.CoinPowerMultiplier`.
- **What it affects:** every coin's value.

### 3.2 Coin tiers

Each coin's value is computed first (§4). Its look follows from that value (`PickupRules.GoldKindFor`):

| Value | Pickup | Sprite |
|---|---|---|
| 1–14 | coin | `p_coin` |
| 15–59 | coin stack | `p_coin_big` |
| 60+ | gold bar | `p_gold_bar` |

All three are collected the same way, and Gold Gain (`GoldGain`) applies on pickup as before.

### 3.3 Enemies keep up, more slowly (2026-09-25)

**Power Match's adaptive HP is switched off** (`EnemyManager.AdaptiveHp = false`). It used to raise enemy HP
whenever the player killed fast. That hid the effect of upgrades: "geliştirdim, fark etmedi" ("I upgraded and
noticed no difference"). In QA run 45 it tripled enemy HP.

Enemy HP now scales with **√P** instead (`Formulas.EnemyHpForPower`):

- At P = 2, enemies have ×1.41 HP, while Forge damage alone is ×1.5–2. Upgrades always come out stronger.
- Coins scale with P, so they grow faster than enemy HP.
- The threat curve in `threat.md` still decides how long a run lasts.

### 3.4 Slower meta (2026-09-25, device feedback)

Device feedback: *"ilk geliştirmelerden sonra 26. seviyeye geldim… para bu kadar kolay kazanılmamalı… oyun dışı
güncellemelerde yüzdeler çok yüksek, saçma ve ucuz"* ("after the first upgrades I reached level 26… money
shouldn't come this easily… the out-of-game upgrade percentages are far too high, absurd and cheap").

**What was wrong.** A modelled 4-minute run paid about 785 gold, and an ad-doubled one about 1 570. At
40·1.14^L that bought about Forge 10/9, which is ×1.84 damage from the Forge alone, with the workshop on top.
Income also snowballed: coin value = base × **P** × (1 + 0.08t), so every upgrade made the next one arrive
faster.

**Change.**

| What | Before | After |
|---|---|---|
| Forge Power per level | +5 % | +2 % |
| Forge Speed per level | +2.5 % (cap ×2) | +1 % (cap ×1.4) |
| Forge cost | 40·1.14^L | 120·1.16^L |
| Workshop Health / Damage / Fire rate / Gold per level | 8 / 5 / 3 / 10 % | 4 / 2 / 1.5 / 5 % |
| Workshop base costs | 80–1 500 | 180–3 000 (about ×2) |
| Luck / Experience per level | 5 / 4 % | 3 / 3 % |
| Weapon mastery | +8 %/level, 150·1.55^L | +4 %/level, 300·1.6^L |
| Pilot level cost | 300·1.6^(L−1) | 600·1.6^(L−1) |
| Coin value | base × P × (1 + 0.08t) | base × **√P** × (1 + 0.04t) |
| Endless gold multiplier | ×1.5 | ×1.25 |
| P coefficients (Forge P / Forge S / workshop) | 0.04 / 0.03 / 0.02 | 0.02 / 0.01 / 0.01, to track what the levels really add |

**Target.**
- A 4-minute first run pays about 480 gold, or about 970 with the double-gold ad.
- That buys about Forge 3/2: +8 % damage, not +84 %.
- The next run should reach a few tens of seconds further, not minutes.
- Existing saves keep their levels. Each level is simply worth less now.

## 4. Formulas

- `PowerRating = 1 + 0.04·ForgePower + 0.03·ForgeSpeed + 0.02·ΣWorkshop + 0.02·ΣMastery + 0.03·(PilotLevel − 1)`.
  Examples:
  - A fresh save is ×1.00.
  - Forge 10/10 plus 10 workshop levels is ×1.90.
- `CoinValue = base × max(1, PowerRating) × (1 + 0.08·t)`, where t is run minutes.
  - `base` is the enemy's gold value × 5. Formation clears use 15 instead.
  - Example: a Jelly Bear gives 10 at the start. At ×2 power and 5 minutes into the run, it gives
    10 × 2 × 1.4 = 28, which drops as a coin stack.

## 5. Edge Cases

- **Coin value:** it is always at least 1.
- **Power rating:** it is never applied below ×1.
- **Resumed run:** the multiplier is recomputed at run start, including on resume.

## 6. Dependencies

- `Formulas`
- `MetaProgressionService`
- `PickupSystem`
- `RunController`
- `GameUI.Rewards` (menu readout)
- `ContentBuilder` (pickup visuals 7–8)

## 7. Tuning Knobs

| Knob | Where | Safe range |
|---|---|---|
| Power weights (0.04 / 0.03 / 0.02 / 0.02 / 0.03) | `Formulas.PowerRating` | 0.01–0.08 |
| Run-time coin growth (0.08/min) | `Formulas.CoinValue` | 0.03–0.15 |
| Tier thresholds (15 / 60) | `PickupRules.GoldKindFor` | — |

## 8. Acceptance Criteria

The following tests must pass:

- `test_power_rating_starts_at_one_and_grows_with_upgrades`
- `test_coin_value_scales_with_power_and_run_time`
- `test_gold_kind_by_value`
