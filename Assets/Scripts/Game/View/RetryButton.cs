using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CodexOfEchoes.Game.View
{
    /// <summary>Reloads the Battle scene. Self-wiring: no cross-component
    /// UnityEvent persistent listener needed in the scene file.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class RetryButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Retry);
        }

        private void Retry()
        {
            SceneManager.LoadScene("Battle");
        }
    }
}
