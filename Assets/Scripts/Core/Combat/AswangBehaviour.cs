using System;
using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Combat
{
    public enum EnemyAbility
    {
        None = 0,
        Strike = 1,
        Feast = 2,
    }

    /// <summary>One enemy action: what it did, and its damage and self-heal.</summary>
    public readonly struct EnemyAction
    {
        public EnemyAction(EnemyAbility ability, int damage, int heal)
        {
            Ability = ability;
            Damage = damage;
            Heal = heal;
        }

        public EnemyAbility Ability { get; }

        public int Damage { get; }

        public int Heal { get; }
    }

    /// <summary>
    /// Chapter 1's Aswang. Strikes for 7-11, and every fourth turn Feasts for 20 while
    /// healing 10 — telegraphed a full turn ahead so the player can see it coming.
    /// </summary>
    public sealed class AswangBehaviour
    {
        public const int FeastInterval = 4;
        public const int FeastDamage = 20;
        public const int FeastHeal = 10;
        public const int StrikeMin = 7;
        public const int StrikeMax = 11;

        private readonly IRandomSource _rng;

        public AswangBehaviour(IRandomSource rng)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
        }

        /// <summary>Turn numbers are 1-based.</summary>
        public EnemyAbility AbilityForTurn(int turnNumber) =>
            turnNumber > 0 && turnNumber % FeastInterval == 0
                ? EnemyAbility.Feast
                : EnemyAbility.Strike;

        public bool TelegraphsFeastNextTurn(int turnNumber) =>
            AbilityForTurn(turnNumber + 1) == EnemyAbility.Feast;

        public EnemyAction Act(int turnNumber)
        {
            if (AbilityForTurn(turnNumber) == EnemyAbility.Feast)
            {
                return new EnemyAction(EnemyAbility.Feast, FeastDamage, FeastHeal);
            }

            return new EnemyAction(
                EnemyAbility.Strike, _rng.NextInclusive(StrikeMin, StrikeMax), 0);
        }
    }
}
