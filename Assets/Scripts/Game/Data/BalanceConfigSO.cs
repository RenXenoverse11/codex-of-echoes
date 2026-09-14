using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>Player-side tuning. Combat constants live in Core; this is what a
    /// designer adjusts without a recompile.</summary>
    [CreateAssetMenu(
        fileName = "BalanceConfig", menuName = "Codex of Echoes/Balance Config", order = 2)]
    public sealed class BalanceConfigSO : ScriptableObject
    {
        [SerializeField] private int playerMaxHp = 100;

        [Tooltip("Leave at 0 to seed each battle from the clock.")]
        [SerializeField] private int fixedSeed;

        public int PlayerMaxHp => playerMaxHp;

        public int FixedSeed => fixedSeed;
    }
}
