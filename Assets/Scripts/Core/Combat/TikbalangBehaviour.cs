using CodexOfEchoes.Core.Rng;

namespace CodexOfEchoes.Core.Combat
{
    /// <summary>
    /// Chapter 2's Tikbalang. Startles for 5-9, and every third turn Misleads instead of
    /// attacking — dealing no damage but scrambling the whole board — telegraphed a full
    /// turn ahead so the player can rush a big word before their letters are shuffled.
    /// </summary>
    public sealed class TikbalangBehaviour : EnemyBehaviour
    {
        public const int MisleadInterval = 3;
        public const int StartleMin = 5;
        public const int StartleMax = 9;

        public TikbalangBehaviour(IRandomSource rng) : base(rng)
        {
        }

        public override EnemyAbility SpecialAbility => EnemyAbility.Mislead;

        public override EnemyAbility AbilityForTurn(int turnNumber) =>
            turnNumber > 0 && turnNumber % MisleadInterval == 0
                ? EnemyAbility.Mislead
                : EnemyAbility.Startle;

        public override EnemyAction Act(int turnNumber)
        {
            if (AbilityForTurn(turnNumber) == EnemyAbility.Mislead)
            {
                return new EnemyAction(
                    EnemyAbility.Mislead, damage: 0, heal: 0, scramblesGrid: true);
            }

            return new EnemyAction(
                EnemyAbility.Startle, Rng.NextInclusive(StartleMin, StartleMax), 0);
        }
    }
}
