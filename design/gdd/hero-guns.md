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

## 8. Acceptance

- Starting a run as any hero gives an ANA SİLAH card track for that hero's own gun, and visible bullets from the
  ship.
- The hero's signature weapon is in the second slot.
