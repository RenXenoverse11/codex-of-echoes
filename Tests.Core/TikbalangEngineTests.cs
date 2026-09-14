using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    /// <summary>
    /// Verifies the engine actually wires up Tikbalang's behaviour (EnemyKind selection,
    /// Mislead's grid-scramble side effect) rather than re-testing AI numbers already
    /// covered by TikbalangBehaviourTests.
    /// </summary>
    public class TikbalangEngineTests
    {
        private static BattleEngine NewEngine(int seed = 1) =>
            new BattleEngine(new BattleSetup(
                dictionary: new HashSetWordDictionary(new[] { "cat" }),
                storyWords: new StoryWordTable(new string[0], new string[0]),
                enemyName: "Tikbalang",
                enemyMaxHp: 70,
                playerMaxHp: 100,
                seed: seed,
                enemyKind: EnemyKind.Tikbalang));

        private static void StackDeck(BattleEngine engine, params string[] letters)
        {
            for (var i = 0; i < letters.Length; i++)
            {
                engine.State.Grid.SetTileForTesting(i, LetterTable.CreateTile(letters[i]));
            }
        }

        private static void CastCat(BattleEngine engine)
        {
            StackDeck(engine, "C", "A", "T");
            engine.Execute(new SelectTileCommand(0));
            engine.Execute(new SelectTileCommand(1));
            engine.Execute(new SelectTileCommand(2));
        }

        [Test]
        public void MisleadTurnScramblesTheGridInsteadOfDamagingLiora()
        {
            var engine = NewEngine();

            // Turns 1 and 2 are Startle; turn 3 is Mislead.
            CastCat(engine);
            engine.Execute(new CastWordCommand());
            CastCat(engine);
            engine.Execute(new CastWordCommand());
            Assert.That(engine.State.TurnNumber, Is.EqualTo(3));

            var before = engine.State.Grid.Tiles.Select(t => t.Letter).ToList();
            var hpBefore = engine.State.Player.Hp;

            CastCat(engine);
            var events = engine.Execute(new CastWordCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Ability, Is.EqualTo(EnemyAbility.Mislead));
            Assert.That(acted.Damage, Is.EqualTo(0));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(hpBefore));

            var scrambled = events.OfType<GridScrambledEvent>().Single();
            Assert.That(scrambled.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(engine.State.Grid.Tiles.Select(t => t.Letter), Is.Not.EqualTo(before));
            Assert.That(events.OfType<HealedEvent>(), Is.Empty);
        }

        [Test]
        public void MisleadEventOrderMatchesTheContract()
        {
            // Damage(Liora) must land before the grid scramble.
            var engine = NewEngine();
            CastCat(engine);
            engine.Execute(new CastWordCommand());
            CastCat(engine);
            engine.Execute(new CastWordCommand());

            CastCat(engine);
            var events = engine.Execute(new CastWordCommand()).ToList();

            var damageIndex = events.FindIndex(e =>
                e is DamageDealtEvent d && d.Target == CombatantId.Liora);
            var scrambleIndex = events.FindIndex(e => e is GridScrambledEvent);

            Assert.That(damageIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(scrambleIndex, Is.GreaterThan(damageIndex));
        }

        [Test]
        public void TelegraphsMisleadOneTurnBeforeItLands()
        {
            var engine = NewEngine();

            // Turn 1 -> 2: no telegraph yet (Mislead isn't due until turn 3).
            CastCat(engine);
            engine.Execute(new CastWordCommand());

            // Turn 2 -> 3: this is the turn ahead of Mislead, so it telegraphs now.
            CastCat(engine);
            var events = engine.Execute(new CastWordCommand());

            var telegraph = events.OfType<EnemyTelegraphedEvent>().Single();
            Assert.That(telegraph.Ability, Is.EqualTo(EnemyAbility.Mislead));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(3));
        }
    }
}
