using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>
    /// Chapter 1's Aswang. Strikes for 7-11, and every fourth turn Feasts for 20 while
    /// healing 10 — telegraphed a full turn ahead so the player can see it coming.
    /// </summary>
    public sealed class AswangBehaviour : EnemyBehaviour
    {
        public const int FeastInterval = 4;
        public const int FeastDamage = 20;
        public const int FeastHeal = 10;
        public const int StrikeMin = 7;
        public const int StrikeMax = 11;

        public AswangBehaviour(IRandomSource rng) : base(rng)
        {
        }

        public override EnemyAbility SpecialAbility => EnemyAbility.Feast;

        public override EnemyAbility AbilityForTurn(int turnNumber) =>
            turnNumber > 0 && turnNumber % FeastInterval == 0
                ? EnemyAbility.Feast
                : EnemyAbility.Strike;

        public override EnemyAction Act(int turnNumber)
        {
            if (AbilityForTurn(turnNumber) == EnemyAbility.Feast)
            {
                return new EnemyAction(EnemyAbility.Feast, FeastDamage, FeastHeal);
            }

            return new EnemyAction(
                EnemyAbility.Strike, Rng.NextInclusive(StrikeMin, StrikeMax), 0);
        }
    }
}
