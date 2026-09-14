using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class DamageCalculatorTests
    {
        private static IReadOnlyList<LetterTile> Spell(params string[] letters) =>
            letters.Select(LetterTable.CreateTile).ToList();

        private static int Damage(StoryWordTier tier, params string[] letters)
        {
            var tiles = Spell(letters);
            var wordLength = letters.Sum(l => l.Length);
            return DamageCalculator.Compute(tiles, wordLength, tier);
        }

        [Test]
        public void CatDealsFive()
        {
            // C(3) + A(1) + T(1) = 5 base, length 3, multiplier 1.00
            Assert.That(Damage(StoryWordTier.None, "C", "A", "T"), Is.EqualTo(5));
        }

        [Test]
        public void SilenceDealsSeventeen()
        {
            // 9 base, length 7, multiplier 1.88 -> 16.92 -> 17
            Assert.That(
                Damage(StoryWordTier.None, "S", "I", "L", "E", "N", "C", "E"),
                Is.EqualTo(17));
        }

        [Test]
        public void SaltAsABaneWordDealsTwelve()
        {
            // 4 base, length 4, multiplier 1.22, Bane 2.5 -> 12.2 -> 12
            Assert.That(Damage(StoryWordTier.Bane, "S", "A", "L", "T"), Is.EqualTo(12));
        }

        [Test]
        public void GarlicAsABaneWordDealsThirtySeven()
        {
            // 9 base, length 6, multiplier 1.66, Bane 2.5 -> 37.35 -> 37
            Assert.That(
                Damage(StoryWordTier.Bane, "G", "A", "R", "L", "I", "C"),
                Is.EqualTo(37));
        }

        [Test]
        public void EchoTierMultipliesByOnePointFive()
        {
            var plain = Damage(StoryWordTier.None, "D", "A", "W", "N");
            var echo = Damage(StoryWordTier.Echo, "D", "A", "W", "N");

            // D(2)+A(1)+W(4)+N(1) = 8 base, length 4, x1.22 = 9.76 -> 10
            // Echo: 9.76 x 1.5 = 14.64 -> 15
            Assert.That(plain, Is.EqualTo(10));
            Assert.That(echo, Is.EqualTo(15));
        }

        [Test]
        public void RoundsHalfAwayFromZeroNotToEven()
        {
            // Two 'E' tiles and an 'S': base 2... construct an exact .5 case instead.
            // K(5) + E(1) = 6 base is length 2 (illegal), so use a 3-letter build:
            // I(1) + C(3) + E(1) = 5 base, length 3, x1.00, Echo x1.5 = 7.5
            // Banker's rounding would give 8 here too, so force a .5 that differs:
            // N(1)+A(1)+P(3) = 5 base, length 3, Echo 1.5 -> 7.5 -> 8 (away from zero)
            Assert.That(Damage(StoryWordTier.Echo, "N", "A", "P"), Is.EqualTo(8));

            // E(1)+A(1)+T(1) = 3 base, length 3, Echo 1.5 -> 4.5 -> 5 (away from zero).
            // Banker's rounding would give 4. This is the case that distinguishes them.
            Assert.That(Damage(StoryWordTier.Echo, "E", "A", "T"), Is.EqualTo(5));
        }

        [Test]
        public void LengthMultiplierNeverDropsBelowOne()
        {
            // Length 3 is the minimum, so the step is never applied negatively.
            Assert.That(Damage(StoryWordTier.None, "E", "A", "T"), Is.EqualTo(3));
        }

        [Test]
        public void QuTileCountsTenPointsButTwoCharactersOfLength()
        {
            // Qu(10) + I(1) + Z(10) = 21 base. "QUIZ" is 4 characters, so x1.22.
            // 21 x 1.22 = 25.62 -> 26
            Assert.That(Damage(StoryWordTier.None, "Qu", "I", "Z"), Is.EqualTo(26));
        }

        [Test]
        public void LengthStepMatchesTheSpec()
        {
            Assert.That(DamageCalculator.LengthStep, Is.EqualTo(0.22f).Within(0.0001f));
        }

        [Test]
        public void EmptyWordDealsNoDamage()
        {
            Assert.That(DamageCalculator.Compute(new List<LetterTile>(), 0,
                StoryWordTier.None), Is.EqualTo(0));
        }
    }
}
