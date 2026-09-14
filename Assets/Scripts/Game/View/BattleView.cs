using System.Collections.Generic;
using System.Linq;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Core.Combat;
using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Core.Words;
using CodexOfEchoes.Game.Battle;
using CodexOfEchoes.Game.Input;
using TMPro;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// Turns the event stream into visuals. Subscribes to BattleRunner and reacts one
    /// event at a time, which is why the order the engine emits them in matters.
    /// </summary>
    public sealed class BattleView : MonoBehaviour
    {
        [SerializeField] private BattleRunner runner;
        [SerializeField] private TileInputRouter router;
        [SerializeField] private Transform tileParent;
        [SerializeField] private TileView tilePrefab;
        [SerializeField] private CombatantView lioraView;
        [SerializeField] private CombatantView enemyView;
        [SerializeField] private HealthBarView lioraHealth;
        [SerializeField] private HealthBarView enemyHealth;
        [SerializeField] private WordCastView wordCast;
        [SerializeField] private TMP_Text currentWordLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private GameObject rewrittenPagePanel;
        [SerializeField] private GameObject retryButton;

        private readonly List<TileView> _tiles = new List<TileView>();

        private void Start()
        {
            BuildGrid();
            RefreshAll();

            runner.EventPlayed += OnEvent;
            lioraView.PlayIdle();
            enemyView.PlayIdle();

            if (rewrittenPagePanel != null)
            {
                rewrittenPagePanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (runner != null)
            {
                runner.EventPlayed -= OnEvent;
            }
        }

        private void BuildGrid()
        {
            var state = runner.Engine.State;

            for (var index = 0; index < TileGrid.Size; index++)
            {
                var tile = Instantiate(tilePrefab, tileParent);
                tile.Bind(index, state.Grid[index], router);
                _tiles.Add(tile);
            }
        }

        private void OnEvent(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case TileSelectedEvent selected:
                    _tiles[selected.Index].SetSelected(true);
                    RefreshCurrentWord();
                    break;

                case TileDeselectedEvent deselected:
                    _tiles[deselected.Index].SetSelected(false);
                    RefreshCurrentWord();
                    break;

                case SelectionClearedEvent _:
                    foreach (var tile in _tiles)
                    {
                        tile.SetSelected(false);
                    }

                    RefreshCurrentWord();
                    break;

                case WordRejectedEvent rejected:
                    ShowMessage(rejected.Reason == WordRejectionReason.TooShort
                        ? "Too short."
                        : $"\"{rejected.Word}\" isn't a word.");
                    break;

                case WordCastEvent cast:
                    lioraView.PlayCast();
                    wordCast.Play(cast.Word, cast.Tier);
                    ShowMessage(MessageFor(cast));
                    break;

                case DamageDealtEvent damage:
                    ApplyDamage(damage);
                    break;

                case HealedEvent healed:
                    enemyHealth.Set(healed.NewHp, runner.Engine.State.Enemy.MaxHp);
                    ShowMessage(
                        $"{runner.Engine.State.Enemy.Name} recovers {healed.Amount} HP.");
                    break;

                case TilesFellEvent _:
                case TilesSpawnedEvent _:
                case GridScrambledEvent _:
                    RefreshTiles();
                    break;

                case EnemyTelegraphedEvent telegraphed:
                    enemyHealth.SetTelegraph(
                        $"{telegraphed.Ability.ToString().ToUpperInvariant()} NEXT TURN");
                    break;

                case EnemyActedEvent _:
                    enemyHealth.SetTelegraph(string.Empty);
                    enemyView.PlayCast();
                    break;

                case BattleEndedEvent ended:
                    ShowEnding(ended.Outcome);
                    break;
            }
        }

        private void ApplyDamage(DamageDealtEvent damage)
        {
            var state = runner.Engine.State;

            if (damage.Target == CombatantId.Enemy)
            {
                enemyHealth.Set(damage.NewHp, state.Enemy.MaxHp);
                enemyView.PlayHurt();
            }
            else
            {
                lioraHealth.Set(damage.NewHp, state.Player.MaxHp);
                lioraView.PlayHurt();
            }
        }

        private void ShowEnding(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Victory)
            {
                lioraView.PlayVictory();
                enemyView.PlayDefeat();
                ShowMessage("The myth is yours to rewrite.");

                // Seam for the real narrative payoff.
                if (rewrittenPagePanel != null)
                {
                    rewrittenPagePanel.SetActive(true);
                }
            }
            else
            {
                lioraView.PlayDefeat();
                enemyView.PlayVictory();
                ShowMessage("The Silence takes the page.");
            }

            if (retryButton != null)
            {
                retryButton.SetActive(true);
            }
        }

        private string MessageFor(WordCastEvent cast)
        {
            switch (cast.Tier)
            {
                case StoryWordTier.Bane:
                    return $"{cast.Word}! It burns. {cast.Damage} damage.";
                case StoryWordTier.Echo:
                    return $"{cast.Word} — the story answers. {cast.Damage} damage.";
                default:
                    return $"{cast.Word}: {cast.Damage} damage.";
            }
        }

        private void RefreshAll()
        {
            var state = runner.Engine.State;
            lioraHealth.Set(state.Player.Hp, state.Player.MaxHp);
            enemyHealth.Set(state.Enemy.Hp, state.Enemy.MaxHp);
            enemyHealth.SetTelegraph(string.Empty);
            RefreshCurrentWord();
        }

        private void RefreshTiles()
        {
            var state = runner.Engine.State;

            for (var index = 0; index < _tiles.Count; index++)
            {
                _tiles[index].Bind(index, state.Grid[index], router);
                _tiles[index].SetSelected(state.Selection.Contains(index));
            }
        }

        private void RefreshCurrentWord()
        {
            if (currentWordLabel != null)
            {
                currentWordLabel.text = runner.Engine.State.CurrentWord;
            }
        }

        private void ShowMessage(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.text = message;
            }
        }
    }
}
