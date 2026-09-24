# Güç Eşleme, Sonsuz Mod & Ocak — the Ball Blast layer

## 1. Overview

Device feedback on 2026-09-24: *"sonuna doğru çok basitleşiyor"* (it gets too easy toward the end). The same message pointed to Ball Blast: no chapters, one endless climb, and cannon upgrades you keep buying between tries. This document takes three ideas from it:
- **Güç Eşleme (Power Match).** Enemy HP follows the player's kill speed, so a strong build never makes the late run trivial.
- **Sonsuz Mod.** A menu entry that plays the chapter, then keeps going with returning bosses until the player falls. The goal is a time record.
- **Ocak (the Forge).** Two big menu buttons, Ateş Gücü and Ateş Hızı. They level with gold between runs, and Ateş Gücü has no cap, so there is always something to buy.

## 2. Player Fantasy

"One more try — I'm stronger now." Every run pays gold, and every gold buys a visible number on the menu. The endless run is a mountain to climb, not a level to finish. The fight should feel like a fair duel at every minute: the player grows and so do the sweets they face. That keeps the late game tense rather than a victory lap.

## 3. Detailed Rules

### 3.1 Power Match
- `EnemyManager` stamps each enemy with `SeenAt` the first frame it enters the playfield (the same bounds that make it hittable).
- When a **normal** enemy dies (not elite, not boss, not during a Sugar Rush), the player's time-to-kill (`now − SeenAt`) feeds a running average (weight `AverageWeight` per kill, floored at `MinTtk`).
- For the first `CalibrationSeconds` of a run the HP scale stays 1. The average at that moment becomes the run's **baseline**. It is measured with the player's current Forge, mastery and pilot levels, so permanent upgrades still make the whole run feel stronger.
- Every frame after that, the scale moves toward the value that keeps the average time-to-kill at `baseline × TargetRatio`. The step is `PowerMatchStep`, clamped to `[1, MaxScale]`.
- Every enemy spawned after that, bosses included, gets `Formulas.EnemyHp × Scale`. The scale never goes below 1, so a struggling player is left to the existing difficulty director (DDA), never punished twice.
- The scale resets at the start of every run.

### 3.2 What is excluded from Power Match and why
- **Rush kills:** a 6 s ×1.7 fire-rate spike would push the scale up for power that goes away.
- **Elites and bosses:** their long fights would drag the average and hide a snowballing build.

### 3.3 Sonsuz Mod
- The menu's **SONSUZ** button starts the highest unlocked chapter's timeline in endless mode (`WaveDirector.StartRun(run, endlessMode: true)`).
- Killing the final boss opens the chest and continues into endless waves. The last wave phase repeats, with HP and budget still scaling with the clock and Power Match.
- In endless mode, a boss from the chapter's roster returns `endlessBossEverySeconds` after the previous one dies. Bosses come in rotation, each with the current Power Match scale.
- The run ends only on death, after revives or a give-up. It pays its gold normally (no ×1.5; that bonus stays with the "continue after victory" button).
- The time survived is compared with `bestEndlessSeconds`. A new best shows **YENİ REKOR!**. The record is shown on the SONSUZ button (`SONSUZ / Rekor m:ss`) and on the run-end screen.
- **Tekrar Oyna** repeats the mode that was played.

### 3.4 Ocak (Forge)
- **Ateş Gücü:** all weapon and wingman damage × `ForgePowerMultiplier(L)`. No cap.
- **Ateş Hızı:** all weapon and wingman cooldowns tick × `ForgeSpeedMultiplier(L)` (stacks with the rush). Capped at level `MaxForgeSpeedLevel`, where the button reads MAKS.
- Each track costs `ForgeCost(L)` gold for the next level. A button is disabled while gold is short.
- The run-end "Geliştir !" hint also lights when Ateş Gücü is affordable.

## 4. Formulas

| Name | Formula | Variables | Range | Example |
|---|---|---|---|---|
| Power step | `s' = clamp(s · e^(r · clamp(ln(T/ā), −1, 1) · dt), 1, S)` | s = scale, ā = average TTK, T = baseline × `TargetRatio` (0.8), r = `Rate` (0.05/s), S = `MaxScale` (6) | 1 – 6 | Killing in half the target time: ln 2 = 0.69, so +3.5 %/s. ≈ 20 s to double HP |
| Enemy HP | `hp = EnemyHp(base, t, c) · s` | see game-concept.md §5 | — | Minute 5, chapter 1, s = 2.5: a 10-HP chick has 10 · (1 + 1.25 + 0.875) · 2.5 ≈ 78 HP |
| Forge cost | `40 · 1.14^L`, rounded to 5 | L = current level | 40 → ∞ | L 0: 40, L 1: 45, L 10: 150, L 20: 550, L 30: 2 040 |
| Ateş Gücü | `1 + 0.05 · L` | — | ×1 → ∞ | L 20: ×2 damage |
| Ateş Hızı | `1 + 0.025 · min(L, 40)` | — | ×1 – ×2 | L 20: ×1.5 fire rate |

Rationale for the cost curve: a chapter-1 run pays about 250 gold (`ExpectedRunGold`). That buys 3–5 early levels per run, dropping to one level every few runs by level 20. There is always a next purchase in sight, which is Ball Blast's retention loop.

## 5. Edge Cases

- **Player killing nothing during calibration** (the average stays unset): the baseline is taken at the first kill after calibration, and the scale starts moving from there.
- **Kill streaks from bombs and overlaps:** each sample is floored at `MinTtk` (0.15 s), so a screen wipe cannot collapse the average.
- **Revive shockwave kills:** these count, and are bounded by the floor and the per-kill weight.
- **Endless boss alive when the timer fires:** the timer is only re-armed on the boss's death, so bosses never overlap.
- **Chapter without bosses:** endless waves run with no returning boss.
- **Ateş Hızı at level 40:** shows MAKS and cannot be bought, while Ateş Gücü stays buyable.
- **Old saves:** the new fields default to 0 (level 0, no record).

## 6. Dependencies

- **EnemyManager**: spawn HP, the `SeenAt` stamp and kill samples. **Enemy**: the `SeenAt` property.
- **WaveDirector**: resets Power Match per run, the endless mode flag, returning bosses.
- **SugarRush** (`RushActive`): excludes rush kills. sugar-rush.md's meter and combo rules are unchanged.
- **RunController**: `StartEndless`, `Forge.Configure`, `RecordEndless`, the run summary's `NewRecord`.
- **MetaProgressionService / SaveData**: Forge levels and costs, the Endless record.
- **WeaponBehaviour, Fleet**: read `Forge.DamageMultiplier` and `Forge.FireRate`.
- **GameUI**: the menu's OYNA/SONSUZ row, the Forge buttons, run-end record text.
- **QaAutopilot**: plays Sonsuz Mod for 420 s and logs `pw(ttk)` = scale (average/baseline).

## 7. Tuning Knobs

| Knob | Default | Safe range | Affects |
|---|---|---|---|
| `CalibrationSeconds` | 75 | 45–120 | How much of the run defines "the feel". The user rated the opening as good, so it is the reference |
| `TargetRatio` | 0.8 | 0.6–1.0 | How much faster than the opening the player may kill. Lower means more felt growth but an easier late game |
| `Rate` | 0.05 /s | 0.02–0.1 | How quickly HP catches up with a power spike. Too high and it feels like rubber-banding |
| `MaxScale` | 6 | 3–10 | Ceiling on the adaptive HP |
| `AverageWeight` | 0.04 | 0.02–0.1 | Memory of the average, about 25 kills |
| `endlessBossEverySeconds` | 120 | 90–180 | Boss rhythm in Sonsuz Mod |
| Forge base / growth | 40 / 1.14 | 30–60 / 1.10–1.20 | Purchase rhythm between runs |
| `ForgePowerPerLevel` / `ForgeSpeedPerLevel` | 0.05 / 0.025 | 0.03–0.08 / 0.015–0.04 | Size of each Forge level |

## 8. Acceptance Criteria

1. Unit tests cover these cases:
   - Power Match holds the scale at 1 during calibration.
   - It rises above 2 when kills get 5× faster than the baseline.
   - It stays at 1 when the player is slower.
   - Reset clears it.
   - `ForgeCost(0/1/10)` = 40/45/150.
2. QA telemetry (`pw` column) shows scale 1.00 until about 75 s. After that the scale rises while the build snowballs. At no 10 s row after minute 4 do both of these hold: "HP full and zero enemies on screen".
3. Menu:
   - OYNA and SONSUZ sit side by side. The two Forge buttons show level and cost, and are disabled when gold is short.
   - Buying a level deducts the cost, raises the level by one and updates the wallet at once.
4. A Sonsuz Mod run continues after the final boss, with a boss banner again about 2 min later. It ends only on death. When it beats the stored time, the run-end title reads YENİ REKOR! and the SONSUZ button shows the new record.
5. English: "ATEŞ GÜCÜ\nSv.3  ·  60 altın" renders as "FIREPOWER\nLv.3  ·  60 gold" (LocTests).
