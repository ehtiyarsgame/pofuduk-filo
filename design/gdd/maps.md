# Maps: Harder Places That Pay More

> Status: implemented 2026-09-25 · Owner: design · Related: `threat.md`, `economy.md` §3.4, `main-menu.md` §6

## 1. Overview

Owner idea on 2026-09-25: *"eğer düşman basit görürse 2 tane daha haritamız olsun, çok çok daha zor olsun orası ama
parası da çok olacak"* ("if the player finds the enemies easy, let's have 2 more maps; they should be much, much
harder, but pay a lot more").

Each map is the same endless run with harder numbers and a bigger payout:
- A player who has outgrown map 1 moves on for money.
- Hitting the new wall sends them back to upgrade.
- The loop never flattens out.

## 3. Detailed Rules

| # | Map | Unlock | Enemy HP | Spawn | Enemy damage | Threat clock | Gold | Sky tint |
|---|---|---|---|---|---|---|---|---|
| 1 | Şekerkamışı (Candy Cane) | open | ×1 | ×1 | ×1 | +0 min | ×1 | none |
| 2 | Jöle Nebulası (Jelly Nebula) | survive **15:00** on map 1 | ×1.8 | ×1.3 | ×1.4 | +3 min | **×2.5** | mint |
| 3 | Kurabiye Kuşağı (Cookie Belt) | survive **15:00** on map 2 | ×3 | ×1.6 | ×1.8 | +6 min | **×5** | biscuit |

- The unlock time was 25:00 at first; the owner changed it to 15:00 for both maps (`Maps.UnlockSeconds`).
- **Threat clock.** Every threat curve (HP, bullet and contact damage, fire rate, spawn budget) reads
  `EnemyManager.ThreatMinutes`, which is run minutes plus the map's offset. Map 2 opens as hard as minute 3 of
  map 1 and keeps climbing from there.
- **Records.** Each map has its own best time (`SaveData.mapBest`). Map 1 is still the classic endless record, and
  it alone feeds the record chests.
- **Lobby.** A selector sits between the Rekor Yolu (Record Road) and OYNA (PLAY): ‹ map · ×gold ›.
  - **Locked map:** it reads "KİLİTLİ · best / 15:00" (LOCKED · best / 15:00). Pressing PLAY explains what opens
    it.
  - **Saved run:** it resumes on its own map (`RunSnapshot.mapIndex`), and the selector cannot change the map
    until that run ends.
- **Look.** The sky and nebula take the map's tint (`BackgroundScroller`).

## 5. Edge Cases

- A save from before maps loads as map 1, with an empty `mapBest`.
- If the selected map somehow becomes locked (for example, an edited save), `SelectedMap` falls back to map 1.

## 7. Tuning Knobs

`Meta/Maps.cs`: every multiplier, the threat offset, `UnlockSeconds` and the tint.

## 8. Acceptance Criteria

- `MapsTests` pass: map 1 is open, map 2 opens at exactly 15:00, maps get harder and pay more, and the threat
  clock adds the offset.
- On map 2, coins show about 2.5× the value and enemies arrive tougher from the first second.
