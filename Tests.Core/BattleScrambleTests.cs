using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleScrambleTests
    {
        private static BattleEngine NewEngine(int seed = 1, int playerMaxHp = 100)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { "cat" }),
                storyWords: new StoryWordTable(new string[0], new string[0]),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: playerMaxHp,
                seed: seed);

            return new BattleEngine(setup);
        }

        [Test]
        public void ScrambleReplacesTheBoard()
        {
            var engine = NewEngine();
            var before = engine.State.Grid.Tiles.Select(t => t.Letter).ToList();

            var events = engine.Execute(new ScrambleCommand());

            var scrambled = events.OfType<GridScrambledEvent>().Single();
            Assert.That(scrambled.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(engine.State.Grid.Tiles.Select(t => t.Letter), Is.Not.EqualTo(before));
        }

        [Test]
        public void ScrambleKeepsTheVowelInvariant()
        {
            for (var seed = 1; seed <= 50; seed++)
            {
                var engine = NewEngine(seed);
                engine.Execute(new ScrambleCommand());

                Assert.That(engine.State.Grid.VowelCount,
                    Is.GreaterThanOrEqualTo(TileGrid.MinVowels), $"seed {seed}");
            }
        }

        [Test]
        public void ScrambleCostsTheTurnAndTheEnemyStillAttacks()
        {
            var engine = NewEngine();

            var events = engine.Execute(new ScrambleCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Damage, Is.InRange(7, 11));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100 - acted.Damage));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void ScrambleClearsAnyExistingSelection()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.OfType<SelectionClearedEvent>().Any(), Is.True);
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void ScrambleDealsNoDamageToTheEnemy()
        {
            var engine = NewEngine();

            engine.Execute(new ScrambleCommand());

            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
        }

        [Test]
        public void ScramblingIntoDefeatEndsTheBattle()
        {
            // 11 HP cannot survive a Strike of 7-11 in the worst case; use 1 to be certain.
            var engine = NewEngine(playerMaxHp: 1);

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(engine.State.Player.Hp, Is.EqualTo(0));
            Assert.That(engine.State.Player.IsDefeated, Is.True);
            Assert.That(events.OfType<BattleEndedEvent>().Single().Outcome,
                Is.EqualTo(BattleOutcome.Defeat));
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Defeat));
        }

        [Test]
        public void NoTelegraphIsEmittedAfterTheBattleEnds()
        {
            var engine = NewEngine(playerMaxHp: 1);

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.OfType<EnemyTelegraphedEvent>(), Is.Empty);
        }

        [Test]
        public void CommandsAfterDefeatAreRejected()
        {
            var engine = NewEngine(playerMaxHp: 1);
            engine.Execute(new ScrambleCommand());

            var events = engine.Execute(new ScrambleCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }
    }
}
