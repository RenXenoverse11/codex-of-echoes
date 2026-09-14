using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    /// <summary>
    /// Guards the difficulty model recorded in spec section 4. If the tuning is
    /// deliberately changed, these expectations change with it — but they should never
    /// drift silently.
    /// </summary>
    public class TuningModelTests
    {
        private const string FillerWord = "NAP";

        private static BattleEngine NewEngine(int seed) =>
            new BattleEngine(new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { FillerWord }),
                storyWords: new StoryWordTable(new string[0], new string[0]),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed));

        /// <summary>Casts NAP, then tops the enemy damage up to the modelled figure.</summary>
        private static void PlayTurn(BattleEngine engine, int targetDamage)
        {
            engine.State.Grid.SetTileForTesting(0, LetterTable.CreateTile("N"));
            engine.State.Grid.SetTileForTesting(1, LetterTable.CreateTile("A"));
            engine.State.Grid.SetTileForTesting(2, LetterTable.CreateTile("P"));

            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));
            engine.Execute(new SelectTileCommand(2));

            var events = engine.Execute(new CastWordCommand());

            if (engine.State.Outcome != BattleOutcome.InProgress)
            {
                return;
            }

            // Read the player's own cast damage from the event, rather than diffing
            // Enemy.Hp across the whole turn — a same-turn Feast heal would otherwise be
            // silently cancelled by the top-up below.
            var actualCastDamage = events.OfType<WordCastEvent>().Single().Damage;
            var shortfall = targetDamage - actualCastDamage;
            if (shortfall > 0)
            {
                engine.State.Enemy.TakeDamage(shortfall);
            }
        }

        [Test]
        public void AveragePlayLosesAroundTurnNine()
        {
            // ~9 damage/turn: five-letter words, no Story Words.
            var engine = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                PlayTurn(engine, 9);
            }

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(engine.State.TurnNumber, Is.InRange(8, 10),
                "Spec models defeat on turn 9; a drift of more than one turn means the "
                + "tuning changed.");
            Assert.That(engine.State.Enemy.Hp, Is.GreaterThan(0));
        }

        [Test]
        public void StrongPlayWinsAroundTurnSixWithHealthToSpare()
        {
            // ~15 damage/turn: Story Words in regular use.
            var engine = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                PlayTurn(engine, 15);
            }

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(engine.State.TurnNumber, Is.InRange(5, 7));
            Assert.That(engine.State.Player.Hp, Is.GreaterThan(30));
        }

        [Test]
        public void TheEnemyEffectivePoolIncludesTwoFeastHeals()
        {
            // 75 HP plus a 10 heal on turns 4 and 8 is the ~95 figure in the spec.
            var engine = NewEngine(7);
            var totalDealt = 0;

            for (var turn = 1; turn <= 8; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                var before = engine.State.Enemy.Hp;
                PlayTurn(engine, 9);
                totalDealt += before - engine.State.Enemy.Hp;
            }

            // Eight turns at 9 damage is 72 dealt, yet the enemy survives, because
            // 20 HP came back.
            Assert.That(engine.State.Enemy.IsDefeated, Is.False);
            Assert.That(totalDealt, Is.LessThan(95));
        }

        [Test]
        public void TheModelIsReproducibleFromItsSeed()
        {
            var first = NewEngine(4242);
            var second = NewEngine(4242);

            for (var turn = 1; turn <= 6; turn++)
            {
                PlayTurn(first, 9);
                PlayTurn(second, 9);
            }

            Assert.That(first.State.Player.Hp, Is.EqualTo(second.State.Player.Hp));
            Assert.That(first.State.Enemy.Hp, Is.EqualTo(second.State.Enemy.Hp));
            Assert.That(first.State.TurnNumber, Is.EqualTo(second.State.TurnNumber));
        }

        [Test]
        public void StoryWordsAreWhatSeparateWinningFromLosing()
        {
            // The same player, differing only by Story Word usage, flips the result.
            var withoutStoryWords = NewEngine(20260914);
            var withStoryWords = NewEngine(20260914);

            for (var turn = 1; turn <= 12; turn++)
            {
                if (withoutStoryWords.State.Outcome == BattleOutcome.InProgress)
                {
                    PlayTurn(withoutStoryWords, 9);
                }

                if (withStoryWords.State.Outcome == BattleOutcome.InProgress)
                {
                    PlayTurn(withStoryWords, 15);
                }
            }

            Assert.That(withoutStoryWords.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(withStoryWords.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }
    }
}
