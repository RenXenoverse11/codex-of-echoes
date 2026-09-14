using CodexOfEchoes.Game.Input;
using UnityEngine;
using UnityEngine.UI;

namespace CodexOfEchoes.Game.View
{
    /// <summary>Casts the current word. Self-wiring: finds the scene's TileInputRouter
    /// rather than needing an Inspector-wired reference.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class AttackButton : MonoBehaviour
    {
        private void Awake()
        {
            var router = FindFirstObjectByType<TileInputRouter>();
            GetComponent<Button>().onClick.AddListener(router.OnAttackClicked);
        }
    }
}
