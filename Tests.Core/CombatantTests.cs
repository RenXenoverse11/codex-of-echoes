using CodexOfEchoes.Core.Combat;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class CombatantTests
    {
        [Test]
        public void StartsAtFullHealth()
        {
            var liora = new Combatant("Liora", 100);

            Assert.That(liora.Hp, Is.EqualTo(100));
            Assert.That(liora.MaxHp, Is.EqualTo(100));
            Assert.That(liora.Name, Is.EqualTo("Liora"));
            Assert.That(liora.IsDefeated, Is.False);
        }

        [Test]
        public void TakingDamageReducesAndReturnsHp()
        {
            var aswang = new Combatant("Aswang", 75);

            var remaining = aswang.TakeDamage(37);

            Assert.That(remaining, Is.EqualTo(38));
            Assert.That(aswang.Hp, Is.EqualTo(38));
        }

        [Test]
        public void HpClampsAtZeroAndMarksDefeat()
        {
            var aswang = new Combatant("Aswang", 75);

            aswang.TakeDamage(999);

            Assert.That(aswang.Hp, Is.EqualTo(0));
            Assert.That(aswang.IsDefeated, Is.True);
        }

        [Test]
        public void HealingNeverExceedsMaxHp()
        {
            var aswang = new Combatant("Aswang", 75);
            aswang.TakeDamage(5);

            var result = aswang.Heal(10);

            Assert.That(result, Is.EqualTo(75));
            Assert.That(aswang.Hp, Is.EqualTo(75));
        }

        [Test]
        public void HealingRestoresPartialDamage()
        {
            var aswang = new Combatant("Aswang", 75);
            aswang.TakeDamage(40);

            aswang.Heal(10);

            Assert.That(aswang.Hp, Is.EqualTo(45));
        }

        [Test]
        public void NegativeAmountsAreIgnored()
        {
            var liora = new Combatant("Liora", 100);

            liora.TakeDamage(-50);
            Assert.That(liora.Hp, Is.EqualTo(100));

            liora.TakeDamage(30);
            liora.Heal(-10);
            Assert.That(liora.Hp, Is.EqualTo(70));
        }
    }
}
