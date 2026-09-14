using UnityEngine;

namespace CodexOfEchoes.Game.Data
{
    /// <summary>Chapter-wide Echo words. x1.5 damage anywhere in the chapter.</summary>
    [CreateAssetMenu(
        fileName = "StoryWords_", menuName = "Codex of Echoes/Story Word Set", order = 1)]
    public sealed class StoryWordSetSO : ScriptableObject
    {
        [SerializeField]
        private string[] echoWords =
        {
            "NAME", "TRUTH", "LIGHT", "DAWN", "STORY", "INK", "PAGE",
        };

        public string[] EchoWords => echoWords;
    }
}
