using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class TikbalangBehaviourTests
    {
        private static TikbalangBehaviour NewBehaviour(int seed = 1) =>
            new TikbalangBehaviour(new SeededRandom(seed));

        [TestCase(1, EnemyAbility.Startle)]
        [TestCase(2, EnemyAbility.Startle)]
        [TestCase(3, EnemyAbility.Mislead)]
        [TestCase(4, EnemyAbility.Startle)]
        [TestCase(6, EnemyAbility.Mislead)]
        [TestCase(9, EnemyAbility.Mislead)]
        public void MisleadsOnEveryThirdTurn(int turn, EnemyAbility expected)
        {
            Assert.That(NewBehaviour().AbilityForTurn(turn), Is.EqualTo(expected));
        }

        [TestCase(2, true)]
        [TestCase(5, true)]
        [TestCase(8, true)]
        [TestCase(1, false)]
        [TestCase(3, false)]
        [TestCase(6, false)]
        public void TelegraphsMisleadOneTurnAhead(int turn, bool expected)
        {
            Assert.That(NewBehaviour().TelegraphsSpecialNextTurn(turn), Is.EqualTo(expected));
        }

        [Test]
        public void StartleDamageStaysInFiveToNine()
        {
            var behaviour = NewBehaviour(4242);

            for (var turn = 1; turn <= 300; turn++)
            {
                if (turn % TikbalangBehaviour.MisleadInterval == 0)
                {
                    continue;
                }

                var action = behaviour.Act(turn);

                Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Startle));
                Assert.That(action.Damage, Is.InRange(5, 9));
                Assert.That(action.Heal, Is.EqualTo(0));
                Assert.That(action.ScramblesGrid, Is.False);
            }
        }

        [Test]
        public void MisleadDealsNoDamageAndScramblesTheGrid()
        {
            var action = NewBehaviour().Act(3);

            Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Mislead));
            Assert.That(action.Damage, Is.EqualTo(0));
            Assert.That(action.Heal, Is.EqualTo(0));
            Assert.That(action.ScramblesGrid, Is.True);
        }

        [Test]
        public void ConstantsMatchTheSpec()
        {
            Assert.That(TikbalangBehaviour.MisleadInterval, Is.EqualTo(3));
            Assert.That(TikbalangBehaviour.StartleMin, Is.EqualTo(5));
            Assert.That(TikbalangBehaviour.StartleMax, Is.EqualTo(9));
        }

        [Test]
        public void SameSeedProducesSameStartleDamage()
        {
            var first = NewBehaviour(99).Act(1).Damage;
            var second = NewBehaviour(99).Act(1).Damage;

            Assert.That(first, Is.EqualTo(second));
        }
    }
}
