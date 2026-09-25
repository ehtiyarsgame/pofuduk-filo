# Main Menu (Lobby)

> Status: v4 implemented 2026-09-25 · Owner: UI · Code: `UI/GameUI.MainMenu.cs`, `Meta/RecordChests.cs`
> Related: `design/gdd/ad-rewards.md` (gift, "+"), `design/gdd/retention.md` (missions, streak)

## 1. Purpose

The lobby has one job: make the player press **OYNA** (PLAY), and give them a reason to come back tomorrow.

## 2. History

| Version | Feedback that drove it | Change |
|---|---|---|
| v1 | — | Flat buttons in a column |
| v2 | "çok basit, profesyonel değil" (too plain, unprofessional) | Candy logo, rays, pedestal, glossy PLAY |
| v3 | "menü karmaşık" (menu cluttered) | Everything docked into a top bar and a 4-tab bottom bar |
| v4 | "piyasadaki oyunlara bak… çok basit, oynayası gelmiyor" (look at games on the market; so plain it doesn't make you want to play) | Market-style lobby, §3 |

## 3. v4 layout (market research)

### 3.1 Research findings

Top-grossing survivor-likes and shooters (Habby's Archero / Survivor.io / Capybara Go and their followers)
share one lobby pattern:

1. **An illustrated stage or chapter card** is the centrepiece. It sells the run, not the logo.
2. **A chest/milestone track** under the card always shows the next reward for going further. This is the
   "one more try" hook.
3. **Side event icons** sit left and right of the card. Each has a badge or timer, so something is always
   ready or counting down.
4. **A profile avatar with a power number** sits top-left, with **currencies with a "+"** next to it.
5. **A 5-tab bottom bar** with a raised centre **Home** tab.
6. **One huge, pulsing, warm-coloured PLAY button.**

Sources:
- https://naavik.co/deep-dives/survivorio-archeros-footsteps/
- https://mobilegamer.biz/the-design-secrets-powering-habbys-hit-machine/
- https://www.gameuidatabase.com/index.php?scrn=43
- https://www.designstudiouiux.com/blog/mobile-navigation-ux/

### 3.2 Layout

The layout is given in fractions of the safe area (x left→right, y bottom→top).

| Element | Rect | Tap |
|---|---|---|
| Pilot avatar (current pilot portrait) | x 0.02–0.15, y 0.935–0.99 | Pilotlar (Pilots) |
| Power tag "GÜÇ ×1.23" (POWER) | x 0–0.2, y 0.905–0.932 | — |
| Gold / Stardust capsules | x 0.21–0.48 / 0.52–0.72 | — |
| "📺 +" (ad for currency = gift) | x 0.735–0.845 | watch ad → gift |
| Gear | x 0.86–0.98 | Settings |
| Logo GALAXY PAWS (smaller than v3) | y 0.818–0.91 | — |
| Stage card "SONSUZ GALAKSİ" (Endless Galaxy) with hero ship, pedestal and rays | x 0.17–0.83, y 0.54–0.8 | — |
| Best time / saved-run chip | inside the card foot, y 0.548–0.58 | — |
| Left icons: HEDİYE (Gift: timer / Yarın) and YILDIZLAR (Stars) | x 0.02–0.15, y 0.69 / 0.585 | gift ad / Constellation |
| Right icons: next pilot (portrait, gold %) and streak GÜN n/7 (DAY n/7) | x 0.85–0.98, y 0.69 / 0.585 | Pilots / Missions |
| Chest track: hint + 5 chests + times | y 0.419–0.528 | claim / explain |
| OYNA / DEVAM ET (PLAY / CONTINUE), gold | x 0.1–0.9, y 0.28–0.39 | start / resume |
| Yeni Oyun (New Game, only with a saved run) | y 0.222–0.262 | new run |
| Bottom bar: AR-GE · SİLAHLAR · **ANA SAYFA** (raised) · PİLOTLAR · GÖREVLER (R&D · Weapons · **Home** · Pilots · Missions) | y 0–0.165 | screens |

### 3.3 Record chests

- There are five one-time chests for the best endless time:

  | Best time | Gold | Stardust |
  |---|---|---|
  | 1:30 | 200 | 0 |
  | 3:00 | 400 | 2 |
  | 5:00 | 700 | 4 |
  | 7:00 | 1 000 | 6 |
  | 10:00 | 1 600 | 10 |

- The track fills piecewise, so each chest sits at an even step (`RecordChests.Progress`).
- Chest states:
  - **Locked:** greyed out. A tap says which time opens it.
  - **Ready:** full colour, wiggles and pulses. A tap claims it.
  - **Claimed:** shows the open sprite, dimmed.
- The hint line reads:
  - *"Sandık hazır — dokun, aç!"* ("Chest ready — tap to open!") when a chest is ready.
  - Otherwise *"Sonraki sandık: m:ss hayatta kal"* ("Next chest: survive m:ss").
- Chests are claimed by hand, never automatically. The tap is the reward moment.
- Total payout is 3 900 gold and 22 Stardust. That is under a quarter of one mid pilot (`economy.md`), so
  the chests guide the player without flooding the economy.

## 4. Edge Cases

- **Old saves** have `recordChestsClaimed = 0`. A veteran with a 12:00 best finds all five chests ready.
  This is intended: it is a welcome-back gift.
- **All pilots owned:** the next-pilot icon and its tag are hidden.
- **Saved run:** the chip shows "Kayıt: Bölüm n · m:ss" (Saved: Stage n · m:ss), PLAY reads DEVAM ET
  (CONTINUE), and Yeni Oyun (New Game) appears.

## 5. Acceptance Criteria

- `RecordChestsTests` pass: claim order, progress steps and the next goal.
- QA screenshots `00_menu` and `30_menu_saved_run` show no overlapping text or buttons at 1080×2400.
