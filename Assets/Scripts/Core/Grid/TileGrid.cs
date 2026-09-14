using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>
    /// The 4x4 board. Index 0 is top-left, index 15 is bottom-right;
    /// index = row * Columns + column. Gravity pulls tiles toward higher indices.
    /// </summary>
    public sealed class TileGrid
    {
        public const int Columns = 4;
        public const int Rows = 4;
        public const int Size = Columns * Rows;
        public const int MinVowels = 4;

        private readonly TileBag _bag;
        private readonly LetterTile[] _tiles = new LetterTile[Size];

        public TileGrid(TileBag bag)
        {
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            FillEmptySlots();
            EnforceVowelInvariant();
        }

        public IReadOnlyList<LetterTile> Tiles => _tiles;

        public LetterTile this[int index] => _tiles[index];

        public int VowelCount => _tiles.Count(t => LetterTable.IsVowel(t.Letter));

        /// <summary>
        /// Removes the given tiles, drops survivors to the bottom of their column,
        /// and refills from the top. Indices may arrive in any order.
        /// </summary>
        public GridRefillResult Consume(IReadOnlyList<int> indices)
        {
            if (indices == null)
            {
                throw new ArgumentNullException(nameof(indices));
            }

            if (indices.Count == 0)
            {
                return new GridRefillResult(Array.Empty<TileMove>(), Array.Empty<TileSpawn>());
            }

            var removed = new HashSet<int>(indices);
            var moves = new List<TileMove>();
            var spawns = new List<TileSpawn>();

            for (var column = 0; column < Columns; column++)
            {
                CollapseColumn(column, removed, moves, spawns);
            }

            EnforceVowelInvariant();

            return new GridRefillResult(moves, spawns);
        }

        /// <summary>Replaces the whole board. Costs the player a turn; see BattleEngine.</summary>
        public void Scramble()
        {
            for (var i = 0; i < Size; i++)
            {
                _tiles[i] = _bag.Draw();
            }

            EnforceVowelInvariant();
        }

        private void CollapseColumn(
            int column, HashSet<int> removed, List<TileMove> moves, List<TileSpawn> spawns)
        {
            // Survivors, read bottom-up, keep their relative order as they settle.
            var survivors = new List<int>();
            for (var row = Rows - 1; row >= 0; row--)
            {
                var index = (row * Columns) + column;
                if (!removed.Contains(index))
                {
                    survivors.Add(index);
                }
            }

            var settled = new LetterTile[Rows];
            var writeRow = Rows - 1;

            foreach (var sourceIndex in survivors)
            {
                var destination = (writeRow * Columns) + column;
                settled[writeRow] = _tiles[sourceIndex];

                if (sourceIndex != destination)
                {
                    moves.Add(new TileMove(sourceIndex, destination));
                }

                writeRow--;
            }

            // Everything above the settled survivors is new.
            for (var row = writeRow; row >= 0; row--)
            {
                var index = (row * Columns) + column;
                var tile = _bag.Draw();
                settled[row] = tile;
                spawns.Add(new TileSpawn(index, tile));
            }

            for (var row = 0; row < Rows; row++)
            {
                _tiles[(row * Columns) + column] = settled[row];
            }
        }

        private void FillEmptySlots()
        {
            for (var i = 0; i < Size; i++)
            {
                _tiles[i] = _bag.Draw();
            }
        }

        /// <summary>
        /// Guarantees at least MinVowels vowels on the board. Cheapest available guard
        /// against unplayable hands; real dead-board detection is combinatorial and
        /// deliberately out of scope.
        /// </summary>
        private void EnforceVowelInvariant()
        {
            var deficit = MinVowels - VowelCount;
            if (deficit <= 0)
            {
                return;
            }

            var consonantIndices = Enumerable.Range(0, Size)
                .Where(i => !LetterTable.IsVowel(_tiles[i].Letter))
                .ToList();

            for (var i = 0; i < deficit && i < consonantIndices.Count; i++)
            {
                _tiles[consonantIndices[i]] = _bag.DrawVowel();
            }
        }
    }
}
