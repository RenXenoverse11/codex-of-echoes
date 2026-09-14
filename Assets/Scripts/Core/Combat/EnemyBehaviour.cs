using System;
using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Combat
{
    public enum EnemyAbility
    {
        None = 0,
        Strike = 1,
        Feast = 2,
        Startle = 3,
        Mislead = 4,
    }

    /// <summary>One enemy action: what it did, its damage/self-heal, and any grid effect.</summary>
    public readonly struct EnemyAction
    {
        public EnemyAction(
            EnemyAbility ability, int damage, int heal, bool scramblesGrid = false)
        {
            Ability = ability;
            Damage = damage;
            Heal = heal;
            ScramblesGrid = scramblesGrid;
        }

        public EnemyAbility Ability { get; }

        public int Damage { get; }

        public int Heal { get; }

        /// <summary>True if resolving this action replaces the whole board (see Tikbalang's Mislead).</summary>
        public bool ScramblesGrid { get; }
    }

    /// <summary>
    /// One enemy's AI: what it does on a given turn, and which of its abilities gets
    /// telegraphed a turn ahead. Each enemy is its own subclass rather than a shared
    /// set of tunables, because each mythology chapter's monster is meant to threaten
    /// the player in a genuinely different way, not just with different numbers.
    /// </summary>
    public abstract class EnemyBehaviour
    {
        protected readonly IRandomSource Rng;

        protected EnemyBehaviour(IRandomSource rng)
        {
            Rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>The ability this enemy warns the player about one turn in advance.</summary>
        public abstract EnemyAbility SpecialAbility { get; }

        /// <summary>Turn numbers are 1-based.</summary>
        public abstract EnemyAbility AbilityForTurn(int turnNumber);

        public bool TelegraphsSpecialNextTurn(int turnNumber) =>
            AbilityForTurn(turnNumber + 1) == SpecialAbility;

        public abstract EnemyAction Act(int turnNumber);
    }
}
