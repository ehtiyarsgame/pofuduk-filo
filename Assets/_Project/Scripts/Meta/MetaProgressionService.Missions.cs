using System;
using System.Collections.Generic;

namespace PofudukFilo.Meta
{
    /// <summary>What one finished run contributes to the daily missions.</summary>
    public readonly struct RunStats
    {
        public readonly int Kills, Gold, Bosses, Rushes;
        public readonly float Minutes;

        public RunStats(int kills, float minutes, int gold, int bosses, int rushes)
        {
            Kills = kills;
            Minutes = minutes;
            Gold = gold;
            Bosses = bosses;
            Rushes = rushes;
        }
    }

    /// <summary>Daily missions and the login streak on the save (retention.md).</summary>
    public sealed partial class MetaProgressionService
    {
        private static long Day(long utcTicks) => utcTicks / TimeSpan.TicksPerDay;

        /// <summary>Today's three missions; rolls progress over at UTC midnight.</summary>
        public MissionDef[] TodaysMissions(long nowTicks)
        {
            long day = Day(nowTicks);
            if (_data.missionDay != day)
            {
                _data.missionDay = day;
                _data.missionProgress = new List<int>(new int[Missions.PerDay]);
                _data.missionClaimed = new List<int>(new int[Missions.PerDay]);
            }
            while (_data.missionProgress.Count < Missions.PerDay) _data.missionProgress.Add(0);
            while (_data.missionClaimed.Count < Missions.PerDay) _data.missionClaimed.Add(0);
            return Missions.ForDay(day);
        }

        public int MissionProgress(int index) =>
            index >= 0 && index < _data.missionProgress.Count ? _data.missionProgress[index] : 0;

        public bool MissionClaimed(int index) =>
            index >= 0 && index < _data.missionClaimed.Count && _data.missionClaimed[index] != 0;

        public bool CanClaimMission(int index, long nowTicks)
        {
            MissionDef[] m = TodaysMissions(nowTicks);
            return index >= 0 && index < m.Length && !MissionClaimed(index) && MissionProgress(index) >= m[index].Target;
        }

        public bool AnyMissionClaimable(long nowTicks)
        {
            for (int i = 0; i < Missions.PerDay; i++)
                if (CanClaimMission(i, nowTicks)) return true;
            return CanClaimStreak(nowTicks);
        }

        public bool ClaimMission(int index, long nowTicks)
        {
            if (!CanClaimMission(index, nowTicks)) return false;
            MissionDef m = TodaysMissions(nowTicks)[index];
            _data.missionClaimed[index] = 1;
            _data.gold += m.RewardGold;
            _data.stardust += m.RewardDust;
            Commit();
            return true;
        }

        /// <summary>Adds a finished run to today's missions.</summary>
        public void RecordRunForMissions(in RunStats run, long nowTicks)
        {
            MissionDef[] m = TodaysMissions(nowTicks);
            for (int i = 0; i < m.Length; i++)
            {
                int add = m[i].Kind switch
                {
                    MissionKind.Kills => run.Kills,
                    MissionKind.Gold => run.Gold,
                    MissionKind.Bosses => run.Bosses,
                    MissionKind.Rushes => run.Rushes,
                    MissionKind.Runs => 1,
                    _ => 0
                };
                int value = m[i].IsBestOfRun
                    ? Math.Max(_data.missionProgress[i], (int)Math.Floor(run.Minutes))
                    : _data.missionProgress[i] + add;
                _data.missionProgress[i] = Math.Min(value, m[i].Target);
            }
            Commit();
        }

        // ---------------------------------------------------------------- Login streak

        /// <summary>Call on reaching the menu: advances the streak on a new day.</summary>
        public int CheckIn(long nowTicks)
        {
            long today = Day(nowTicks);
            if (_data.streakLastDay != today)
            {
                _data.streak = Missions.NextStreak(_data.streak, _data.streakLastDay, today);
                _data.streakLastDay = today;
                Commit();
            }
            return _data.streak;
        }

        public int Streak => Math.Max(1, _data.streak);

        public bool CanClaimStreak(long nowTicks) => _data.streakLastDay == Day(nowTicks) && _data.streakClaimedDay != Day(nowTicks);

        public bool ClaimStreak(long nowTicks)
        {
            if (!CanClaimStreak(nowTicks)) return false;
            (int gold, int dust) = Missions.StreakReward(_data.streak);
            _data.gold += gold;
            _data.stardust += dust;
            _data.streakClaimedDay = Day(nowTicks);
            Commit();
            return true;
        }
    }
}
