# Power Wall: Required Power per Minute

> Status: implemented 2026-09-26 · Code: `Formulas.RequiredPower` / `PowerDeficit` / `PowerLastsMinutes`,
> `EnemyManager.PowerDeficit` / `EnemyDamageMultiplier`, `MapDef.PowerScale`
> Related: `threat.md`, `economy.md` §3.3 (superseded), `maps.md`, `main-menu.md`

## 1. Overview

Every minute of a run has a **Required Power** (Gereken Güç). The player's power rating P (Güç Katsayısı,
`economy.md`) is compared against it:
- While P is at or above the requirement, the run plays at its normal difficulty.
- Once P falls short, enemies get tougher and hit harder, in proportion to the gap. This is the wall.

Upgrades raise P, so they push the wall to a later minute. Nothing else moves it.

## 2. Player Fantasy

"I know exactly why I died: my power ran out at 4:10. Two more Forge levels and I'll get past minute 5."

Owner, 2026-09-26: *"belli bir dakika sonra o çarpana gelmezse oyun çok zorlaşsın, ama o çarpandaysa normal
oynasın… bunu oyun çarpanına indexlesek"* ("if the player hasn't reached that multiplier after a certain minute,
the game should get very hard; if they have, it should play normally… let's index it to the power multiplier").

Target (owner): **with no upgrades, the first run lasts 3–4 minutes.**

## 3. Detailed Rules

### 3.1 Required Power and the deficit

- **Required Power** follows run minutes t, not the threat clock:
  - R(t) = s × (1 + 0.08x + 0.01x²), where x = max(0, t − 2.5).
  - s is the map's `PowerScale`.
- **Deficit:** g = clamp(R / P, 1, 3).
  - g = 1 means the player has enough power.
- **When g > 1**, the wall applies:
  - Newly spawned enemies get HP × g^1.7.
  - Every enemy hit created from then on is × g. This covers bullets, lasers and body contact.
- **Hits already created are fixed.** The multiplier is read when an enemy spawns or fires, so enemies and bullets
  already on screen keep their values.

### 3.2 What was removed

- The √P enemy-HP scaling (`EnemyHpForPower`) is gone. It raised enemies alongside the player and ate part of
  every upgrade. An upgrade is now felt in full until the wall.
- The threat curves (`threat.md`) are unchanged: HP, damage, the horde and leaks. They are the normal difficulty.
  The wall stacks on top of them.

### 3.3 What the player sees

| Where | Text |
|---|---|
| **Menu, PLAY subline** | "Şekerkamışı · Güç yeter: 2:30" ("Candy Cane · Power lasts: 2:30"): the minute R(t) passes P on the shown map (`PowerLastsMinutes`). If P is below the map's starting requirement: "Güç yetmiyor!" ("Not enough power!"). |
| **HUD, in a run** | While g > 1.03: a pulsing coral line under the boss bar, "GÜÇ YETERSİZ! Düşmanlar ×1.5" ("UNDERPOWERED! Enemies ×1.5"). The number is the HP multiplier g^1.7. |
| **Run end** | If the run ended past the wall: "Güç ×1.00 · gereken ×1.26 → AR-GE'de güçlen!" ("Power ×1.00 · needed ×1.26 → power up in R&D!"). |

### 3.4 Maps

Each map's `PowerScale` multiplies the requirement from minute 0:

| Map | PowerScale |
|---|---|
| Şekerkamışı (Candy Cane) | 1 |
| Jöle Nebulası (Jelly Nebula) | 1.5 |
| Kurabiye Kuşağı (Cookie Belt) | 2.5 |

The maps' own HP, damage and spawn multipliers still apply on top (`maps.md`).

## 4. Formulas

| Variable | Meaning | Range |
|---|---|---|
| t | run minutes (not the threat clock) | 0–60 |
| s | map PowerScale | 1–2.5 |
| P | Güç Katsayısı (`Formulas.PowerRating`) | 1.0 fresh; about 1.1 after the first run; 3+ late |
| R | required power | ≥ s |
| g | deficit | 1–3 |

Examples on map 1:

| Minute | R | HP × at P = 1 | Damage × at P = 1 |
|---|---|---|---|
| 2:30 | 1.00 | 1 | 1 |
| 4 | 1.14 | 1.25 | 1.14 |
| 5 | 1.26 | 1.49 | 1.26 |
| 6 | 1.40 | 1.78 | 1.40 |
| 10 | 2.16 | 3.70 | 2.16 |

How long P holds on map 1, W(P) = 2.5 + x, where 0.01x² + 0.08x + 1 = P:

| P | 1.0 | 1.2 | 1.5 | 2.0 | 3.56 |
|---|---|---|---|---|---|
| W(P) | 2:30 | 4:30 | 6:37 | 9:16 | 15:00 |

**Map unlocks.** Unlocking map 2 needs 15:00 on map 1, which takes roughly P ≈ 3.5. On map 2 that P lasts to
about minute 10.9.

## 5. Edge Cases

- **Resumed run:** P is re-read from the meta at start and R from the restored run clock, so the wall picks up
  where it was.
- **Upgrades bought mid-run:** they are not possible. P is fixed for the run.
- **Very high P:** g stays at 1 until minute W(P). The run is then just the normal threat curve, and the horde is
  the "I'm strong" display the owner asked for.
- **Hopeless deficit:** g is capped at 3, so HP is at most ×6.5 and damage ×3. The 35 % per-hit cap
  (`threat.md` §3.7) still prevents one-shots.
- **Leaks:** leak damage is a share of max HP and is not scaled by the wall.

## 6. Dependencies

- `Formulas`
- `EnemyManager` (spawn HP; damage for contact hits)
- `BulletSystem` (enemy shots)
- `Enemy` (laser)
- `RunController` (sets P)
- `Maps`
- `GameUI` (the menu subline, the HUD warning and the run-end line)
- `QaAutopilot` (logs power and requirement at each death)

## 7. Tuning Knobs

| Knob | Where | Safe range | Effect |
|---|---|---|---|
| Grace minutes (2.5) | `RequiredPowerGraceMinutes` | 1.5–4 | When a fresh player first feels the wall |
| Linear / quadratic (0.08 / 0.01) | `RequiredPowerLinear` / `RequiredPowerQuadratic` | 0.04–0.15 / 0.005–0.02 | How much P each extra minute costs |
| HP exponent (1.7) | `DeficitHpExponent` | 1.2–2.5 | How brutal the wall is |
| Deficit cap (3) | `MaxPowerDeficit` | 2–4 | Upper bound of the wall |
| Map PowerScale | `Maps.All` | 1–3 | Entry power per map |

## 8. Acceptance Criteria

- The `FormulasTests` tests `test_required_power_*`, `test_power_deficit_*` and `test_power_lasts_*` pass.
- `MapsTests` shows PowerScale rising map to map.
- **QA autopilot** (fresh save, P = 1): every `[QA] Died at` line after 2:30 shows required > power.
- **Owner playtest:**
  - With no upgrades, a human reaches 3–4 minutes.
  - After upgrading, the menu line shows a later minute, and the run actually lasts about that long.
- QA screenshots show the menu subline and the HUD warning with no overlap.
