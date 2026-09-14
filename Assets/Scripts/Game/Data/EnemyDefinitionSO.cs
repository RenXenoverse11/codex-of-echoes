using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>
    /// Authorable enemy stats. Converted to plain Core values at load — the rules never
    /// see a Unity type.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Enemy_", menuName = "Codex of Echoes/Enemy Definition", order = 0)]
    public sealed class EnemyDefinitionSO : ScriptableObject
    {
        [SerializeField] private string enemyName = "Aswang";
        [SerializeField] private int maxHp = 75;

        [Tooltip("Folklore weaknesses. Bane words deal x2.5 damage against this enemy.")]
        [SerializeField]
        private string[] baneWords =
        {
            "SALT", "GARLIC", "ASH", "OIL", "STING", "SPINE",
        };

        public string EnemyName => enemyName;

        public int MaxHp => maxHp;

        public string[] BaneWords => baneWords;
    }
}
