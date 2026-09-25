# Rewarded Ads: Unlocks, Trials and the Daily Gift

> Status: implemented 2026-09-24 · Owner: economy · Related: `meta-economy.md` §3.5 (ad placements),
> `weapon-system.md` §3.2 (weapons 6–7), `run-resume.md`

> **Revised 2026-09-25 (device feedback):** "karakter açmak 3-5 video ile olmaz… para için sürekli izlesinler"
> — pilots and weapons are **no longer unlockable with ads** (`adsToUnlock` = 0 everywhere; the progress UI is
> gone) and their prices went up (pilots 2 500–22 000 gold + 10–80 Stardust, Lab weapons 1 200–5 000). Ads now
> pay currency: the gift is claimable **10× a day, 5 min apart**, from the menu box and from a "📺 +" button beside
> every meta screen's wallet. **Dene** (one ad → one trial run) stays. §3.1 below is kept for history only.

## 1. Overview

Outside the run, the game offers three opt-in rewarded-ad placements:

- **Unlock by ads.** Every locked pilot and every locked Lab weapon can be bought with gold *or* earned with
  a few rewarded ads. Progress is kept, e.g. "📺 2/5".
- **Dene (Try).** One ad buys one run with a locked pilot or weapon. That ad also counts towards unlocking it.
- **Daily gift.** On the main menu, one ad pays gold plus 2 Stardust. This can be claimed 3 times a day,
  20 minutes apart.

The goal is steady ad revenue from players who choose it, without turning ads into a toll: gold always
remains a full alternative, and nothing is ever forced.

Two new weapons and two new pilots give the ads something worth watching for:

| Content | Unlock | Starts with / evolves with |
|---|---|---|
| Balık Füzesi (Fish Missile) → Köpekbalığı Sürüsü | 1 200 gold **or** 3 ads | evolution key: Mıknatıs Kulak |
| Yün Yumağı (Yarn Ball) → Kozmik Yumak | 1 600 gold **or** 4 ads | evolution key: Havuç Kalkan |
| Pengu (penguin) | 4 000 gold + 25 Stardust **or** 5 ads | Fish Missile; shots +25 % speed, +5 % crit |
| Kuzu (lamb) | 6 000 gold + 40 Stardust **or** 6 ads | Yarn Ball; +30 % HP, +10 % area |

## 2. Player Fantasy

"I can get the new penguin today without grinding. I'll watch a couple of ads while I'm on the bus."

The try-before-you-buy run is the hook. You fly Pengu once and fall for the homing fish. The ad you
watched already counts as 1 of the 5, so finishing the unlock feels close.

## 3. Detailed Rules

### 3.1 Unlock by ads

- Each unlock item's ad cost is set in its `adsToUnlock` field (`WeaponDefinition` / `CharacterDefinition`).
  A value of 0 means gold only.
  - Pilots: Cıvık 2, Mırnav 3, Balonbaş 4, Pengu 5, Yıldızpati 6, Kuzu 6.
  - Weapons: Yıldız Bumerang 2, Sakız Balonu 3, Balık Füzesi 3, Yün Yumağı 4.
- The secret pilot (stage-gated) and the Lab passives are gold only.
- Each watched ad adds +1 to that item's progress (`SaveData.adProgress`).
  - When progress reaches the ad cost, the item unlocks.
  - A pilot is also selected on unlock.
- Unlock views and trials share a limit of **8 per UTC day** (`AdRules.UnlockViewsPerDay`).
  - When the day's views are used up, the button reads *Yarın* ("Tomorrow") and is disabled.
- Ad progress and gold are independent. Buying with gold never refunds ads, and ads never refund gold.

### 3.2 Dene (trial)

- A **Dene** button appears on every item that can be unlocked with ads. It is disabled when the day's
  views are used up.
- After the ad:
  - +1 is added to the item's progress (counted against the daily limit).
  - A new endless run starts with the pilot (`RunController.StartTrial`), or with the weapon added next to
    the pilot's own.
- If a run is saved (`run-resume.md`), Dene is refused with the message *"Önce kayıtlı oyununa devam et."*
  ("Finish your saved run first."). This is because a new run would overwrite the saved one.
- A trial run pays out normally, and resume works during it: the snapshot stores the pilot and loadout.

### 3.3 Daily gift

- The gift can be claimed 3 times per UTC day, at least 20 minutes apart (`AdRules`).
- The button shows *HEDİYE* ("GIFT") when ready, a `m:ss` countdown while waiting, and *Yarın* when the
  day's gifts are used up.
- When ready, it gives a short wiggle every few seconds. It never shows a pop-up or a red badge.

### 3.4 What we never do

- We never force an ad: there are no timed pop-ups and no ads during play.
- We never cut a player off mid-run to show an ad.
- We never hide the gold price.
- We never show an ad without saying first what it pays.
- An ad that is cut short pays nothing, and the game says so: *"Reklam yarıda kaldı, ödül verilmedi."*
  ("Ad was skipped, no reward.").
- Existing interstitial pacing (`AdPacing`) is unchanged, so a rewarded ad in a run still suppresses the
  interstitial after it.

## 4. Formulas

- **Gift gold** = `max(100, ForgeCost(⌊(forgePower + forgeSpeed) / 2⌋))` (`Formulas.GiftGold`).
  - This is roughly one Forge level at the player's current depth, so the gift keeps its value as the
    player's economy grows.
  - Examples: 100 gold at the start, 150 gold at Forge 10/10, 550 gold at Forge 20/20.
- **Gift Stardust** = 2 (`AdRules.GiftStardust`).
- **Daily ad ceiling per player** = 8 unlock/trial views + 3 gifts + the existing run-end ads
  (double gold, revive).
  - Fastest unlock of the dearest pilot (6 ads) is therefore one day.
  - Unlocking all the ad content (38 ads) takes at least 5 days of play.

## 5. Edge Cases

- **Clock set back:** if the clock is set back, the gift's cooldown reads as ready, so the player is not
  locked out. The day counter follows the new (earlier) day, which at worst gives one extra day's
  allowance.
- **No ad available:** if no ad is available (offline, no fill), the game says *"Reklam yükleniyor,
  birazdan tekrar dene."* ("Ad is loading, try again in a moment.") and nothing is counted.
- **Item already bought:** if the item was bought with gold while ad progress existed, the ad controls
  disappear. The progress stays in the save but is unused.
- **Old saves:** saves from before this feature load `adProgress` as null, which is repaired in the
  `MetaProgressionService` constructor. The counters default to 0.

## 6. Dependencies

- `RewardedAds` provider: AdMob on Android, instant grant in the editor and QA.
- `MetaProgressionService` handles the unlock lists and wallet.
- `RunController.StartTrial` is used for trials.
- `SaveService` stores the new fields.
- `GameUI.Rewards`.
- `ArtRecipes` provides the new art: `FishSprite`, `YarnSprite`, `PenguinPilot`, `LambPilot`, `IconAd`
  and `GiftBox`.

## 7. Tuning Knobs

| Knob | Where | Default |
|---|---|---|
| `adsToUnlock` per item | `ContentBuilder` | see §3.1 |
| Unlock/trial views per day | `AdRules.UnlockViewsPerDay` | 8 |
| Gifts per day / cooldown | `AdRules.GiftsPerDay` / `GiftCooldown` | 3 / 20 min |
| Gift Stardust | `AdRules.GiftStardust` | 2 |
| Gift gold floor | `Formulas.GiftGold` | 100 |

## 8. Acceptance Criteria

- An item with `adsToUnlock = N` unlocks on exactly the Nth recorded view, and no view is accepted after
  that (`test_ad_unlock_completes_after_the_required_views`).
- A 9th view in one UTC day is refused, and the allowance returns the next day
  (`test_ad_views_capped_per_day_and_reset_next_day`).
- The gift pays `GiftGold` plus 2 Stardust, then refuses until 20 minutes pass, and refuses after 3 in a
  day (`test_gift_pays_then_waits_for_cooldown_and_daily_limit`).
- A clock set back never locks the gift (`test_ad_rules_clock_set_back_does_not_lock_the_gift`).
- In QA, the autopilot plays its run as a Pengu + Yarn Ball trial. Telemetry must show `fish_missile` and
  `yarn_ball` levelling, kills rising, and `[QA] RESUME OK`.
- In the QA screenshots, the Pilots and Weapons screens show the gold price, the 📺 progress button and
  **Dene** on locked items, and the menu shows the gift button.

## 9. Store policy notes

- Rewarded ads are labelled as ads and are opt-in, which is within AdMob's rewarded-ad policy.
- If the game is ever listed under Google Play's **Families** programme (for example, a target audience
  that includes children), the ad SDK must be configured for child-directed treatment. That limits ad
  formats and personalisation, and needs a review of these placements.
