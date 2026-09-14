using System;
using CodexOfEchoes.Core.Battle;
using CodexOfEchoes.Game.Data;
using UnityEngine;

namespace CodexOfEchoes.Game.Battle
{
    /// <summary>
    /// Owns the engine and forwards commands into it. Input is refused while the
    /// presenter is still animating, so the player can never act on a board that is
    /// mid-fall.
    /// </summary>
    [RequireComponent(typeof(BattlePresenter))]
    public sealed class BattleRunner : MonoBehaviour
    {
        [SerializeField] private EnemyDefinitionSO enemy;
        [SerializeField] private StoryWordSetSO storyWords;
        [SerializeField] private BalanceConfigSO balance;

        private BattlePresenter _presenter;

        public BattleEngine Engine { get; private set; }

        /// <summary>True while animations are still playing.</summary>
        public bool IsBusy => _presenter != null && _presenter.IsDraining;

        public event Action<BattleEvent> EventPlayed;

        private void Awake()
        {
            _presenter = GetComponent<BattlePresenter>();
            _presenter.EventPlayed += OnEventPlayed;

            RequireAsset(enemy, nameof(enemy));
            RequireAsset(storyWords, nameof(storyWords));
            RequireAsset(balance, nameof(balance));

            var dictionary = DictionaryLoader.LoadFromStreamingAssets();
            var seed = balance.FixedSeed != 0
                ? balance.FixedSeed
                : Environment.TickCount;

            Engine = new BattleEngine(BattleSetupFactory.Create(
                dictionary, enemy, storyWords, balance, seed));

            Debug.Log($"[Codex] Battle seed {seed}. Replay this fight with it.");
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.EventPlayed -= OnEventPlayed;
            }
        }

        public void Submit(BattleCommand command)
        {
            if (Engine == null || IsBusy)
            {
                return;
            }

            _presenter.Enqueue(Engine.Execute(command));
        }

        private void OnEventPlayed(BattleEvent battleEvent) => EventPlayed?.Invoke(battleEvent);

        /// <summary>
        /// An unassigned asset is a developer error. Fail at load with the field name
        /// rather than throwing a NullReferenceException on the first keystroke.
        /// </summary>
        private void RequireAsset(UnityEngine.Object asset, string fieldName)
        {
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(BattleRunner)} on '{name}' has no '{fieldName}' assigned. "
                    + "Assign it in the Inspector.");
            }
        }
    }
}
