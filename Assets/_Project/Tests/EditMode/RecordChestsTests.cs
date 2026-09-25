using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class RecordChestsTests
    {
        [Test]
        public void test_record_chest_claimable_once_after_reaching_mark()
        {
            Assert.That(RecordChests.CanClaim(0, 89f, 0), Is.False);
            Assert.That(RecordChests.CanClaim(0, 90f, 0), Is.True);
            Assert.That(RecordChests.CanClaim(0, 500f, 1), Is.False); // already claimed (bit 0)
            Assert.That(RecordChests.CanClaim(1, 500f, 1), Is.True);
        }

        [Test]
        public void test_record_chest_progress_steps_evenly()
        {
            Assert.That(RecordChests.Progress(0f), Is.EqualTo(0f));
            Assert.That(RecordChests.Progress(90f), Is.EqualTo(0.2f).Within(1e-5f));
            Assert.That(RecordChests.Progress(135f), Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(RecordChests.Progress(9999f), Is.EqualTo(1f));
        }

        [Test]
        public void test_record_chest_next_goal()
        {
            Assert.That(RecordChests.NextGoal(0f), Is.EqualTo(0));
            Assert.That(RecordChests.NextGoal(200f), Is.EqualTo(2));
            Assert.That(RecordChests.NextGoal(600f), Is.EqualTo(-1));
        }
    }
}
