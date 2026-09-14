using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class TileGridTests
    {
        private static TileGrid NewGrid(int seed) => new TileGrid(new TileBag(new SeededRandom(seed)));

        [Test]
        public void StartsWithSixteenTiles()
        {
            Assert.That(NewGrid(1).Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(TileGrid.Size, Is.EqualTo(16));
            Assert.That(TileGrid.Columns, Is.EqualTo(4));
            Assert.That(TileGrid.Rows, Is.EqualTo(4));
        }

        [Test]
        public void AlwaysStartsWithAtLeastFourVowels()
        {
            for (var seed = 1; seed <= 300; seed++)
            {
                Assert.That(NewGrid(seed).VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void HoldsTheVowelInvariantAfterConsumingEveryVowel()
        {
            for (var seed = 1; seed <= 100; seed++)
            {
                var grid = NewGrid(seed);
                var vowelIndices = Enumerable.Range(0, TileGrid.Size)
                    .Where(i => LetterTable.IsVowel(grid[i].Letter))
                    .ToList();

                grid.Consume(vowelIndices);

                Assert.That(grid.VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void StaysFullAfterConsuming()
        {
            var grid = NewGrid(42);

            grid.Consume(new[] { 0, 5, 10, 15 });

            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
        }

        [Test]
        public void ConsumingReportsOneSpawnPerConsumedTile()
        {
            var grid = NewGrid(42);

            var result = grid.Consume(new[] { 1, 2, 3 });

            Assert.That(result.Spawns.Count, Is.EqualTo(3));
        }

        [Test]
        public void SurvivorsFallToTheBottomOfTheirColumn()
        {
            var grid = NewGrid(77);
            // Column 0 is indices 0, 4, 8, 12 (top to bottom).
            var bottomOfColumnZero = grid[12];

            // Consume index 8, directly above the bottom tile.
            var result = grid.Consume(new[] { 8 });

            // The bottom tile never moves; the tiles above index 8 shift down one row.
            Assert.That(grid[12].Letter, Is.EqualTo(bottomOfColumnZero.Letter));
            Assert.That(result.Moves.Any(m => m.From == 4 && m.To == 8), Is.True);
            Assert.That(result.Moves.Any(m => m.From == 0 && m.To == 4), Is.True);
            Assert.That(result.Spawns.Single().Index, Is.EqualTo(0));
        }

        [Test]
        public void ConsumingAWholeColumnSpawnsFourNewTilesInIt()
        {
            var grid = NewGrid(11);

            var result = grid.Consume(new[] { 2, 6, 10, 14 });

            Assert.That(result.Moves, Is.Empty);
            Assert.That(result.Spawns.Select(s => s.Index),
                Is.EquivalentTo(new[] { 2, 6, 10, 14 }));
        }

        [Test]
        public void ConsumingAcceptsIndicesInAnyOrder()
        {
            var grid = NewGrid(5);
            var shuffled = new[] { 10, 0, 5 };

            var result = grid.Consume(shuffled);

            Assert.That(result.Spawns.Count, Is.EqualTo(3));
            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
        }

        [Test]
        public void ConsumingNothingChangesNothing()
        {
            var grid = NewGrid(9);
            var before = grid.Tiles.Select(t => t.Letter).ToList();

            var result = grid.Consume(new List<int>());

            Assert.That(grid.Tiles.Select(t => t.Letter), Is.EqualTo(before));
            Assert.That(result.Moves, Is.Empty);
            Assert.That(result.Spawns, Is.Empty);
        }

        [Test]
        public void ScrambleReplacesEveryTileAndKeepsTheInvariant()
        {
            var grid = NewGrid(303);
            var before = grid.Tiles.Select(t => t.Letter).ToList();

            grid.Scramble();

            Assert.That(grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(grid.VowelCount, Is.GreaterThanOrEqualTo(TileGrid.MinVowels));
            Assert.That(grid.Tiles.Select(t => t.Letter), Is.Not.EqualTo(before));
        }
    }
}
