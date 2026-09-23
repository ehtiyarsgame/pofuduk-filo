using System.Collections.Generic;
using NUnit.Framework;
using PofudukFilo.Progression;
using Random = Unity.Mathematics.Random;

namespace PofudukFilo.Tests
{
    public sealed class WeightedPickerTests
    {
        [Test]
        public void PickDistinct_NeverReturnsDuplicates()
        {
            var random = new Random(1234);
            float[] weights = { 60, 28, 10, 2, 60, 28 };
            int[] results = new int[3];

            for (int run = 0; run < 1000; run++)
            {
                int count = WeightedPicker.PickDistinct(weights, results, ref random);
                Assert.That(count, Is.EqualTo(3));
                Assert.That(new HashSet<int>(results).Count, Is.EqualTo(3));
            }
        }

        [Test]
        public void PickDistinct_SkipsZeroWeights_AndReturnsFewerWhenShort()
        {
            var random = new Random(99);
            float[] weights = { 0, 5, 0 };
            int[] results = new int[3];

            int count = WeightedPicker.PickDistinct(weights, results, ref random);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(1));
        }

        [Test]
        public void PickDistinct_FollowsWeights()
        {
            var random = new Random(7);
            float[] weights = { 90, 10 };
            int[] result = new int[1];
            int firstPicked = 0;

            for (int i = 0; i < 10000; i++)
            {
                WeightedPicker.PickDistinct(weights, result, ref random);
                if (result[0] == 0) firstPicked++;
            }

            Assert.That(firstPicked / 10000f, Is.EqualTo(0.9f).Within(0.02f));
        }
    }
}
