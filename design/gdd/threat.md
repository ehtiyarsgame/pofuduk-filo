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

### 3.4 Human-tuned wall (2026-09-25 revision)

Device feedback on 2026-09-25: *"oyun başları hala çok kolay… 10 dakikadan fazla oynayabiliyorum, çok
geliştirme yapmadan"* ("the openings are still too easy… I can play for more than 10 minutes without much
upgrading"). The QA bot never dodges, and it hid how easy the game is for a human who does.

The wall is now a **DPS race**, which dodging alone cannot beat:

- **Enemy HP:** HP = base × (1 + 0.35t + 0.06t²). That is ×4.25 at 5 minutes and ×10.5 at 10 minutes.
- **Spawn budget:** 2 + 1.1t + 0.12t², up from 1.5 + 0.9t + 0.08t². There is more pressure from second 0.
- **Bullet damage:** 1 + 0.25t + 0.04t².
- **Body contact:** touching an enemy deals 14 × the damage scale, or ×1.5 that for elites and bosses. Small
  enemies are destroyed by the ram. A swarm that isn't shot down is now dangerous.

With these settings the QA bot is expected to fall at about 90–150 s. A human without upgrades should fall at
about 3–5 minutes. The owner's playtest, not the bot, is the calibration target.

### 3.5 Leaks: the DPS race finally binds (2026-09-25, second revision)

Device feedback: *"ilk oynayışımda geliştirince ikinci oynayışımda istersem yanmam"* ("after upgrading in my
first run, in the second one I don't die if I don't want to").

**Root cause.** An enemy that fell off the bottom of the screen simply vanished. The HP curve in §3.4 raced the
player's damage, but losing that race cost nothing: a player who dodged well let the swarm pass and never died.
Upgrades made this worse, because they bought HP and armour against bullets, which a good dodger barely needed.
Modelling one run's gold (≈ 500–1 000) through the Forge and workshop gives about ×1.2–1.3 net DPS after the √P
enemy-HP scaling. That is a modest gain. The meta curve was not the problem; the missing penalty was.

**Rule.** An enemy that escapes off the bottom alive costs the player a share of max HP
(`Formulas.LeakDamageFraction`):

- Size share: clamp(2 % + 0.1 % × base HP, 3 %, 10 %), doubled for elites.
  - Examples: Chick 3 %, Cookie Robot 4.5 %, Jelly Bear 6.5 %, Ice Cream Tower 9 %, elite up to 20 %.
- The share is multiplied by the enemy's HP left, with a floor of 0.2. A nearly killed leaker costs a fifth.
- Leak damage ignores armour and i-frames, and grants none.
- Leak damage does not feed the DifficultyDirector, so the wall does not soften itself.
- Bosses never leak.
- The HUD shows a red band along the bottom edge and "Kaçtı! −N" ("Escaped! −N"). Leaks within 1.2 s add
  up into one number.

**Effect.**
- Leak damage is a share of max HP, so HP upgrades do not dilute it. Only damage (Forge, weapons, passives)
  holds the line.
- Dodging is still the answer to bullets.
- The player now faces a real positioning trade-off: stay low and safe, or move up and across to cut the
  swarm before it passes.

### 3.6 The horde (2026-09-25, third revision)

Device feedback, with a screenshot of a level 11 run showing barely any enemies on screen: *"canavarları
çoklatabiliriz, hem de stres atar; çok güçlü olduğunda… güçlü olduğunu buradan bilir… hala çok ama çok ama çokkk
basit"* ("we can multiply the monsters, it also relieves stress; when you're very strong you know it from this…
still very, very, veryyy easy"). QA telemetry agreed: 4–12 enemies on screen at a time.

**Rule.** The crowd is both the power readout and the threat:
- A strong build mows it down, and the combo climbs.
- A weak one cannot keep up. The swarm leaks (§3.5) and runs into the ship.

| What | Before | After | Why |
|---|---|---|---|
| Spawn budget (points/s) | 2 + 1.1t + 0.12t² | **5 + 2.75t + 0.3t²** | 2.5× as many enemies |
| Fodder HP (Chick / Bee / Cookie Robot / Balloon) | 10 / 8 / 25 / 20 | **7 / 6 / 18 / 14** | Each dies fast, but the crowd needs about 1.75× the DPS |
| Fodder fire interval (s) | 5 / 4 / 3.4 / 6 | **9 / 7 / 5.5 / 9** | The bodies are the threat, not a bullet storm (about 1.4× the bullets overall) |
| XP per level | `XpToNextLevel` | **× 2.5** (`XpScale`) | Levelling pace stays as it was |
| Sugar meter | 130 | **320** | Rush pace stays as it was |
| Coin / heart / bomb chance per kill | 0.12 / 0.0018 / 0.002 | **0.05 / 0.0007 / 0.0008** | Per-minute drops stay as they were; gold a little lower |

**Follow-up (same day):** *"düşmanları bence biraz daha arttıralım"* ("I think we should increase the enemies a
bit more"). The budget now rises another ×1.3, to **6.5 + 3.6t + 0.39t²**, which is 3.25× the original. XP per
level (×3.25), the sugar meter (420) and the drop chances (0.04 / 0.00055 / 0.0006) follow it.

Big enemies, elites, formations and bosses are unchanged. Leak shares follow base HP, clamped at a 3 % floor,
so they stay the same for fodder.

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
