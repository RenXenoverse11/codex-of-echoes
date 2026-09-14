using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodexOfEchoes.Game.View
{
    public sealed class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text telegraph;

        public void Set(int hp, int maxHp)
        {
            if (fill != null && maxHp > 0)
            {
                fill.fillAmount = Mathf.Clamp01((float)hp / maxHp);
            }

            if (label != null)
            {
                label.text = $"{hp} / {maxHp}";
            }
        }

        /// <summary>Feast warning. Shown a full turn before it lands.</summary>
        public void SetTelegraph(string message)
        {
            if (telegraph != null)
            {
                telegraph.text = message;
                telegraph.gameObject.SetActive(!string.IsNullOrEmpty(message));
            }
        }
    }
}
