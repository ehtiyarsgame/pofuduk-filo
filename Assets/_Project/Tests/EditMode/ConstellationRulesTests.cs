using System;
using System.Collections.Generic;
using NUnit.Framework;
using PofudukFilo.Meta;

namespace PofudukFilo.Tests
{
    public sealed class ConstellationRulesTests
    {
        private static readonly string[] NoRequirements = Array.Empty<string>();

        [Test]
        public void CanBuy_RequiresPrerequisites()
        {
            var owned = new HashSet<string>();
            string[] requires = { "atk_1" };

            Assert.That(ConstellationRules.CanBuy("atk_2", 2, requires, owned, stardust: 10), Is.False);
            owned.Add("atk_1");
            Assert.That(ConstellationRules.CanBuy("atk_2", 2, requires, owned, stardust: 10), Is.True);
        }

        [Test]
        public void CanBuy_RejectsOwnedOrUnaffordable()
        {
            var owned = new HashSet<string> { "atk_1" };

            Assert.That(ConstellationRules.CanBuy("atk_1", 2, NoRequirements, owned, stardust: 10), Is.False);
            Assert.That(ConstellationRules.CanBuy("atk_3", 5, NoRequirements, owned, stardust: 4), Is.False);
            Assert.That(ConstellationRules.CanBuy("atk_3", 5, NoRequirements, owned, stardust: 5), Is.True);
        }

        [Test]
        public void CanRespec_OncePer24Hours()
        {
            long day = TimeSpan.TicksPerDay;
            long last = 10 * day;

            Assert.That(ConstellationRules.CanRespec(0, last), Is.True, "never respecced");
            Assert.That(ConstellationRules.CanRespec(last, last + day - 1), Is.False);
            Assert.That(ConstellationRules.CanRespec(last, last + day), Is.True);
        }
    }
}
