using System.Linq;
using CodexOfEchoes.Core.Grid;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class LetterTableTests
    {
        [Test]
        public void CoversTwentySixLetterSlotsIncludingQu()
        {
            Assert.That(LetterTable.AllLetters.Count, Is.EqualTo(26));
            Assert.That(LetterTable.AllLetters, Contains.Item("Qu"));
            Assert.That(LetterTable.AllLetters, Does.Not.Contain("Q"));
        }

        [TestCase("E", 1)]
        [TestCase("U", 1)]
        [TestCase("D", 2)]
        [TestCase("G", 2)]
        [TestCase("C", 3)]
        [TestCase("H", 4)]
        [TestCase("K", 5)]
        [TestCase("J", 8)]
        [TestCase("X", 8)]
        [TestCase("Z", 10)]
        [TestCase("Qu", 10)]
        public void ValuesMatchTheSpecTable(string letter, int expected)
        {
            Assert.That(LetterTable.ValueOf(letter), Is.EqualTo(expected));
        }

        [Test]
        public void ValueLookupIsCaseInsensitive()
        {
            Assert.That(LetterTable.ValueOf("qu"), Is.EqualTo(10));
            Assert.That(LetterTable.ValueOf("e"), Is.EqualTo(1));
        }

        [Test]
        public void WeightsSumToNinetyEight()
        {
            var total = LetterTable.AllLetters.Sum(LetterTable.WeightOf);

            Assert.That(total, Is.EqualTo(98));
            Assert.That(LetterTable.TotalWeight, Is.EqualTo(98));
        }

        [Test]
        public void VowelWeightIsFortyTwo()
        {
            var vowelWeight = LetterTable.AllLetters
                .Where(LetterTable.IsVowel)
                .Sum(LetterTable.WeightOf);

            Assert.That(vowelWeight, Is.EqualTo(42));
        }

        [Test]
        public void VowelsAreAeiouOnly()
        {
            var vowels = LetterTable.AllLetters.Where(LetterTable.IsVowel).ToList();

            Assert.That(vowels, Is.EquivalentTo(new[] { "A", "E", "I", "O", "U" }));
            Assert.That(LetterTable.IsVowel("Y"), Is.False);
            Assert.That(LetterTable.IsVowel("Qu"), Is.False);
        }

        [Test]
        public void EveryLetterHasPositiveValueAndWeight()
        {
            foreach (var letter in LetterTable.AllLetters)
            {
                Assert.That(LetterTable.ValueOf(letter), Is.GreaterThan(0), letter);
                Assert.That(LetterTable.WeightOf(letter), Is.GreaterThan(0), letter);
            }
        }
    }
}
