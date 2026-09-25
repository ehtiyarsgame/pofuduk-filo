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

## 8. Acceptance

- Starting a run as any hero gives an ANA SİLAH card track for that hero's own gun, and visible bullets from the
  ship.
- The hero's signature weapon is in the second slot.
