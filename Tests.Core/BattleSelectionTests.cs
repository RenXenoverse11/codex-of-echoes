using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleSelectionTests
    {
        private static BattleEngine NewEngine(int seed = 1)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { "cat", "salt" }),
                storyWords: new StoryWordTable(new[] { "dawn" }, new[] { "salt" }),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed);

            return new BattleEngine(setup);
        }

        [Test]
        public void StartsInProgressAtTurnOneWithFullHealth()
        {
            var engine = NewEngine();

            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
            Assert.That(engine.State.Enemy.Name, Is.EqualTo("Aswang"));
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void StoresItsSeedForReplay()
        {
            Assert.That(NewEngine(20260914).State.Seed, Is.EqualTo(20260914));
        }

        [Test]
        public void SelectingATileEmitsTileSelected()
        {
            var engine = NewEngine();

            var events = engine.Execute(new SelectTileCommand(5));

            Assert.That(events.Single(), Is.TypeOf<TileSelectedEvent>());
            Assert.That(((TileSelectedEvent)events.Single()).Index, Is.EqualTo(5));
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void SelectionPreservesOrder()
        {
            var engine = NewEngine();

            engine.Execute(new SelectTileCommand(9));
            engine.Execute(new SelectTileCommand(2));
            engine.Execute(new SelectTileCommand(14));

            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 9, 2, 14 }));
        }

        [Test]
        public void CurrentWordConcatenatesSelectedTilesInOrder()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));

            var expected =
                (engine.State.Grid[0].Letter + engine.State.Grid[1].Letter).ToUpperInvariant();

            Assert.That(engine.State.CurrentWord, Is.EqualTo(expected));
        }

        [Test]
        public void SelectingTheSameTileTwiceIsRejected()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(3));

            var events = engine.Execute(new SelectTileCommand(3));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 3 }));
        }

        [TestCase(-1)]
        [TestCase(16)]
        [TestCase(999)]
        public void SelectingAnOutOfRangeIndexIsRejectedWithoutThrowing(int index)
        {
            var engine = NewEngine();

            var events = engine.Execute(new SelectTileCommand(index));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void DeselectLastRemovesTheMostRecentTile()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(1));
            engine.Execute(new SelectTileCommand(7));

            var events = engine.Execute(new DeselectLastCommand());

            Assert.That(events.Single(), Is.TypeOf<TileDeselectedEvent>());
            Assert.That(((TileDeselectedEvent)events.Single()).Index, Is.EqualTo(7));
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void DeselectLastOnAnEmptySelectionIsRejected()
        {
            var engine = NewEngine();

            var events = engine.Execute(new DeselectLastCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }

        [Test]
        public void ClearSelectionEmptiesTheSelection()
        {
            var engine = NewEngine();
            engine.Execute(new SelectTileCommand(4));
            engine.Execute(new SelectTileCommand(8));

            var events = engine.Execute(new ClearSelectionCommand());

            Assert.That(events.Single(), Is.TypeOf<SelectionClearedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void ClearSelectionIsIdempotent()
        {
            var engine = NewEngine();

            var events = engine.Execute(new ClearSelectionCommand());

            Assert.That(events.Single(), Is.TypeOf<SelectionClearedEvent>());
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void AnUnknownCommandIsRejectedRatherThanThrowing()
        {
            var engine = NewEngine();

            var events = engine.Execute(new UnknownTestCommand());

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }

        private sealed class UnknownTestCommand : BattleCommand
        {
        }
    }
}
