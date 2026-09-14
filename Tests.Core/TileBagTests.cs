using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class TileBagTests
    {
        [Test]
        public void DrawsOnlyKnownLetters()
        {
            var bag = new TileBag(new SeededRandom(1));

            for (var i = 0; i < 500; i++)
            {
                var tile = bag.Draw();
                Assert.That(LetterTable.AllLetters, Contains.Item(tile.Letter));
                Assert.That(tile.Value, Is.EqualTo(LetterTable.ValueOf(tile.Letter)));
            }
        }

        [Test]
        public void SameSeedProducesSameDraws()
        {
            var first = DrawMany(new TileBag(new SeededRandom(555)), 100);
            var second = DrawMany(new TileBag(new SeededRandom(555)), 100);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void DrawVowelAlwaysReturnsAVowel()
        {
            var bag = new TileBag(new SeededRandom(3));

            for (var i = 0; i < 200; i++)
            {
                Assert.That(LetterTable.IsVowel(bag.DrawVowel().Letter), Is.True);
            }
        }

        [Test]
        public void VowelShareApproximatesTheWeightTable()
        {
            var bag = new TileBag(new SeededRandom(20260914));
            var vowels = 0;
            const int draws = 20000;

            for (var i = 0; i < draws; i++)
            {
                if (LetterTable.IsVowel(bag.Draw().Letter))
                {
                    vowels++;
                }
            }

            // Expected share is 42/98 = 42.9%. Generous band: this asserts the
            // weighting is applied at all, not that the RNG is perfectly uniform.
            var share = (double)vowels / draws;
            Assert.That(share, Is.InRange(0.39, 0.47));
        }

        [Test]
        public void CommonLettersOutnumberRareOnes()
        {
            var bag = new TileBag(new SeededRandom(808));
            var counts = new Dictionary<string, int>();

            for (var i = 0; i < 20000; i++)
            {
                var letter = bag.Draw().Letter;
                counts.TryGetValue(letter, out var current);
                counts[letter] = current + 1;
            }

            // E has weight 12, Z has weight 1.
            Assert.That(counts["E"], Is.GreaterThan(counts["Z"] * 4));
        }

        private static List<string> DrawMany(TileBag bag, int count) =>
            Enumerable.Range(0, count).Select(_ => bag.Draw().Letter).ToList();
    }
}
