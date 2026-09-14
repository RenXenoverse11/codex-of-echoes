using System.Collections.Generic;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    public enum CombatantId
    {
        Liora = 0,
        Enemy = 1,
    }

    public enum BattleOutcome
    {
        InProgress = 0,
        Victory = 1,
        Defeat = 2,
    }

    /// <summary>
    /// What happened, in causal order. The view replays these as an animation queue;
    /// a state diff would report what changed but not in what sequence or why.
    /// </summary>
    public abstract class BattleEvent
    {
    }

    public sealed class TileSelectedEvent : BattleEvent
    {
        public TileSelectedEvent(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class TileDeselectedEvent : BattleEvent
    {
        public TileDeselectedEvent(int index)
        {
            Index = index;
        }

        public int Index { get; }
    }

    public sealed class SelectionClearedEvent : BattleEvent
    {
    }

    /// <summary>
    /// An illegal command. Never thrown — a mid-battle exception would strand the view
    /// out of sync with the state, which is the worst failure mode available here.
    /// </summary>
    public sealed class CommandRejectedEvent : BattleEvent
    {
        public CommandRejectedEvent(string reason)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }

    /// <summary>A legal command that formed an unusable word. Costs no turn.</summary>
    public sealed class WordRejectedEvent : BattleEvent
    {
        public WordRejectedEvent(string word, WordRejectionReason reason)
        {
            Word = word;
            Reason = reason;
        }

        public string Word { get; }

        public WordRejectionReason Reason { get; }
    }

    public sealed class WordCastEvent : BattleEvent
    {
        public WordCastEvent(string word, int damage, StoryWordTier tier)
        {
            Word = word;
            Damage = damage;
            Tier = tier;
        }

        public string Word { get; }

        public int Damage { get; }

        public StoryWordTier Tier { get; }
    }

    public sealed class DamageDealtEvent : BattleEvent
    {
        public DamageDealtEvent(CombatantId target, int amount, int newHp)
        {
            Target = target;
            Amount = amount;
            NewHp = newHp;
        }

        public CombatantId Target { get; }

        public int Amount { get; }

        public int NewHp { get; }
    }

    public sealed class HealedEvent : BattleEvent
    {
        public HealedEvent(CombatantId target, int amount, int newHp)
        {
            Target = target;
            Amount = amount;
            NewHp = newHp;
        }

        public CombatantId Target { get; }

        public int Amount { get; }

        public int NewHp { get; }
    }

    public sealed class TilesConsumedEvent : BattleEvent
    {
        public TilesConsumedEvent(IReadOnlyList<int> indices)
        {
            Indices = indices;
        }

        public IReadOnlyList<int> Indices { get; }
    }

    public sealed class TilesFellEvent : BattleEvent
    {
        public TilesFellEvent(IReadOnlyList<TileMove> moves)
        {
            Moves = moves;
        }

        public IReadOnlyList<TileMove> Moves { get; }
    }

    public sealed class TilesSpawnedEvent : BattleEvent
    {
        public TilesSpawnedEvent(IReadOnlyList<TileSpawn> spawns)
        {
            Spawns = spawns;
        }

        public IReadOnlyList<TileSpawn> Spawns { get; }
    }

    public sealed class GridScrambledEvent : BattleEvent
    {
        public GridScrambledEvent(IReadOnlyList<LetterTile> tiles)
        {
            Tiles = tiles;
        }

        public IReadOnlyList<LetterTile> Tiles { get; }
    }

    public sealed class EnemyTelegraphedEvent : BattleEvent
    {
        public EnemyTelegraphedEvent(EnemyAbility ability)
        {
            Ability = ability;
        }

        public EnemyAbility Ability { get; }
    }

    public sealed class EnemyActedEvent : BattleEvent
    {
        public EnemyActedEvent(EnemyAbility ability, int damage, int heal)
        {
            Ability = ability;
            Damage = damage;
            Heal = heal;
        }

        public EnemyAbility Ability { get; }

        public int Damage { get; }

        public int Heal { get; }
    }

    public sealed class BattleEndedEvent : BattleEvent
    {
        public BattleEndedEvent(BattleOutcome outcome)
        {
            Outcome = outcome;
        }

        public BattleOutcome Outcome { get; }
    }
}
