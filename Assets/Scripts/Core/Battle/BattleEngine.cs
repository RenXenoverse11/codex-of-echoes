using System;
using System.Collections.Generic;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Rng;
using CodexOfEchoes.Core.Words;

namespace CodexOfEchoes.Core.Battle
{
    /// <summary>
    /// The entire core contract: one command in, an ordered list of events out.
    /// Synchronous and instantaneous — all timing lives in the presentation layer,
    /// which is what makes the rules testable without waiting on anything.
    /// </summary>
    public sealed class BattleEngine
    {
        private readonly WordValidator _validator;
        private readonly StoryWordTable _storyWords;
        private readonly AswangBehaviour _enemyBehaviour;

        public BattleEngine(BattleSetup setup)
        {
            if (setup == null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            var rng = new SeededRandom(setup.Seed);

            _validator = new WordValidator(setup.Dictionary);
            _storyWords = setup.StoryWords;
            _enemyBehaviour = new AswangBehaviour(rng);

            State = new BattleState(
                grid: new TileGrid(new TileBag(rng)),
                player: new Combatant("Liora", setup.PlayerMaxHp),
                enemy: new Combatant(setup.EnemyName, setup.EnemyMaxHp),
                seed: setup.Seed);
        }

        public BattleState State { get; }

        public IReadOnlyList<BattleEvent> Execute(BattleCommand command)
        {
            var events = new List<BattleEvent>();

            if (command == null)
            {
                events.Add(new CommandRejectedEvent("Command was null."));
                return events;
            }

            if (State.Outcome != BattleOutcome.InProgress)
            {
                events.Add(new CommandRejectedEvent("The battle is already over."));
                return events;
            }

            switch (command)
            {
                case SelectTileCommand select:
                    HandleSelect(select.Index, events);
                    break;

                case DeselectLastCommand _:
                    HandleDeselectLast(events);
                    break;

                case ClearSelectionCommand _:
                    State.ClearSelection();
                    events.Add(new SelectionClearedEvent());
                    break;

                case CastWordCommand _:
                    HandleCastWord(events);
                    break;

                default:
                    events.Add(new CommandRejectedEvent(
                        $"Unsupported command '{command.GetType().Name}'."));
                    break;
            }

            return events;
        }

        private void HandleSelect(int index, List<BattleEvent> events)
        {
            if (index < 0 || index >= TileGrid.Size)
            {
                events.Add(new CommandRejectedEvent($"Tile index {index} is out of range."));
                return;
            }

            if (State.IsSelected(index))
            {
                events.Add(new CommandRejectedEvent($"Tile {index} is already selected."));
                return;
            }

            State.Select(index);
            events.Add(new TileSelectedEvent(index));
        }

        private void HandleDeselectLast(List<BattleEvent> events)
        {
            if (State.Selection.Count == 0)
            {
                events.Add(new CommandRejectedEvent("Nothing is selected."));
                return;
            }

            events.Add(new TileDeselectedEvent(State.RemoveLastSelected()));
        }

        private void HandleCastWord(List<BattleEvent> events)
        {
            var word = State.CurrentWord;
            var rejection = _validator.Validate(word);

            if (rejection != WordRejectionReason.None)
            {
                // Not an error — gameplay. No turn is consumed, and the selection stays
                // intact so Backspace remains useful after a miss.
                events.Add(new WordRejectedEvent(word, rejection));
                return;
            }

            var tier = _storyWords.TierOf(word);
            var damage = DamageCalculator.Compute(State.SelectedTiles(), word.Length, tier);

            events.Add(new WordCastEvent(word, damage, tier));
            events.Add(new DamageDealtEvent(
                CombatantId.Enemy, damage, State.Enemy.TakeDamage(damage)));

            var consumed = State.SnapshotSelection();
            var refill = State.Grid.Consume(consumed);

            events.Add(new TilesConsumedEvent(consumed));
            events.Add(new TilesFellEvent(refill.Moves));
            events.Add(new TilesSpawnedEvent(refill.Spawns));

            State.ClearSelection();
            events.Add(new SelectionClearedEvent());

            ResolveEnemyTurn(events);
        }

        /// <summary>
        /// Runs the enemy half of a turn. Lethal resolves immediately: if the player's
        /// word finished the enemy, it does not act, which is what makes a damage race
        /// winnable on the turn Feast would otherwise land.
        /// </summary>
        private void ResolveEnemyTurn(List<BattleEvent> events)
        {
            if (State.Enemy.IsDefeated)
            {
                EndBattle(BattleOutcome.Victory, events);
                return;
            }

            var action = _enemyBehaviour.Act(State.TurnNumber);
            events.Add(new EnemyActedEvent(action.Ability, action.Damage, action.Heal));
            events.Add(new DamageDealtEvent(
                CombatantId.Liora, action.Damage, State.Player.TakeDamage(action.Damage)));

            if (action.Heal > 0)
            {
                events.Add(new HealedEvent(
                    CombatantId.Enemy, action.Heal, State.Enemy.Heal(action.Heal)));
            }

            if (State.Player.IsDefeated)
            {
                EndBattle(BattleOutcome.Defeat, events);
                return;
            }

            if (_enemyBehaviour.TelegraphsFeastNextTurn(State.TurnNumber))
            {
                events.Add(new EnemyTelegraphedEvent(EnemyAbility.Feast));
            }

            State.TurnNumber++;
        }

        private void EndBattle(BattleOutcome outcome, List<BattleEvent> events)
        {
            State.Outcome = outcome;
            State.ClearSelection();
            events.Add(new BattleEndedEvent(outcome));
        }
    }
}
