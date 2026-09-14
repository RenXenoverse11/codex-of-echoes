using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// The Spine seam. Exactly five calls, matching Liora's five planned animations.
    /// The slice ships PlaceholderCombatantView; SpineCombatantView will implement the
    /// same five against a SkeletonAnimation, and nothing upstream changes.
    /// </summary>
    public abstract class CombatantView : MonoBehaviour
    {
        public abstract void PlayIdle();

        public abstract void PlayCast();

        public abstract void PlayHurt();

        public abstract void PlayVictory();

        public abstract void PlayDefeat();
    }
}
