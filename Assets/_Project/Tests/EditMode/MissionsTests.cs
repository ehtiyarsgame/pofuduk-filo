using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class MissionsTests
    {
        [Test]
        public void test_missions_same_day_same_missions_and_distinct_kinds()
        {
            MissionDef[] a = Missions.ForDay(20356), b = Missions.ForDay(20356);
            Assert.That(a.Length, Is.EqualTo(Missions.PerDay));
            for (int i = 0; i < a.Length; i++)
            {
                Assert.That(a[i].Kind, Is.EqualTo(b[i].Kind));
                Assert.That(a[i].Target, Is.EqualTo(b[i].Target));
                for (int j = i + 1; j < a.Length; j++) Assert.That(a[i].Kind, Is.Not.EqualTo(a[j].Kind));
                Assert.That(a[i].RewardGold, Is.GreaterThan(0));
            }
        }

        [Test]
        public void test_missions_change_between_days()
        {
            bool anyDifferent = false;
            MissionDef[] first = Missions.ForDay(100);
            for (long d = 101; d < 110 && !anyDifferent; d++)
            {
                MissionDef[] other = Missions.ForDay(d);
                for (int i = 0; i < Missions.PerDay; i++)
                    if (other[i].Kind != first[i].Kind || other[i].Target != first[i].Target) anyDifferent = true;
            }
            Assert.That(anyDifferent, Is.True);
        }

        [TestCase(3, 10, 11, 4)]
        [TestCase(3, 10, 10, 3)]
        [TestCase(3, 10, 13, 1)]
        [TestCase(0, -1, 5, 1)]
        public void test_login_streak(int streak, long last, long today, int expected)
        {
            Assert.That(Missions.NextStreak(streak, last, today), Is.EqualTo(expected));
        }

        [Test]
        public void test_streak_reward_week_cycle()
        {
            Assert.That(Missions.StreakReward(1), Is.EqualTo((100, 0)));
            Assert.That(Missions.StreakReward(7), Is.EqualTo((1000, 5)));
            Assert.That(Missions.StreakReward(8), Is.EqualTo((100, 0)));
        }
    }
}
