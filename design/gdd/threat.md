# Threat Curve: the Wall an Un-upgraded Player Hits

> Status: implemented 2026-09-25 · Related: `power-match.md`, `sugar-rush.md`, `ad-rewards.md`, `meta-economy.md`

## 1. Overview

Ball Blast's loop only works if a run *ends*. The player dies, spends the gold on upgrades and goes further next
time. Device feedback on 2026-09-25 said the opposite was happening: "kaç dakikadır oynuyorum, geliştirmesiz… ölmek
imkansız gibi" ("I've been playing for minutes without any upgrades… it feels impossible to die"). QA run 42 backs
this up. A fresh save with no upgrades played the full 7 minutes without dying, and its HP stayed near full.

The threat curve fixes this by making enemy fire hit harder and more often as the run clock rises. Healing is
cut back at the same time. A player with no Forge levels now meets a wall. Upgrades, the ad revive and a good
build move that wall further out.

## 2. Player Fantasy

"I got further than last time, and I can see exactly which upgrade would push me past minute 5."

## 3. Detailed Rules

### 3.1 Enemy fire

- **Damage:** every enemy bullet, from troops and bosses alike, deals `base × EnemyDamageScale(t)`.
  This is applied in `BulletSystem.SpawnEnemyBullet`.
- **Fire rate:** every troop's fire timer runs `EnemyFireRateScale(t)` times faster (`Enemy.Tick`). This replaces
  the old +8 %/min ramp, which was capped at ×1.8.

### 3.2 Healing and mercy

| Knob | Was | Now |
|---|---|---|
| Heart drop chance per kill | 0.4 % | 0.18 % (0.12 % in QA run 45 was too stingy) |
| Heart heal | 20 % max HP | 15 % |
| "Şeker Molası" fallback heal | 30 | 20 |
| Invulnerability after a hit | 1.2 s | 0.9 s |
| Magnet pickup | 0.3 % | removed (pickups auto-fly after 1.2 s) |

### 3.3 Sugar Rush

- The meter is 130 at the start, and bigger enemies fill it by their XP value (run 51: no rush in 4 min at 160 and 1 per kill). Before that it was 160 (was 120). A meter of 200 gave no rush at all in 3 minutes (QA run 45).
- From minute 2 it grows +40 % of that base per minute (was +35 %).
- A hit keeps only 30 % of the meter (was 50 %).
- A rush lasts 5 s (was 6).

QA run 42 had a rush roughly every 45 s. The target is one every 90–120 s.

## 4. Formulas

- `EnemyDamageScale(t) = 1 + 0.15t + 0.025t²` (0.2t in run 49; run 51, with every enemy type spawning, died at 155 s). Examples: ×1 at 0, ×1.7 at 3 min, ×2.4 at 5 min, ×3.3 at 7 min.
  So a 10-damage bullet hits for 18 at 3 min and 26 at 5 min, against a 100–150 HP ship.
  QA run 45 used 0.3t + 0.03t², and the autopilot died at 142 s: too early, because Power Match also
  raised enemy HP about 3× over the same stretch.
- `EnemyFireRateScale(t) = min(1 + 0.1t, 2.2)`.
- Enemy HP (`EnemyHp`) and Power Match are unchanged.

## 5. Edge Cases

- **Resumed run:** the scale reads `EnemyManager.RunMinutes`, so a resumed run continues at the threat level it
  was saved at.
- **Bullets already in flight:** damage is fixed when a bullet spawns. Bullets already on screen are not
  rescaled.
- **Armour:** armour subtracts a flat amount after scaling (`PlayerHealth.TakeDamage`, minimum 1). Kaplumbağa
  Kabuğu and the workshop's armour therefore lose value as the run goes on. This is intended: they help the
  early and middle game, not the wall.

## 6. Dependencies

- `BulletSystem`
- `Enemy`
- `EnemyManager.RunMinutes`
- `PickupSystem`
- `PlayerHealth`
- `SugarRush`
- `RunController`

The meta economy (Forge, workshop) and ad revive are the counters to the wall.

## 7. Tuning Knobs

| Knob | Where | Safe range | Effect |
|---|---|---|---|
| Damage curve coefficients (0.2, 0.025) | `Formulas.EnemyDamageScale` | 0.15–0.5 / 0–0.06 | When the wall arrives |
| Fire-rate slope and cap | `Formulas.EnemyFireRateScale` | 0.05–0.15 / 1.5–3 | Bullet density |
| Heart chance / heal | `PickupSystem` | 0.0005–0.003 / 0.1–0.25 | Recovery |
| Invulnerability | `PlayerHealth` | 0.6–1.2 s | Forgiveness after a hit |

## 8. Acceptance Criteria

- `test_enemy_damage_scale_grows_with_run_time` and `test_enemy_fire_rate_scale_is_capped` pass.
- **Wall timing:** in the QA autopilot (fresh save, starter pilot, no upgrades, a figure-eight that never aims),
  the first death happens between 3 and 6 minutes (`[QA] Died at`). If it lands outside that window, retune §7.
- **Sugar pacing:** QA telemetry shows at most one rush per 90 s on average (`rushes` column).
