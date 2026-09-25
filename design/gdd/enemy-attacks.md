# Enemy Attacks: One Readable Shot per Enemy

> Status: implemented 2026-09-25 · Owner: combat · Related: `threat.md` §3.6 (the horde), `weapon-system.md`

## 1. Overview

Device feedback on 2026-09-25: *"düşmanların hep bir ateş türü var nerdeyse, o da belli değil, arada kaynıyor"*
("the enemies nearly all have one kind of shot, and it isn't even distinct; it gets lost in the crowd").

Every enemy type now throws its own shot, drawn to say who threw it, in its own pattern. The dangerous ones give
a warning first. A crowded screen stays readable, and each new enemy teaches the player one dodge.

## 2. Player Fantasy

"Yellow eggs just drop, I sidestep them. The bee is glowing, so it's about to shoot: move. A line appeared under
the donut, so a laser is coming. That marshmallow ring has a gap, go through it."

## 3. Detailed Rules

### 3.1 The roster

| Enemy | Shot (sprite) | Pattern | Tell | Counter |
|---|---|---|---|---|
| Civciv (Chick) | yellow egg `b_egg` | 1 shot straight down, not aimed, slow (2.4) | — | sidestep |
| Şeker Arı (Candy Bee) | orange stinger `b_stinger`, faces its flight | 1 aimed shot, fast (6.5), 6 damage | glows for 0.6 s | move when it glows |
| Kurabiye Robot (Cookie Robot) | cookie crumb `b_crumb` | 3-round aimed burst, 0.14 s apart | glows for 0.35 s | move after the burst |
| Sakız Balonu (Gum Balloon) | gum drop `b_gum` | never shoots; **pops into a ring of 8 when killed** | — | don't pop it next to you |
| Jöle Ayı (Jelly Bear) | jelly blob `b_jelly` | 1 slow aimed blob that **splits into 3** (±35°) after 1.1 s | the blob itself | get clear before it splits |
| Donut UFO | laser `fx_laser_beam` | beam straight down for 0.6 s; the UFO nearly stops | a dashed warning line for 0.9 s | leave the line |
| Dondurma Kulesi (Ice Cream Tower) | scoop `b_scoop` | 3-armed spiral: 6 volleys, 0.28 s apart, turning 20° each | — | slip between the arms |
| Marshmallow | puff `b_puff` | ring of 14 with **one 60° gap** at a random angle | — | find the gap |
| Elit Civciv (Elite Chick) | golden egg `b_gold_egg` | 3-round aimed burst **and a lunge** at the ship every 4 s | glows for 0.4 s | sidestep the lunge |

Bosses keep their own patterns (`BossEnemy`).

### 3.2 Mechanisms (all in `Enemy`, set per prefab in `ContentBuilder.BuildEnemies`)

- **`shotTelegraphSeconds`.** The sprite pulses to a warm red over the last part of the fire timer.
- **`burstCount` / `burstGap`.** The volley repeats N times, gap seconds apart.
- **`spinPerShot`.** The base angle of a volley that is not aimed turns after every shot, which makes a spiral.
- **`ringGapDegrees`.** The volley becomes a full ring, minus one gap, at a random angle.
- **`deathBurstBullets`.** Fires a ring when the enemy is killed (`Enemy.OnKilled`, called by
  `EnemyManager.DamageEnemy`). Enemies that leak off the bottom never burst.
- **`splitFuse` / `splitTypeIndex`.** The shot's lifetime is its fuse. When the fuse runs out, the shot splits
  into three children at ×1.5 speed and 60 % damage (`BulletSystem.SplitExpiredShots`). A shot that hits the
  ship, is absorbed, or leaves the screen does not split.
- **`lungeInterval` / `lungeSpeed` / `lungeSeconds`.** The enemy rushes toward the ship for a moment.
- **Laser (`laserBeamSprite`).**
  - First a dashed warning line shows for `laserWarnSeconds`, then the beam fires for `laserFireSeconds`.
  - The beam hits a ship below the UFO within `laserHalfWidth` of its x. The ship's i-frames limit this to
    one hit per 0.9 s.
  - The UFO moves at 20 % speed during the attack.
  - Being stunned or leaving the screen cancels the attack.

## 5. Edge Cases

- **Stunned enemy:** a stun cancels a burst in progress and the laser, and clears the telegraph tint.
- **Popping a balloon by ramming it:** the death ring spawns on the ship. The contact hit's i-frames (0.9 s)
  cover it.
- **Formations:** formations use the same prefabs, so a formation of bees stings and a formation of UFOs lasers.
- **Sugar Rush clear:** children of a split can spawn in the frame of a clear. They are rare and harmless.

## 6. Dependencies

- `Enemy`
- `EnemyManager` (the `OnKilled` hook)
- `BulletSystem` (`SpawnEnemyBullet(..., splitIntoType)`, `SplitExpiredShots`)
- `BulletData.SplitInto`
- `PlayerHealth` (laser damage)
- `ProceduralArt.EnemyShots` (art)
- `ContentBuilder` (bullet types 11–19 and the enemy configs)

## 7. Tuning Knobs

| Knob | Where | Default | Safe range |
|---|---|---|---|
| Bee telegraph / stinger speed | ContentBuilder | 0.5 s / 7.5 | 0.35–0.8 / 5–9 |
| Cookie burst count / gap | ContentBuilder | 3 / 0.14 s | 2–4 / 0.1–0.25 |
| Balloon ring size / speed | ContentBuilder | 8 / 2.6 | 6–12 / 2–3.5 |
| Jelly fuse | ContentBuilder | 1.1 s | 0.8–1.6 |
| Laser warn / fire / half width | Enemy fields | 0.9 / 0.6 / 0.24 | 0.7–1.2 / 0.4–1 / 0.18–0.35 |
| Spiral volleys / turn | ContentBuilder | 6 / 20° | 4–10 / 12–30° |
| Marshmallow ring / gap | ContentBuilder | 14 / 60° | 10–18 / 45–90° |

## 8. Acceptance Criteria

- In the QA screenshots, eggs, stingers, crumbs, jelly, scoops and puffs are all visible and distinct. None of
  the old red orbs come from regular enemies.
- A UFO shows a dashed line before its beam, and the beam damages the ship only while it is firing.
- Killing a balloon produces a ring of 8 gum drops. A balloon that leaks off the bottom produces none.
- A jelly blob that neither hits nor leaves the screen becomes three smaller blobs after 1.1 s.
- No exceptions in `player.log`, and `[QA] RESUME OK`.
