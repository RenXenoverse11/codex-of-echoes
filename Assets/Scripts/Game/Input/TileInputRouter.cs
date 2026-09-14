using System;
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Game.Battle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CodexOfEchoes.Game.Input
{
    /// <summary>
    /// One funnel for both input paths. Keyboard and pointer produce identical commands,
    /// so the engine never learns which device the player used — and a touch path can be
    /// added later without the rules noticing.
    ///
    /// Bindings: letter keys claim a tile, Backspace undoes, Enter casts, Tab scrambles,
    /// Escape clears. The Attack button (see AttackButton.cs) casts the same way Enter
    /// does, via OnAttackClicked.
    /// </summary>
    [RequireComponent(typeof(BattleRunner))]
    public sealed class TileInputRouter : MonoBehaviour
    {
        private BattleRunner _runner;

        private void Awake()
        {
            _runner = GetComponent<BattleRunner>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _runner.IsBusy)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                _runner.Submit(new CastWordCommand());
                return;
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                _runner.Submit(new DeselectLastCommand());
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _runner.Submit(new ClearSelectionCommand());
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                _runner.Submit(new ScrambleCommand());
                return;
            }

            HandleLetterKeys(keyboard);
        }

        /// <summary>Pointer path. TileView forwards clicks here.</summary>
        public void OnTileClicked(int index)
        {
            if (!_runner.IsBusy)
            {
                _runner.Submit(new SelectTileCommand(index));
            }
        }

        /// <summary>Button path. Same command the Enter key produces.</summary>
        public void OnAttackClicked()
        {
            if (!_runner.IsBusy)
            {
                _runner.Submit(new CastWordCommand());
            }
        }

        private void HandleLetterKeys(Keyboard keyboard)
        {
            for (var key = Key.A; key <= Key.Z; key++)
            {
                if (!keyboard[key].wasPressedThisFrame)
                {
                    continue;
                }

                var typed = key.ToString().ToUpperInvariant();
                var index = FindFirstUnselected(typed);

                if (index >= 0)
                {
                    _runner.Submit(new SelectTileCommand(index));
                }

                return;
            }
        }

        /// <summary>
        /// First matching unselected tile in reading order. Which duplicate a keypress
        /// claims is cosmetic — identical letters carry identical value — so the simplest
        /// predictable rule is the right one. Typing Q claims a "Qu" tile.
        /// </summary>
        private int FindFirstUnselected(string typedLetter)
        {
            var state = _runner.Engine.State;
            var wanted = typedLetter == "Q" ? "Qu" : typedLetter;

            for (var index = 0; index < TileGrid.Size; index++)
            {
                if (state.Selection.Contains(index))
                {
                    continue;
                }

                if (string.Equals(
                        state.Grid[index].Letter, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
