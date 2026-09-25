using NUnit.Framework;
using PofudukFilo.Enemies;

namespace PofudukFilo.Tests
{
    public sealed class SwarmPickerTests
    {
        // Chick 10, Bee 5, Marshmallow 2 (cost is not part of the choice any more).
        private static readonly float[] Weights = { 10f, 5f, 2f };

        [TestCase(0.0f, 0)]
        [TestCase(0.5f, 0)]
        [TestCase(0.6f, 1)]
        [TestCase(0.95f, 2)]
        [TestCase(0.9999f, 2)]
        public void test_swarm_picker_every_weighted_type_can_be_chosen(float roll, int expected)
        {
            Assert.That(SwarmPicker.PickWeighted(Weights, roll), Is.EqualTo(expected));
        }

        [Test]
        public void test_swarm_picker_skips_zero_weights_and_empty()
        {
            Assert.That(SwarmPicker.PickWeighted(new[] { 0f, 3f }, 0f), Is.EqualTo(1));
            Assert.That(SwarmPicker.PickWeighted(new[] { 0f, 0f }, 0.5f), Is.EqualTo(-1));
        }
    }
}
