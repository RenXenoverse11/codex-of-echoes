using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Rng;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class AswangBehaviourTests
    {
        private static AswangBehaviour NewBehaviour(int seed = 1) =>
            new AswangBehaviour(new SeededRandom(seed));

        [TestCase(1, EnemyAbility.Strike)]
        [TestCase(2, EnemyAbility.Strike)]
        [TestCase(3, EnemyAbility.Strike)]
        [TestCase(4, EnemyAbility.Feast)]
        [TestCase(5, EnemyAbility.Strike)]
        [TestCase(7, EnemyAbility.Strike)]
        [TestCase(8, EnemyAbility.Feast)]
        [TestCase(12, EnemyAbility.Feast)]
        public void FeastsOnEveryFourthTurn(int turn, EnemyAbility expected)
        {
            Assert.That(NewBehaviour().AbilityForTurn(turn), Is.EqualTo(expected));
        }

        [TestCase(3, true)]
        [TestCase(7, true)]
        [TestCase(11, true)]
        [TestCase(1, false)]
        [TestCase(4, false)]
        [TestCase(8, false)]
        public void TelegraphsFeastOneTurnAhead(int turn, bool expected)
        {
            Assert.That(NewBehaviour().TelegraphsFeastNextTurn(turn), Is.EqualTo(expected));
        }

        [Test]
        public void StrikeDamageStaysInSevenToEleven()
        {
            var behaviour = NewBehaviour(4242);

            for (var turn = 1; turn <= 300; turn++)
            {
                if (turn % AswangBehaviour.FeastInterval == 0)
                {
                    continue;
                }

                var action = behaviour.Act(turn);

                Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Strike));
                Assert.That(action.Damage, Is.InRange(7, 11));
                Assert.That(action.Heal, Is.EqualTo(0));
            }
        }

        [Test]
        public void FeastDealsTwentyAndHealsTen()
        {
            var action = NewBehaviour().Act(4);

            Assert.That(action.Ability, Is.EqualTo(EnemyAbility.Feast));
            Assert.That(action.Damage, Is.EqualTo(20));
            Assert.That(action.Heal, Is.EqualTo(10));
        }

        [Test]
        public void ConstantsMatchTheSpec()
        {
            Assert.That(AswangBehaviour.FeastInterval, Is.EqualTo(4));
            Assert.That(AswangBehaviour.FeastDamage, Is.EqualTo(20));
            Assert.That(AswangBehaviour.FeastHeal, Is.EqualTo(10));
            Assert.That(AswangBehaviour.StrikeMin, Is.EqualTo(7));
            Assert.That(AswangBehaviour.StrikeMax, Is.EqualTo(11));
        }

        [Test]
        public void SameSeedProducesSameStrikeDamage()
        {
            var first = NewBehaviour(99).Act(1).Damage;
            var second = NewBehaviour(99).Act(1).Damage;

            Assert.That(first, Is.EqualTo(second));
        }
    }
}
