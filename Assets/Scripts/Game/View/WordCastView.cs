using System.Collections;
using CodexOfEchoes.Core.Words;
using TMPro;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// The word materialises and flies at the enemy. Placeholder visuals; the seam that
    /// matters is that it receives a WordCastEvent and owns its own timing.
    /// </summary>
    public sealed class WordCastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text wordLabel;
        [SerializeField] private Transform origin;
        [SerializeField] private Transform target;
        [SerializeField] private float flightTime = 0.45f;
        [SerializeField] private Color plainColor = new Color(0.95f, 0.95f, 0.92f);
        [SerializeField] private Color echoColor = new Color(0.62f, 0.84f, 1f);
        [SerializeField] private Color baneColor = new Color(1f, 0.55f, 0.35f);

        private void Awake()
        {
            if (wordLabel != null)
            {
                wordLabel.gameObject.SetActive(false);
            }
        }

        public void Play(string word, StoryWordTier tier)
        {
            if (wordLabel == null || origin == null || target == null)
            {
                return;
            }

            StopAllCoroutines();
            StartCoroutine(Fly(word, tier));
        }

        private IEnumerator Fly(string word, StoryWordTier tier)
        {
            wordLabel.text = word;
            wordLabel.color = ColorFor(tier);
            wordLabel.gameObject.SetActive(true);

            var elapsed = 0f;
            while (elapsed < flightTime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / flightTime;
                wordLabel.transform.position =
                    Vector3.Lerp(origin.position, target.position, t);
                wordLabel.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.25f, t);
                yield return null;
            }

            wordLabel.gameObject.SetActive(false);
        }

        private Color ColorFor(StoryWordTier tier)
        {
            switch (tier)
            {
                case StoryWordTier.Bane: return baneColor;
                case StoryWordTier.Echo: return echoColor;
                default: return plainColor;
            }
        }
    }
}
