# Hero Main Guns

> Status: implemented 2026-09-25 · Related: `weapon-system.md`, `meta-economy.md` §3.3 B

## 1. Overview

Device feedback on 2026-09-25: *"her kahramanın kendine has silahı olmalı"* ("every hero should have a weapon of
their own"). Every hero flies with **two** weapons of their own:

- **Main gun (ANA SİLAH).** It fires straight from the ship's wing barrels, so the player always sees their
  ship shooting.
- **Signature weapon.** This is the hero's second slot.

Main guns run on the `FeatherBlaster` behaviour, and each one has its own bullet and firing pattern. Every
main gun evolves at level 5 when the player owns **Kristal Gözlük**, the same key the Feather Blaster uses.

## 3. Detailed Rules

| Hero | Main gun → evolution | Bullet | Feel | Signature |
|---|---|---|---|---|
| Pıtır | Tüy Blaster → Tüy Fırtınası | feather | balanced | — |
| Cıvık | Civciv Topu → Dev Civciv Topu | chick orb | slow and heavy; every 4th shot ×3 at Lv5 | Yumurta Havanı |
| Mırnav | Kıvılcım Tabancası → Yıldırım Tabancası | spark | very fast and light | Kıvılcım Kedi |
| Balonbaş | Balon Tüfeği → Balon Fırtınası | bubble | piercing from Lv1 | Sakız Balonu |
| Yıldızpati | Yıldız Yayı → Kuyruklu Yıldız Yayı | star | wide fan | Yıldız Bumerang |
| Pengu | Buz Tabancası → Buzul Topu | ice shard | fast, deep pierce | Balık Füzesi |
| Kuzu | Yün Atar → Yün Kasırgası | yarn ball | big and slow | Yün Yumağı |

- **Card pool.** Main guns carry the `heroOnly` flag. They appear in the level-up card pool only for the hero who
  owns them, and they never appear in the Weapons (Lab) screen.
- **Tuning.** Damage per second is tuned to roughly the Feather Blaster's: about 95 at Lv1 and about 190 at Lv5.
- **Hangar.** Each hero's row shows "main gun + signature".
- **Wing guns.** They flash and recoil for the main gun only.

### 3.2 Traits per level (2026-09-25)

Every main-gun level adds a **new trait**, not only more bullets (*"her zaman aynı tip silah… farklı olmayı
ister kullanıcı"* — "always the same kind of gun… players want something different"). The traits are
`BulletEffect` flags, resolved when a shot hits (`BulletSystem.ApplyEffects`):

- **Patlama (explode):** deals 50 % damage within 1 unit.
- **Bölünme (split):** throws off 2 shards at ±40°, each dealing 45 %.
- **Sıçrama (chain):** a spark jumps to the nearest other enemy within 3.5 units and deals 60 %.
- **Yavaşlatma (slow):** the enemy moves at 55 % speed for 1.5 s.

Secondary damage never carries traits, so hits never cascade. Each gun unlocks its traits in its own order:

| Gun | Lv2 | Lv4 | Evolution |
|---|---|---|---|
| Tüy Blaster | split | pierce | split + explode |
| Civciv Topu | explode | + split (baby chicks) | explode + split |
| Kıvılcım | chain | + slow | chain + slow |
| Balon Tüfeği | slow | + explode | slow + explode |
| Yıldız Yayı | chain | + split | chain + split |
| Buz Tabancası | slow | + split (shatter) | slow + split |
| Yün Atar | slow | Lv3 split, Lv4 explode | all three |

### 3.3 Mechanics of their own (2026-09-25, round 2)

Device feedback: *"kahramanların ateşi de ateşlemesi de hepsi farklı şekilde olsun, farklı şekilde güçlensin"*
("the heroes' fire and the way they fire should all be different, and each should grow in its own way"). Every
gun now plays differently and grows along its own axis. Round 2 covers four heroes; round 3 will cover
Balonbaş, Yıldızpati and Kuzu.

| Hero | Gun | How it fires | Grows by | Level path | Evolution |
|---|---|---|---|---|---|
| Pıtır | Tüy Blaster (`FeatherBlaster`) | straight parallel lanes | **width** | 1 → 2 → 3 lanes (split at Lv3) → +2 wing feathers → 4 lanes, pierce, giant every 5th | Prism Beam (unchanged) |
| Cıvık | Civciv Topu (`FeatherBlaster`) | heavy explosive ball | **blast area** | Lv2 explodes, radius 1.0 → 1.15 → 1.3 (+chicks) → 1.45 | Dev Civciv Topu, radius 1.9 |
| Mırnav | Kıvılcım (`ChainArcGun`) | **no bullet**: an instant arc to the nearest enemy within range, then jumps within 3.5 u (×0.85 per jump) | **chain** | 1 → 2 → 2 (+0.4 s stun) → 3 (+slow, 0.5 s) → 4 jumps (0.6 s); range 4.5 → 5.6 | Yıldırım: 7 jumps, 0.8 s stun, range 6.5 |
| Pengu | Buz Işını (`IceBeamGun`) | **continuous beam** straight up; hits the first 1 + pierce enemies, slows them | **control** | Lv2: 1.2 s in the beam freezes (0.8 s); wider; Lv4 splits into 2 beams; Lv5 freeze 1.2 s | Buzul Işını: 3 beams, 7 enemies, 1.5 s freeze |

**Stat mapping for the new behaviours:**
- **Chain:** `projectileCount` = jumps, `area` = range from the ship, `lifetime` = stun seconds.
- **Beam:** `cooldown` = damage tick, `projectileCount` = beams, `area` = half-width, `pierce` = extra enemies per
  beam, `lifetime` = freeze seconds.
- **Explosive shots:** they carry their own blast radius (`BulletData.Area`, from the level's area × area bonuses).
- **Pıtır's wing feathers:** use the new `WeaponLevelStats.sideShots` and deal 80 % damage.

**Mırnav's range is short on purpose.** He must fly up to the swarm, while Pengu and Pıtır work from below. This
fits the leak rule (`threat.md` §3.5).

### 3.4 Round 3: Balonbaş, Yıldızpati, Kuzu (2026-09-25)

| Hero | Gun | How it fires | Grows by | Level path | Evolution |
|---|---|---|---|---|---|
| Balonbaş | Balon Tüfeği | bubbles that **bounce** off the side walls, the HUD edge and enemies, ×1.15 damage per bounce | **bounces** | 2 bubbles, 1 bounce → slow, 2 → 3 bubbles → burst, 3 → 4 bubbles, 4 | Balon Fırtınası: 6 bubbles, 6 bounces |
| Yıldızpati | Yıldız Yayı | wide fan of stars | **aim** | 3-star fan → chain → **homing** from Lv3 (200°/s) → 4 stars + split (260°/s) → 5 stars, ×3 every 5th (320°/s) | Kuyruklu Yıldız: 7 homing stars, pierce 1 (420°/s) |
| Kuzu | Yün Saçmalı | **shotgun**: a wide, short-range spray (speed × life ≈ 3.6 u) | **close punch** | 5 pellets → knockback → 7 → slow → 9 pellets, pierce 1 | Yün Kasırgası: 12 pellets, 80°, pierce 2 |

**Mechanisms.**
- **Bounce.** `BulletData.Bounces`:
  - `MoveBulletsJob` reflects a bouncing shot off the visible walls (`Walls`: the camera sides and `Playfield.TopY`).
  - `PlayerBulletCollisionJob` sends it back off an enemy instead of popping it.
  - Each bounce multiplies damage by ×1.15 and keeps the shot alive for at least 1.2 s more.
- **Homing.** `BulletData.Homing`, in °/s: `HomingJob` turns the shot toward the nearest visible enemy within 9 u
  before it moves.
- **Knockback.** `BulletEffect.Knockback` pushes the enemy 0.3 u back up the screen. Bosses are not pushed.

**Round-3 gun mods.**

| Gun | Track 1 | Track 2 | Track 3 |
|---|---|---|---|
| Balon Tüfeği | Lastik Sakız (Rubber Gum): +1 bounce at Lv2 and Lv4 | Ek Balon (Extra Bubble) | Sert Balon (Hard Bubble) |
| Yıldız Yayı | Yıldız Pusulası (Star Compass): homing +15 %/lv | Ek Yıldız (Extra Star) | Parlak Yıldız (Bright Star) |
| Yün Saçmalı | Ek Saçma (Extra Pellet): +1 pellet per level | Uzun Namlu (Long Barrel): range +10 %/lv | Sıkı Yün (Tight Wool) |

## 4. Gun mods: permanent tracks per gun (hero-guns.md §4)

Owner decision on 2026-09-25: *"her silahın kendine özgü geliştirmesi olsun"* ("every gun should have its own
development").

**How it works.**
- Every hero gun has three permanent tracks of its own (`Meta/GunMods.cs`, pure).
- They are bought at the top of the Weapons screen, under KAHRAMAN SİLAHLARI (HERO GUNS), for every unlocked
  pilot.
- Each track goes up to Lv5 at 250·1.6^L gold (250, 400, 640, 1 020, 1 640). That is about 3 950 per track and
  about 11 850 per gun.
- Each mod level adds 0.01 to the power coefficient.
- An evolution keeps its base gun's mods (`GunModState`).

| Gun | Track 1 | Track 2 | Track 3 |
|---|---|---|---|
| Tüy Blaster | Ek Akış (Extra Lane): +1 lane at Lv3 and Lv5 | Hızlı Kanat (Quick Wings): fire rate +4 %/lv | Keskin Tüy (Sharp Feather): damage +5 %/lv |
| Civciv Topu | Büyük Patlama (Big Blast): blast +10 %/lv | Ek Top (Extra Ball): +1 ball at Lv3 and Lv5 | Ağır Top (Heavy Ball): damage +5 %/lv |
| Kıvılcım | Uzun Zincir (Long Chain): +1 jump at Lv2 and Lv4 | Geniş Menzil (Wide Range): range +8 %/lv | Şok (Shock): stun +15 %/lv |
| Buz Işını | Kalın Işın (Thick Beam): width +8 %/lv | Derin Donma (Deep Freeze): freeze +12 %/lv | Keskin Soğuk (Biting Cold): damage +5 %/lv |
| Balon / Yıldız / Yün (until round 3) | +1 count at Lv3 and Lv5 | fire rate +4 %/lv | damage +5 %/lv |

**Application.** `WeaponBehaviour.Update` applies the mods to the level stats before build-card bonuses:
- damage × (1 + d)
- cooldown ÷ (1 + r)
- area × (1 + a)
- lifetime × (1 + t)
- count + c
- pierce + p

## 8. Acceptance

- Starting a run as any hero gives an ANA SİLAH card track for that hero's own gun, and visible bullets from the
  ship.
- The hero's signature weapon is in the second slot.
- `GunModsTests` pass: costs, three tracks per hero gun, whole-level steps, clamping.
- As Mırnav, arcs jump between enemies with no bullets. As Pengu, a beam holds and enemies freeze (QA plays a
  Pengu trial).
- The Weapons screen lists KAHRAMAN SİLAHLARI (HERO GUNS) with three buyable tracks per owned pilot's gun.
