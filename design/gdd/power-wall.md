# Power Wall: Required Power per Minute

> Status: implemented 2026-09-26 · Code: `Formulas.RequiredPower` / `PowerDeficit` / `PowerLastsMinutes`,
> `EnemyManager.PowerDeficit` / `EnemyDamageMultiplier`, `MapDef.PowerScale`
> Revised 2026-09-26: the wall is **hidden** from the player and softer (§3.3).
> Related: `threat.md`, `economy.md` §3.3 (superseded), `maps.md`

## 1. Overview

Every minute of a run has a **Required Power** (Gereken Güç). The player's power rating P (Güç Katsayısı,
`economy.md`) is compared against it:
- While P is at or above the requirement, the run plays at its normal difficulty.
- Once P falls short, enemies get tougher and hit harder, in proportion to the gap. This is the wall.

Upgrades raise P, so they push the wall to a later minute. A very skilled player can still push through it:
it is steep, not a hard stop.

## 2. Player Fantasy

"It's getting really tough out here… after that upgrade I definitely got further." The player feels the wall; they are never shown it.

Owner, 2026-09-26: *"belli bir dakika sonra o çarpana gelmezse oyun çok zorlaşsın, ama o çarpandaysa normal
oynasın… bunu oyun çarpanına indexlesek"* ("if the player hasn't reached that multiplier after a certain minute,
the game should get very hard; if they have, it should play normally… let's index it to the power multiplier").

Targets (owner):
- **With no upgrades, the first run lasts 3–4 minutes.**
- **Hidden and gradual** (same day): *"kullanıcı bilmesin, hemen zorlaşmasın; becerisi gerçekten çok çok iyiyse
  zorlansa da geçsin; 'bu kadar da olsaydı' gibi şeyler olmasın"* ("the player shouldn't know about it, it shouldn't
  get hard all at once; if their skill is really, really good they should get through even if it's hard; no 'if
  only you'd had this much' messages").

## 3. Detailed Rules

### 3.1 Required Power and the deficit

- **Required Power** follows run minutes t, not the threat clock:
  - R(t) = s × (1 + 0.08x + 0.01x²), where x = max(0, t − 2.5).
  - s is the map's `PowerScale`.
- **Deficit:** g = clamp(R / P, 1, 2).
  - g = 1 means the player has enough power.
- **When g > 1**, the wall applies:
  - Newly spawned enemies get HP × g^1.4.
  - Every enemy hit created from then on is × g^0.6. This covers bullets, lasers and body contact.
- **Hits already created are fixed.** The multiplier is read when an enemy spawns or fires, so enemies and bullets
  already on screen keep their values.

### 3.2 What was removed

- The √P enemy-HP scaling (`EnemyHpForPower`) is gone. It raised enemies alongside the player and ate part of
  every upgrade. An upgrade is now felt in full until the wall.
- The threat curves (`threat.md`) are unchanged: HP, damage, the horde and leaks. They are the normal difficulty.
  The wall stacks on top of them.

### 3.3 Hidden and gradual

**Nothing in the UI shows the requirement.** There is no menu line, no HUD warning and no run-end verdict. The
player only feels it: the swarm gets tougher, and after an upgrade it stays manageable for longer. (A first build
of 4d65357 showed all three. The owner rejected them the same day.)

**It is gradual.** g rises continuously from 1, so there is no jump at 2:30. The damage term is soft (g^0.6), so
the wall is mostly a race to out-damage the swarm and dodging still counts.

**It is beatable.** g is capped at 2: at most HP ×2.64 and damage ×1.52. The 35 % per-hit cap still holds. A very
good player can hold on for a while past their minute. They cannot hold on forever, because the normal threat curve
keeps climbing.

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
| g | deficit | 1–2 |

Examples on map 1:

| Minute | R | HP × at P = 1 | Damage × at P = 1 |
|---|---|---|---|
| 2:30 | 1.00 | 1 | 1 |
| 4 | 1.14 | 1.20 | 1.08 |
| 5 | 1.26 | 1.39 | 1.15 |
| 6 | 1.40 | 1.61 | 1.23 |
| 10 | 2.16 | 2.64 (cap) | 1.52 (cap) |

The minute where P stops being enough on map 1, W(P) = 2.5 + x, where 0.01x² + 0.08x + 1 = P:

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
- **Hopeless deficit:** g is capped at 2, so HP is at most ×2.64 and damage ×1.52. The 35 % per-hit cap
  (`threat.md` §3.7) still prevents one-shots.
- **Leaks:** leak damage is a share of max HP and is not scaled by the wall.

## 6. Dependencies

- `Formulas`
- `EnemyManager` (spawn HP; damage for contact hits)
- `BulletSystem` (enemy shots)
- `Enemy` (laser)
- `RunController` (sets P)
- `Maps`
- `QaAutopilot` (logs power and requirement at each death)

## 7. Tuning Knobs

| Knob | Where | Safe range | Effect |
|---|---|---|---|
| Grace minutes (2.5) | `RequiredPowerGraceMinutes` | 1.5–4 | When a fresh player first feels the wall |
| Linear / quadratic (0.08 / 0.01) | `RequiredPowerLinear` / `RequiredPowerQuadratic` | 0.04–0.15 / 0.005–0.02 | How much P each extra minute costs |
| HP exponent (1.4) | `DeficitHpExponent` | 1.2–2.0 | How steep the wall is |
| Damage exponent (0.6) | `DeficitDamageExponent` | 0.3–1.0 | How much dodging still saves you |
| Deficit cap (2) | `MaxPowerDeficit` | 1.5–3 | How beatable the wall is for a skilled player |
| Map PowerScale | `Maps.All` | 1–3 | Entry power per map |

## 8. Acceptance Criteria

- The `FormulasTests` tests `test_required_power_*` and `test_power_deficit_*` pass.
- `MapsTests` shows PowerScale rising map to map.
- **QA autopilot** (fresh save, P = 1): every `[QA] Died at` line after 2:30 shows required > power.
- **Owner playtest:**
  - With no upgrades, a human reaches 3–4 minutes.
  - After upgrading, the run lasts noticeably longer.
  - No screen mentions the requirement.
