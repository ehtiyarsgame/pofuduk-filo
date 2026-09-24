# Run Resume — continue exactly where you left

## 1. Overview

The user asked on 2026-09-24: *"oyunu yarıda bırakmak zorunda kaldı, kaldığı yerden devam etsin"* (if the player has to leave a game half-way, it should continue from where they left). A run in flight is saved to its own file, `run.json`, which is signed like the main save. The menu's big button then reads **DEVAM ET** (continue) and brings the run back: same stage, clock, weapons, level, HP, gold, wingmen and difficulty. Enemies and bullets on screen are not stored. A resumed run starts with a 3 s breather and 3 s of invulnerability.

## 3. Detailed Rules

- **When the run is saved.** Only during a run (Playing, Paused or LevelUp):
  - every `autosaveSeconds` (15 s, unscaled) while playing;
  - when the app goes to the background (`OnApplicationPause(true)`), because the OS may kill a backgrounded app without warning;
  - on `OnApplicationQuit`;
  - from the pause menu's **Kaydet ve Çık** (save & quit), which then returns to the menu with no rewards paid yet.
- **What the snapshot holds** (`RunSnapshot`):
  - Pilot id.
  - Stage and run clock: elapsed time, stage start, phase index, endless flag, boss cycle.
  - Power Match state.
  - HP, level, XP and pending level-ups (a draft that was on screen is offered again).
  - Weapons and passives, with levels.
  - Damage bonuses from evolutions and fusions.
  - Run gold, fallback gold, kills, revives, free-revive flag, rerolls, banishes.
  - Banked stage gold and stardust, highest stage cleared.
  - Fleet: wingmen, power, milestone progress.
- **Boss alive at save time.**
  - A stage boss is not stored. The phase index is saved one step back, so the boss phase is entered again and the boss re-spawns at full HP.
  - In endless mode, a returning boss comes back 5 s after the resume.
- **When the snapshot is deleted.**
  - When a new run starts (OYNA, Yeni Oyun, Tekrar Oyna).
  - On death: the death screen is never saved, so quitting there cannot bring back a living run.
  - When the run ends.
  - A revive re-saves on the next frame.
- **Menu with a saved run.**
  - The big button reads **DEVAM ET**.
  - A small **Yeni Oyun** (new game) button discards the saved run and starts over.
  - The line under the big button reads "Kayıt: Bölüm N · m:ss" (saved: stage N, time).
  - Without a saved run, the big button reads OYNA and the line shows the record.

## 5. Edge Cases

- **Unreadable or tampered file:** treated as no saved run and deleted when the player presses continue.
- **Saved pilot since changed in the Hangar:** the run resumes with the saved pilot. The menu selection is not changed.
- **Weapon or passive id no longer in the data:** it is skipped. If no weapon resolves, the pilot's starting weapon stays.
- **Wingmen:** they come back in the first slots with the pilot portraits in order. The exact pilot faces may differ, but count and power are exact.

## 6. Dependencies

- RunController: saves, resumes and deletes the snapshot.
- WaveDirector: `WriteSnapshot` / `RestoreSnapshot`.
- EnemyManager.Power: `Restore`.
- WeaponInventory: `RestoreLoadout`.
- XpSystem: `Restore`.
- PlayerHealth: `RestoreHp`.
- PickupSystem: `RestoreRunGold`.
- Fleet: `Restore`.
- SaveService: `SaveRun`, `LoadRun`, `DeleteRun`, `HasRun`.
- GameUI: the DEVAM ET / Yeni Oyun buttons and the pause menu's Kaydet ve Çık.
- QaAutopilot: the resume round-trip check.

## 8. Acceptance Criteria

1. Pause, then Kaydet ve Çık: the menu shows DEVAM ET and "Kayıt: Bölüm N · m:ss".
2. DEVAM ET brings back the same level, kills, weapons with their levels, stage and HP. The QA bot logs `[QA] RESUME OK` at 100 s and keeps screenshots 30_menu_saved_run and 31_resumed.
3. After backgrounding the app and force-closing it, reopening shows DEVAM ET with state at most 15 s old.
4. Dying and force-closing on the death screen: the menu shows OYNA, with no saved run.
5. Yeni Oyun discards the saved run and starts stage 1.
