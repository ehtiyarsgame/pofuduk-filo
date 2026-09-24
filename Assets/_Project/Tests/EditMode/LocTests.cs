using NUnit.Framework;
using PofudukFilo.Core;

namespace PofudukFilo.Tests
{
    public sealed class LocTests
    {
        [Test]
        public void test_exact_entry_translates()
        {
            Assert.That(Loc.ToEnglish("OYNA"), Is.EqualTo("PLAY"));
        }

        [Test]
        public void test_template_with_number_translates()
        {
            Assert.That(Loc.ToEnglish("Seviye 12!"), Is.EqualTo("Level 12!"));
        }

        [Test]
        public void test_template_capture_is_translated_recursively()
        {
            Assert.That(Loc.ToEnglish("EVRİM! Tüy Blaster"), Is.EqualTo("EVOLVED! Feather Blaster"));
            Assert.That(Loc.ToEnglish("+%6 kritik şansı"), Is.EqualTo("+6% crit chance"));
        }

        [Test]
        public void test_more_specific_template_wins()
        {
            Assert.That(Loc.ToEnglish("Bölüm 2  (kilitli)"), Is.EqualTo("Chapter 2  (locked)"));
            Assert.That(Loc.ToEnglish("Dalga 5: Kaos"), Is.EqualTo("Wave 5: Chaos"));
        }

        [Test]
        public void test_multiline_translates_each_line()
        {
            Assert.That(Loc.ToEnglish("Kıvılcım\n+%3 hasar"), Is.EqualTo("Spark\n+3% damage"));
        }

        [Test]
        public void test_unknown_text_passes_through()
        {
            Assert.That(Loc.ToEnglish("xyz 123"), Is.EqualTo("xyz 123"));
        }
    }
}
