using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class SeededRandomTests
    {
        [Test]
        public void SameSeedProducesSameSequence()
        {
            var a = new SeededRandom(12345);
            var b = new SeededRandom(12345);

            var first = Enumerable.Range(0, 50).Select(_ => a.Next(100)).ToList();
            var second = Enumerable.Range(0, 50).Select(_ => b.Next(100)).ToList();

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void DifferentSeedsProduceDifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);

            var first = Enumerable.Range(0, 50).Select(_ => a.Next(100)).ToList();
            var second = Enumerable.Range(0, 50).Select(_ => b.Next(100)).ToList();

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void NextStaysBelowMaxExclusive()
        {
            var rng = new SeededRandom(7);

            for (var i = 0; i < 1000; i++)
            {
                var value = rng.Next(10);
                Assert.That(value, Is.InRange(0, 9));
            }
        }

        [Test]
        public void NextInclusiveCoversBothEnds()
        {
            var rng = new SeededRandom(99);
            var seen = new HashSet<int>();

            for (var i = 0; i < 1000; i++)
            {
                seen.Add(rng.NextInclusive(7, 11));
            }

            Assert.That(seen, Is.EquivalentTo(new[] { 7, 8, 9, 10, 11 }));
        }

        [Test]
        public void SeedIsRecoverable()
        {
            var rng = new SeededRandom(4242);
            rng.Next(10);

            Assert.That(rng.Seed, Is.EqualTo(4242));
        }
    }
}
