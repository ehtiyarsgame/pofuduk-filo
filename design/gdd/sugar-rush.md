# Şeker Hücumu & Pofuduk Filo — Signature Loop

## 1. Overview

Two linked systems give Pofuduk Filo an identity its genre neighbours lack.
**Şeker Hücumu** (Sugar Rush) is a combo-driven fever mode. Kills chained within a
short window build a combo and fill a meter. A full meter unleashes a timed rush:
faster weapons and doubled XP. Getting hit breaks the combo, which rewards dodging.
**Pofuduk Filo** (the Fluffy Fleet) is the payoff you can see on screen. Elites,
bosses and kill milestones drop rescue bubbles holding pilot friends. Each friend
you catch joins as an auto-firing wingman beside your ship, so your fleet visibly
grows during a run.

## 2. Player Fantasy

"I'm untouchable and my squad is huge." Vampire-likes deliver power through
numbers you rarely see. Here power has faces: four chibi pilots fly in formation
around the bunny and the screen erupts in rainbow during a rush. The combo gives a
bullet-hell skill layer inside an auto-shooter. You never aim, but weaving through
bullets is what keeps the sugar flowing.

## 3. Detailed Rules

### Combo
- Every enemy kill adds +1 combo and resets the combo timer to `comboWindow`.
- When the timer reaches 0, the combo resets to 0.
- Any damage taken by the player resets the combo to 0 immediately.
- The HUD shows the combo from 5 upward. It fades as the timer runs out, and its colour warms from cream to hot pink as it nears 60.

### Rush meter
- Each kill adds `killValue × (1 + combo × comboBonus)` to the meter. `killValue` is 1 for normal enemies, `eliteKillValue` for elites and `bossKillValue` for bosses.
- Getting hit multiplies the meter by `meterKeptOnHit` (the rest is spilled).
- When the meter reaches `meterMax`, a rush starts and the meter resets to 0.

### Rush
- A rush lasts `rushSeconds`. The meter bar drains over that time and cycles through rainbow colours; a pulsing rainbow glow frames the screen.
- During a rush, all weapon and wingman cooldowns tick `rushFireRate`× faster, and XP gained is ×`rushXpMultiplier`.
- Kills during a rush still extend the combo but do not fill the meter. Getting hit during a rush breaks the combo but does not end the rush.

### Fleet
- A rescue bubble spawns where an elite or a boss dies. One also spawns when the run's kill count reaches each value in `killMilestones`.
- Bubbles fall at `capsuleFallSpeed`, homing gently toward the ship within 2.2× `pickupRadius`. The ship collects a bubble on contact within `pickupRadius`. A bubble missed by 12 units below the ship is lost.
- Pilots cycle in order: chick, cat, hamster, fox.
- A rescue adds a wingman in the next free slot: (±1.15, −0.35), then (±2.1, −0.95), up to 4 wingmen. Each rescue beyond 4 raises all wingman damage by `powerPerExtraRescue`.
- Each wingman fires a star every `fireInterval` at the nearest enemy within `targetRange`, or straight up if none is in range. Shots never aim more than ~78° below horizontal toward the player's space.
- The fleet and the rush reset when a run ends or the game returns to the menu.

## 4. Formulas

| Name | Formula | Variables | Range | Example |
|---|---|---|---|---|
| Meter gain | `g = v × (1 + c × b)` | v = kill value (1 / 10 / 35), c = combo after this kill, b = `comboBonus` (0.03) | 1.03 – ~50 | Combo 20, normal kill: 1 × 1.6 = 1.6 |
| Kills to rush | `≈ meterMax / mean(g)` | `meterMax` = 120 | 50–116 kills | At a steady combo of 25: 120 / 1.75 ≈ 69 kills (QA run 13 at 70: one rush every ~20 s) |
| Wingman damage | `d = baseDamage × power × DamageMultiplier` | baseDamage = 7, power = 1 + 0.25 × extra rescues | 7 – ~30 | 2 extras, +30 % damage stat: 7 × 1.5 × 1.3 = 13.7 |
| Wingman DPS | `d × rushFireRate / fireInterval` | fireInterval = 0.6 s | 11.7 – 85 per wingman | 7 / 0.6 = 11.7; during a rush: 19.8 |

## 5. Edge Cases

- **Hit during invulnerability frames:** PlayerHealth ignores the hit, so no `Damaged` event fires and the combo survives.
- **Several kills in one frame (bomb, area weapon):** each kill counts separately. A bomb can therefore trigger a rush on its own. This is intended: the bomb is a rare pickup.
- **Boss killed during a rush:** the combo increases and the meter does not change. The boss still drops a bubble.
- **Level-up screen or pause:** timers use scaled time, so combo and rush freeze while paused or during a draft.
- **Revive:** combo is 0 (death was a hit). The meter keeps what the hit left. An active rush continues if time remains.
- **Bubble collected at a full fleet:** it grants power (+0.25) instead of a wingman, shows the toast "Filo güçlendi!" (Fleet powered up!), and plays the same pop and sparks.
- **Endless mode:** milestones stop after the last entry, but elites and bosses keep dropping bubbles.

## 6. Dependencies

- **EnemyManager** (`EnemyKilled`): combo, meter and bubble spawns.
- **PlayerHealth** (`Damaged`): combo break and meter spill.
- **RunController** (`StateChanged`): reset on MainMenu or RunEnd.
- **WeaponBehaviour** reads `SugarRush.FireRate`; **XpSystem** reads `SugarRush.XpMultiplier`.
- **BulletSystem**: wingman shots use the Star bullet type.
- **WeaponInventory.Stats**: wingman damage scaling.
- **GameUI** (`GameUI.Rush.cs`): meter, combo, rush glow and toasts.
- **AudioManager**: Evolution sound when a rush starts, LevelUp sound on a rescue.
- **QaAutopilot**: telemetry records rushes, best combo and fleet size.

## 7. Tuning Knobs

| Knob | Default | Safe range | Affects |
|---|---|---|---|
| `comboWindow` | 1.6 s | 1.0–2.5 | How forgiving combos are; below 1.0 combos only last during dense waves |
| `meterMax` | 120 | 70–160 | Rush frequency (target: about one rush per 60–90 s for a decent player) |
| `comboBonus` | 0.03 | 0–0.06 | How much skill (keeping a long combo) speeds up rushes |
| `meterKeptOnHit` | 0.5 | 0.25–0.8 | How hard getting hit is punished |
| `rushSeconds` | 6 | 4–9 | Length of the power spike |
| `rushFireRate` | 1.7 | 1.3–2.2 | Size of the power spike; above 2.2 bullet density hurts readability |
| `rushXpMultiplier` | 2 | 1.5–3 | Progression pull toward rushes |
| `killMilestones` | 40, 160, 400, 800 | — | Guarantees the first wingman arrives within about 40 s of a run |
| `baseDamage` / `fireInterval` | 7 / 0.6 s | 4–12 / 0.4–1.0 | Wingman share of total DPS (target: 15–25 % with a full fleet) |
| `powerPerExtraRescue` | 0.25 | 0.1–0.4 | Late-run value of rescues |

## 8. Acceptance Criteria

1. Killing 5 enemies within 1.6 s of each other shows "5 KOMBO". Waiting 1.6 s without a kill hides it.
2. Taking a hit sets the combo to 0 and visibly halves the meter bar.
3. Filling the meter shows the toast "ŞEKER HÜCUMU!" (Sugar Rush!), plays the Evolution sound and turns on the rainbow edge glow. The glow and the rainbow bar end after 6 s.
4. During a rush, the Feather Blaster fires about 1.7× as many shots per second (count in a 5 s window). An XP gem gives double XP.
5. The first rescue bubble appears on or before the 40th kill. Touching it adds a wingman beside the ship and shows "Filoya katıldı! (1/4)" (Joined the fleet!).
6. With 4 wingmen, the next bubble shows "Filo güçlendi!" and does not add a fifth wingman.
7. Returning to the menu and starting a new run begins with 0 wingmen, 0 combo and an empty meter.
8. QA telemetry logs rushes, best combo and fleet size every 10 s.
