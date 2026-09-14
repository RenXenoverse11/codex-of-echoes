using System;
using System.Collections;
using System.Collections.Generic;
using CodexOfEchoes.Core.Battle;
using UnityEngine;

namespace CodexOfEchoes.Game.Battle
{
    /// <summary>
    /// Plays events in order with delays between them. Every bit of timing in the game
    /// lives here — the engine is instantaneous, which is what makes the rules testable
    /// without waiting on anything.
    /// </summary>
    public sealed class BattlePresenter : MonoBehaviour
    {
        [Header("Beat lengths (seconds)")]
        [SerializeField] private float instantBeat = 0.02f;
        [SerializeField] private float castBeat = 0.55f;
        [SerializeField] private float damageBeat = 0.35f;
        [SerializeField] private float tileBeat = 0.25f;
        [SerializeField] private float endBeat = 0.8f;

        private readonly Queue<BattleEvent> _pending = new Queue<BattleEvent>();
        private Coroutine _drain;

        /// <summary>Raised per event as it plays, so views can react in sequence.</summary>
        public event Action<BattleEvent> EventPlayed;

        public bool IsDraining => _drain != null;

        public void Enqueue(IReadOnlyList<BattleEvent> events)
        {
            if (events == null)
            {
                return;
            }

            foreach (var battleEvent in events)
            {
                _pending.Enqueue(battleEvent);
            }

            if (_drain == null && isActiveAndEnabled)
            {
                _drain = StartCoroutine(Drain());
            }
        }

        private IEnumerator Drain()
        {
            while (_pending.Count > 0)
            {
                var battleEvent = _pending.Dequeue();

                EventPlayed?.Invoke(battleEvent);

                yield return new WaitForSeconds(BeatFor(battleEvent));
            }

            _drain = null;
        }

        private float BeatFor(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case WordCastEvent _:
                    return castBeat;
                case DamageDealtEvent _:
                case HealedEvent _:
                case EnemyActedEvent _:
                    return damageBeat;
                case TilesConsumedEvent _:
                case TilesFellEvent _:
                case TilesSpawnedEvent _:
                case GridScrambledEvent _:
                    return tileBeat;
                case BattleEndedEvent _:
                    return endBeat;
                default:
                    return instantBeat;
            }
        }
    }
}
