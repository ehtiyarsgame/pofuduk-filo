# First-Run Coach Marks

> Status: implemented 2026-09-26 · Code: `UI/GameUI.Tutorial.cs`, `GameSettings.TutorialDone`

## 1. Purpose

A new player learns the three things the game never says on its own: how to steer, why a leak hurts, and that
the Sugar Bomb is a button. Each lesson is one line of text above the ship during the first run. There are no
modal popups and the game never pauses for them.

## 2. Steps

| # | Shown | Text (TR / EN) | Finishes when |
|---|---|---|---|
| 1 | Run start | "Parmağını ekranda sürükle — gemi seni takip eder" / "Drag your finger — the ship follows it" | 1.5 s of finger-down time |
| 2 | Straight after step 1 | "Aşağıdan kaçan canavarlar can yakar — hepsini vur!" / "Monsters that slip past the bottom hurt you — shoot them all!" | 5 s later |
| 3 | First full sugar meter | "Şeker Bombası hazır! Sol alttaki ŞEKER! tuşuna bas" / "Sugar Bomb ready! Tap SUGAR! at the bottom left" | The bomb is used |

- **Position:** the text sits at y 0.32–0.40 of the safe area, above the ship and below the swarm. It breathes
  (alpha 0.75–1) so it reads as a hint rather than a HUD label.
- **Hidden** over cards, pause, death and results.

## 3. Edge Cases

- **Run ends early:**
  - Ended before step 3 was reached: the next run starts again from step 1.
  - Ended during step 3: the device counts as taught and `TutorialDone` is set. The bomb hint is a bonus.
- **Storage:** the setting is saved per device in PlayerPrefs (`pf.tutorial`). A reinstall shows the hints again.
- **QA autopilot:** it never touches the screen, so step 1 stays up for the whole run. This is expected in the
  QA screenshots.

## 4. Acceptance Criteria

- On a fresh install, the step 1 text is visible in QA screenshot `10_t004`, above the ship, with no overlap.
- On device, the three hints appear in order during the first run, and none appear on the second run.
