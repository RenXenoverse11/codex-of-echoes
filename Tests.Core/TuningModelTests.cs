using System.Collections.Generic;
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

        /// <summary>
        /// Casts NAP, then tops the enemy damage up to the modelled figure. Returns the
        /// events from the cast (or the underlying command's rejection/end-of-battle
        /// events) so callers can inspect what actually happened this turn — e.g. whether
        /// a Feast heal landed — rather than re-deriving it from HP deltas.
        /// </summary>
        private static IReadOnlyList<BattleEvent> PlayTurn(BattleEngine engine, int targetDamage)
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
                return events;
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

            return events;
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

            // The range allows for a "phantom turn": PlayTurn's top-up TakeDamage call
            // happens outside Execute, so it can drop the enemy to 0 HP without tripping
            // BattleEngine's own EndBattle (that only fires inside ResolveEnemyTurn). The
            // win isn't detected until the next PlayTurn's real cast finds the enemy
            // already dead, which can land the recorded win one turn later than the
            // spec's literal "turn 6" — do not tighten this to (5, 6) without accounting
            // for that mechanic.
            Assert.That(engine.State.TurnNumber, Is.InRange(5, 7));
            Assert.That(engine.State.Player.Hp, Is.GreaterThan(30));
        }

        [Test]
        public void TheEnemyEffectivePoolIncludesTwoFeastHeals()
        {
            // Two Feast heals (turns 4 and 8) must actually land — asserted directly via
            // HealedEvent rather than inferred from HP arithmetic, because 8 turns x 9
            // target damage = 72 is already less than the enemy's 75 max HP even with
            // zero healing, so an HP-only check doesn't actually prove the heals occurred.
            var engine = NewEngine(7);
            var totalHealed = 0;

            for (var turn = 1; turn <= 8; turn++)
            {
                if (engine.State.Outcome != BattleOutcome.InProgress)
                {
                    break;
                }

                var events = PlayTurn(engine, 9);
                totalHealed += events.OfType<HealedEvent>()
                    .Where(healed => healed.Target == CombatantId.Enemy)
                    .Sum(healed => healed.Amount);
            }

            Assert.That(totalHealed, Is.EqualTo(2 * AswangBehaviour.FeastHeal));
            Assert.That(engine.State.Enemy.IsDefeated, Is.False);
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
