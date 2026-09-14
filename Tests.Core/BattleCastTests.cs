using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using NUnit.Framework;

namespace CodexOfEchoes.Core.Tests
{
    public class BattleCastTests
    {
        private static BattleEngine NewEngine(
            int seed = 1, IEnumerable<string> words = null, IEnumerable<string> bane = null)
        {
            var setup = new BattleSetup(
                dictionary: new HashSetWordDictionary(
                    words ?? new[] { "cat", "salt", "dawn", "eat" }),
                storyWords: new StoryWordTable(
                    echoWords: new[] { "dawn" },
                    baneWords: bane ?? new[] { "salt" }),
                enemyName: "Aswang",
                enemyMaxHp: 75,
                playerMaxHp: 100,
                seed: seed);

            return new BattleEngine(setup);
        }

        /// <summary>
        /// Overwrites the board so tests can cast a chosen word deterministically.
        /// Selection then takes indices 0..n-1.
        /// </summary>
        private static void StackDeck(BattleEngine engine, params string[] letters)
        {
            for (var i = 0; i < letters.Length; i++)
            {
                engine.State.Grid.SetTileForTesting(i, LetterTable.CreateTile(letters[i]));
            }
        }

        private static void SelectFirst(BattleEngine engine, int count)
        {
            for (var i = 0; i < count; i++)
            {
                engine.Execute(new SelectTileCommand(i));
            }
        }

        [Test]
        public void CastingAValidWordDamagesTheEnemy()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var cast = events.OfType<WordCastEvent>().Single();
            Assert.That(cast.Word, Is.EqualTo("CAT"));
            Assert.That(cast.Damage, Is.EqualTo(5));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(70));
        }

        [Test]
        public void EventOrderIsCausal()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var types = engine.Execute(new CastWordCommand())
                .Select(e => e.GetType().Name).ToList();

            var wordCast = types.IndexOf(nameof(WordCastEvent));
            var enemyHit = types.IndexOf(nameof(DamageDealtEvent));
            var consumed = types.IndexOf(nameof(TilesConsumedEvent));
            var fell = types.IndexOf(nameof(TilesFellEvent));
            var spawned = types.IndexOf(nameof(TilesSpawnedEvent));
            var enemyActed = types.IndexOf(nameof(EnemyActedEvent));

            Assert.That(wordCast, Is.LessThan(enemyHit));
            Assert.That(enemyHit, Is.LessThan(consumed));
            Assert.That(consumed, Is.LessThan(fell));
            Assert.That(fell, Is.LessThan(spawned));
            Assert.That(spawned, Is.LessThan(enemyActed));
        }

        [Test]
        public void BaneWordsApplyTheirMultiplier()
        {
            var engine = NewEngine();
            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);

            var cast = engine.Execute(new CastWordCommand()).OfType<WordCastEvent>().Single();

            Assert.That(cast.Tier, Is.EqualTo(StoryWordTier.Bane));
            Assert.That(cast.Damage, Is.EqualTo(12));
        }

        [Test]
        public void EchoWordsApplyTheirMultiplier()
        {
            var engine = NewEngine();
            StackDeck(engine, "D", "A", "W", "N");
            SelectFirst(engine, 4);

            var cast = engine.Execute(new CastWordCommand()).OfType<WordCastEvent>().Single();

            Assert.That(cast.Tier, Is.EqualTo(StoryWordTier.Echo));
            Assert.That(cast.Damage, Is.EqualTo(15));
        }

        [Test]
        public void CastingConsumesTheSelectedTilesAndRefillsTheBoard()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var consumed = events.OfType<TilesConsumedEvent>().Single();
            Assert.That(consumed.Indices, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(events.OfType<TilesSpawnedEvent>().Single().Spawns.Count, Is.EqualTo(3));
            Assert.That(engine.State.Grid.Tiles.Count, Is.EqualTo(TileGrid.Size));
            Assert.That(engine.State.Selection, Is.Empty);
        }

        [Test]
        public void TheEnemyStrikesBackAndAdvancesTheTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Ability, Is.EqualTo(EnemyAbility.Strike));
            Assert.That(acted.Damage, Is.InRange(7, 11));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100 - acted.Damage));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(2));
        }

        [Test]
        public void AShortWordIsRejectedAndCostsNoTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "A", "T");
            SelectFirst(engine, 2);

            var events = engine.Execute(new CastWordCommand());

            var rejected = events.OfType<WordRejectedEvent>().Single();
            Assert.That(rejected.Reason, Is.EqualTo(WordRejectionReason.TooShort));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(75));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
        }

        [Test]
        public void AnUnknownWordIsRejectedAndCostsNoTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "Z", "Z", "Z");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            var rejected = events.OfType<WordRejectedEvent>().Single();
            Assert.That(rejected.Reason, Is.EqualTo(WordRejectionReason.NotInDictionary));
            Assert.That(rejected.Word, Is.EqualTo("ZZZ"));
            Assert.That(engine.State.TurnNumber, Is.EqualTo(1));
        }

        [Test]
        public void ARejectedWordKeepsTheSelectionSoThePlayerCanEditIt()
        {
            var engine = NewEngine();
            StackDeck(engine, "Z", "Z", "Z");
            SelectFirst(engine, 3);

            engine.Execute(new CastWordCommand());

            // Backspace has to remain useful after a miss.
            Assert.That(engine.State.Selection, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void CastingWithNothingSelectedIsRejectedAsTooShort()
        {
            var engine = NewEngine();

            var events = engine.Execute(new CastWordCommand());

            Assert.That(events.OfType<WordRejectedEvent>().Single().Reason,
                Is.EqualTo(WordRejectionReason.TooShort));
        }

        [Test]
        public void FeastLandsOnTurnFourDealingTwentyAndHealing()
        {
            var engine = NewEngine();

            for (var turn = 1; turn <= 3; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            Assert.That(engine.State.TurnNumber, Is.EqualTo(4));
            var enemyHpBeforeFeastTurn = engine.State.Enemy.Hp;

            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);
            var events = engine.Execute(new CastWordCommand());

            var acted = events.OfType<EnemyActedEvent>().Single();
            Assert.That(acted.Ability, Is.EqualTo(EnemyAbility.Feast));
            Assert.That(acted.Damage, Is.EqualTo(20));
            Assert.That(acted.Heal, Is.EqualTo(10));

            var healed = events.OfType<HealedEvent>().Single();
            Assert.That(healed.Target, Is.EqualTo(CombatantId.Enemy));
            // 5 damage from CAT, then 10 healed back.
            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(enemyHpBeforeFeastTurn - 5 + 10));
        }

        [Test]
        public void FeastIsTelegraphedOnTheTurnBefore()
        {
            var engine = NewEngine();

            for (var turn = 1; turn <= 2; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            // Resolving turn 3 should warn that turn 4 Feasts.
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);
            var events = engine.Execute(new CastWordCommand());

            var telegraph = events.OfType<EnemyTelegraphedEvent>().Single();
            Assert.That(telegraph.Ability, Is.EqualTo(EnemyAbility.Feast));
        }

        [Test]
        public void NoTelegraphOnAnOrdinaryTurn()
        {
            var engine = NewEngine();
            StackDeck(engine, "C", "A", "T");
            SelectFirst(engine, 3);

            var events = engine.Execute(new CastWordCommand());

            Assert.That(events.OfType<EnemyTelegraphedEvent>(), Is.Empty);
        }

        [Test]
        public void LethalEndsTheBattleBeforeTheEnemyCanAct()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt" }, bane: new[] { "salt" });
            // Drop the enemy to 12 HP so one SALT (12 damage) is exactly lethal.
            engine.State.Enemy.TakeDamage(63);

            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            var events = engine.Execute(new CastWordCommand());

            Assert.That(engine.State.Enemy.Hp, Is.EqualTo(0));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
            Assert.That(engine.State.Player.Hp, Is.EqualTo(100));
            Assert.That(events.OfType<BattleEndedEvent>().Single().Outcome,
                Is.EqualTo(BattleOutcome.Victory));
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void LethalStillResolvesOnAFeastTurn()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt", "cat" });

            for (var turn = 1; turn <= 3; turn++)
            {
                StackDeck(engine, "C", "A", "T");
                SelectFirst(engine, 3);
                engine.Execute(new CastWordCommand());
            }

            Assert.That(engine.State.TurnNumber, Is.EqualTo(4));
            engine.State.Enemy.TakeDamage(engine.State.Enemy.Hp - 12);
            var playerHpBefore = engine.State.Player.Hp;

            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            var events = engine.Execute(new CastWordCommand());

            // Winning the race means eating no Feast.
            Assert.That(engine.State.Player.Hp, Is.EqualTo(playerHpBefore));
            Assert.That(events.OfType<EnemyActedEvent>(), Is.Empty);
            Assert.That(engine.State.Outcome, Is.EqualTo(BattleOutcome.Victory));
        }

        [Test]
        public void CommandsAfterTheBattleEndsAreRejected()
        {
            var engine = NewEngine(seed: 1, words: new[] { "salt" }, bane: new[] { "salt" });
            engine.State.Enemy.TakeDamage(63);
            StackDeck(engine, "S", "A", "L", "T");
            SelectFirst(engine, 4);
            engine.Execute(new CastWordCommand());

            var events = engine.Execute(new SelectTileCommand(0));

            Assert.That(events.Single(), Is.TypeOf<CommandRejectedEvent>());
        }
    }
}
