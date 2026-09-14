using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class WordValidationTests
    {
        private static HashSetWordDictionary Fixture() =>
            new HashSetWordDictionary(new[] { "cat", "salt", "garlic", "silence", "quiz", "qua" });

        [Test]
        public void DictionaryFindsKnownWords()
        {
            Assert.That(Fixture().Contains("cat"), Is.True);
            Assert.That(Fixture().Contains("aardvark"), Is.False);
        }

        [Test]
        public void DictionaryIsCaseInsensitive()
        {
            Assert.That(Fixture().Contains("CAT"), Is.True);
            Assert.That(Fixture().Contains("SiLeNcE"), Is.True);
        }

        [Test]
        public void DictionaryReportsItsSize()
        {
            Assert.That(Fixture().Count, Is.EqualTo(6));
        }

        [Test]
        public void DictionaryIgnoresBlankAndDuplicateEntries()
        {
            var dictionary = new HashSetWordDictionary(
                new[] { "cat", "CAT", "  ", "", null, " dog " });

            Assert.That(dictionary.Count, Is.EqualTo(2));
            Assert.That(dictionary.Contains("dog"), Is.True);
        }

        [Test]
        public void ValidatorAcceptsAKnownWordOfLegalLength()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("cat"), Is.EqualTo(WordRejectionReason.None));
        }

        [Test]
        public void ValidatorRejectsShortWordsBeforeCheckingTheDictionary()
        {
            // "at" is absent from the fixture, but length is the reason reported.
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("at"), Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void ValidatorRejectsUnknownWords()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate("zzzz"),
                Is.EqualTo(WordRejectionReason.NotInDictionary));
        }

        [Test]
        public void ValidatorTreatsNullAndEmptyAsTooShort()
        {
            var validator = new WordValidator(Fixture());

            Assert.That(validator.Validate(null), Is.EqualTo(WordRejectionReason.TooShort));
            Assert.That(validator.Validate(""), Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void MinimumLengthIsThree()
        {
            Assert.That(WordValidator.MinimumLength, Is.EqualTo(3));
        }

        [Test]
        public void StoryTableTiersEchoAndBaneWords()
        {
            var table = new StoryWordTable(
                echoWords: new[] { "name", "truth", "light" },
                baneWords: new[] { "salt", "garlic", "ash" });

            Assert.That(table.TierOf("truth"), Is.EqualTo(StoryWordTier.Echo));
            Assert.That(table.TierOf("garlic"), Is.EqualTo(StoryWordTier.Bane));
            Assert.That(table.TierOf("cat"), Is.EqualTo(StoryWordTier.None));
        }

        [Test]
        public void BaneBeatsEchoWhenAWordIsInBothSets()
        {
            var table = new StoryWordTable(
                echoWords: new[] { "light" },
                baneWords: new[] { "light" });

            Assert.That(table.TierOf("light"), Is.EqualTo(StoryWordTier.Bane));
        }

        [Test]
        public void StoryTableIsCaseInsensitive()
        {
            var table = new StoryWordTable(new[] { "dawn" }, new[] { "SALT" });

            Assert.That(table.TierOf("DAWN"), Is.EqualTo(StoryWordTier.Echo));
            Assert.That(table.TierOf("salt"), Is.EqualTo(StoryWordTier.Bane));
        }

        [TestCase(StoryWordTier.None, 1.0f)]
        [TestCase(StoryWordTier.Echo, 1.5f)]
        [TestCase(StoryWordTier.Bane, 2.5f)]
        public void MultipliersMatchTheSpec(StoryWordTier tier, float expected)
        {
            Assert.That(StoryWordTable.MultiplierFor(tier), Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
