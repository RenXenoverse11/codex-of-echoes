using System.Collections.Generic;
using System.Text;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// The observable battle. Mutated only by BattleEngine; the view reads it but
    /// animates from events rather than from diffs of this.
    /// </summary>
    public sealed class BattleState
    {
        private readonly List<int> _selection = new List<int>();

        public BattleState(TileGrid grid, Combatant player, Combatant enemy, int seed)
        {
            Grid = grid;
            Player = player;
            Enemy = enemy;
            Seed = seed;
            TurnNumber = 1;
            Outcome = BattleOutcome.InProgress;
        }

        public TileGrid Grid { get; }

        public Combatant Player { get; }

        public Combatant Enemy { get; }

        public int Seed { get; }

        public int TurnNumber { get; internal set; }

        public BattleOutcome Outcome { get; internal set; }

        public IReadOnlyList<int> Selection => _selection;

        /// <summary>Selected tiles concatenated in selection order, uppercased.</summary>
        public string CurrentWord
        {
            get
            {
                var builder = new StringBuilder();
                foreach (var index in _selection)
                {
                    builder.Append(Grid[index].Letter);
                }

                return builder.ToString().ToUpperInvariant();
            }
        }

        internal void Select(int index) => _selection.Add(index);

        internal bool IsSelected(int index) => _selection.Contains(index);

        internal int RemoveLastSelected()
        {
            var last = _selection[_selection.Count - 1];
            _selection.RemoveAt(_selection.Count - 1);
            return last;
        }

        internal void ClearSelection() => _selection.Clear();

        internal List<int> SnapshotSelection() => new List<int>(_selection);

        internal List<LetterTile> SelectedTiles()
        {
            var tiles = new List<LetterTile>(_selection.Count);
            foreach (var index in _selection)
            {
                tiles.Add(Grid[index]);
            }

            return tiles;
        }
    }
}
