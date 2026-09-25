# In-Run HUD

> Status: implemented 2026-09-25 · Related: `screen-fit.md`, `design/gdd/sugar-rush.md`

## 1. Layout

Everything the player reads during play sits in one **top panel**, out of the thumb zone.

- **Row 1:** HP bar with a heart and "85/130", the gold count, and the pause button.
- **Row 2:** the XP bar with "Lv. N" on the left, and the Sugar meter (ŞEKER / ŞEKER HAZIR! / HÜCUM!) on the right.

Just below the panel, and only while a boss is alive, sit the boss HP bar and the combo counter. The Sugar Bomb
button sits in the bottom-left corner.

## 2. The panel hides what cannot be hit

Device feedback on 2026-09-25: *"düşman en üstten gelirken dokunmaz oluyor… can barlarının olduğu yerin
üstünden gözüküyor… müşteriyi yanıltabilir"* ("enemies coming in at the very top are untouchable… they show
through where the health bars are… it can mislead the player"). Enemies were visible through the old
see-through plate, but they could not be hit there.

The rule is now: **what you can see, you can hit.**

- The panel is opaque (plum `#221733`) and runs edge to edge. It extends up through any notch or cut-out.
- A lilac rim runs along its bottom edge, with a soft shadow beneath it. Enemies read as flying out from under a
  dashboard.
- `GameUI` measures the panel's bottom edge every frame and writes it to `Playfield.TopInset`, as a fraction of
  the camera height. Three systems take their top edge from `Playfield.TopY`, which is derived from that value:
  - `EnemyManager.IsOnScreen`, which decides whether an enemy is hittable and allowed to shoot.
  - The Yarn Ball's ceiling.
  - Anything else that needs the top of the fight.

  So the line stays correct on any phone, tablet or notch.
- An enemy becomes hittable when its centre is 0.15 world units below the panel edge, which is about half of its
  body.

## 3. Acceptance

- In QA screenshots, no enemy body is visible above the panel's rim.
- Enemies just below the rim take damage. The QA run's kills keep rising, and no formation hangs untouched.
