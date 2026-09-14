using System.Collections.Generic;

namespace CodexOfEchoes.Core.Grid
{
    /// <summary>A tile sliding from one board index to another under gravity.</summary>
    public readonly struct TileMove
    {
        public TileMove(int from, int to)
        {
            From = from;
            To = to;
        }

        public int From { get; }

        public int To { get; }
    }

    /// <summary>A newly drawn tile entering the board at the top of a column.</summary>
    public readonly struct TileSpawn
    {
        public TileSpawn(int index, LetterTile tile)
        {
            Index = index;
            Tile = tile;
        }

        public int Index { get; }

        public LetterTile Tile { get; }
    }

    /// <summary>
    /// What a consume did to the board. Carries enough ordering for the view to
    /// animate the fall: apply every move, then every spawn.
    /// </summary>
    public sealed class GridRefillResult
    {
        public GridRefillResult(IReadOnlyList<TileMove> moves, IReadOnlyList<TileSpawn> spawns)
        {
            Moves = moves;
            Spawns = spawns;
        }

        public IReadOnlyList<TileMove> Moves { get; }

        public IReadOnlyList<TileSpawn> Spawns { get; }
    }
}
