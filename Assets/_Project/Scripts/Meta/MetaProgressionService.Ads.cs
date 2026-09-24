using System;
using PofudukFilo.Weapons;

namespace PofudukFilo.Meta
{
    /// <summary>Rewarded-ad progress on the save: unlock-by-ads, trials and the daily gift.</summary>
    public sealed partial class MetaProgressionService
    {
        /// <summary>Resets the daily counters when the UTC day changed.</summary>
        private void RollAdDay(long nowTicks)
        {
            long day = AdRules.DayOf(nowTicks);
            if (_data.adDay == day) return;
            _data.adDay = day;
            _data.adViewsToday = 0;
            _data.giftsToday = 0;
        }

        public int AdProgress(string id) => FindIn(_data.adProgress, id)?.level ?? 0;

        public int AdViewsLeftToday(long nowTicks)
        {
            RollAdDay(nowTicks);
            return Math.Max(0, AdRules.UnlockViewsPerDay - _data.adViewsToday);
        }

        public bool CanWatchForUnlock(CharacterDefinition c, long nowTicks) =>
            c.adsToUnlock > 0 && !IsUnlocked(c) && AdViewsLeftToday(nowTicks) > 0;

        public bool CanWatchForUnlock(WeaponDefinition w, long nowTicks) =>
            w.adsToUnlock > 0 && !IsUnlocked(w) && AdViewsLeftToday(nowTicks) > 0;

        /// <summary>One watched ad towards a pilot. True when this view completed the unlock.</summary>
        public bool RecordAdView(CharacterDefinition c, long nowTicks)
        {
            if (!CanWatchForUnlock(c, nowTicks)) return false;
            bool done = Advance(c.id, c.adsToUnlock);
            if (done) _data.unlockedCharacters.Add(c.id);
            Commit();
            return done;
        }

        /// <summary>One watched ad towards a weapon. True when this view completed the unlock.</summary>
        public bool RecordAdView(WeaponDefinition w, long nowTicks)
        {
            if (!CanWatchForUnlock(w, nowTicks)) return false;
            bool done = Advance(w.id, w.adsToUnlock);
            if (done) _data.unlockedWeapons.Add(w.id);
            Commit();
            return done;
        }

        private bool Advance(string id, int needed)
        {
            _data.adViewsToday++;
            int progress = AdProgress(id) + 1;
            SetIn(_data.adProgress, id, progress);
            return progress >= needed;
        }

        // ---------------------------------------------------------------- Daily gift

        public int GiftsLeftToday(long nowTicks)
        {
            RollAdDay(nowTicks);
            return Math.Max(0, AdRules.GiftsPerDay - _data.giftsToday);
        }

        public TimeSpan GiftWait(long nowTicks) => AdRules.GiftWait(_data.lastGiftUtcTicks, nowTicks);

        public bool CanClaimGift(long nowTicks) => GiftsLeftToday(nowTicks) > 0 && GiftWait(nowTicks) == TimeSpan.Zero;

        public int GiftGold => Core.Formulas.GiftGold(_data.forgePower, _data.forgeSpeed);

        /// <summary>Pays the gift after its ad; returns the gold granted (0 if not claimable).</summary>
        public int ClaimGift(long nowTicks)
        {
            if (!CanClaimGift(nowTicks)) return 0;
            int gold = GiftGold;
            _data.giftsToday++;
            _data.lastGiftUtcTicks = nowTicks;
            _data.gold += gold;
            _data.stardust += AdRules.GiftStardust;
            Commit();
            return gold;
        }
    }
}
