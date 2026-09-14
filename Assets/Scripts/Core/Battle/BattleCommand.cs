namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// Everything the player can do. Plain classes rather than records because Unity 6
    /// has unreliable C# 9 support for records and init setters.
    /// </summary>
    public abstract class BattleCommand
    {
    }

    public sealed class SelectTileCommand : BattleCommand
    {
        public SelectTileCommand(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class DeselectLastCommand : BattleCommand
    {
    }

    public sealed class ClearSelectionCommand : BattleCommand
    {
    }

    public sealed class CastWordCommand : BattleCommand
    {
    }

    public sealed class ScrambleCommand : BattleCommand
    {
    }
}
