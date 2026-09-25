# Retention: Daily Missions, Login Streak and Next Goals

> Status: implemented 2026-09-25 · Related: `economy.md`, `ad-rewards.md`

## 1. Overview

This system gives the player a reason to come back every day, and a visible next goal after every run. It was
agreed in the 2026-09-25 design discussion.

## 3. Detailed Rules

### Daily missions

- There are 3 missions a day, all of different kinds, chosen from the UTC day number with
  `Missions.ForDay`. Every device therefore gets the same set.
- Mission kinds:
  - defeat N enemies
  - survive N minutes in a single run
  - earn N gold
  - defeat N bosses
  - trigger N Sugar Rushes
  - play N runs
- Each finished run adds its numbers (`RecordRunForMissions`). The minutes mission counts the best single run of
  the day; every other kind adds up.
- Missions reward 150–450 gold, plus 0–4 Stardust on some.
- Progress resets at UTC midnight.

### Login streak

- **Checking in:** reaching the menu checks the player in (`CheckIn`).
- **Counting:** a consecutive day adds 1 to the streak; a missed day resets it to 1.
- **Rewards:** one reward per day, starting at 100 gold on day 1 and rising to 1000 gold + 5 Stardust on day 7.
  The week then repeats.

### Where it shows

- **Menu:** a trophy button labelled **GÖREVLER**, below the gift button. It shows a "!" badge while something
  can be claimed.
- **Run-end screen:** alongside the cheapest workshop upgrade, it now shows **"Sonraki pilot: X %NN"** — the
  cheapest pilot the player can unlock, and how close their gold is.
- **PİLOTLAR tab:** it shows a "!" when a pilot is affordable.
- **First pilot:** Cıvık costs 1 500 gold and no Stardust, which is about 4 runs. The first unlock comes early;
  later pilots stay expensive.

## 8. Acceptance Criteria

The following tests must pass:

- `test_missions_same_day_same_missions_and_distinct_kinds`
- `test_missions_change_between_days`
- `test_login_streak`
- `test_streak_reward_week_cycle`

QA screenshot `05_missions` shows the streak row and 3 mission rows, with no overlaps.
