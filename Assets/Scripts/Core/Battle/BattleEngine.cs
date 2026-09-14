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
    }
}
