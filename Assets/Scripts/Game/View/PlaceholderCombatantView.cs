using System.Collections;
using UnityEngine;

namespace CodexOfEchoes.Game.View
{
    /// <summary>
    /// A colored rectangle that nudges and flashes. Exists only to make the five seam
    /// calls visible during the slice; every line of it is expected to be deleted when
    /// Spine assets arrive.
    /// </summary>
    public sealed class PlaceholderCombatantView : CombatantView
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Color idleColor = new Color(0.35f, 0.42f, 0.65f);
        [SerializeField] private Color hurtColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private float nudgeDistance = 0.35f;
        [SerializeField] private float beat = 0.18f;

        private Vector3 _home;
        private Coroutine _running;

        private void Awake()
        {
            _home = transform.localPosition;

            if (body == null)
            {
                body = GetComponentInChildren<SpriteRenderer>();
            }
        }

        public override void PlayIdle()
        {
            Restart(Tint(idleColor));
        }

        public override void PlayCast()
        {
            Restart(Nudge(nudgeDistance));
        }

        public override void PlayHurt()
        {
            Restart(Flash(hurtColor));
        }

        public override void PlayVictory()
        {
            Restart(Nudge(nudgeDistance * 0.5f));
        }

        public override void PlayDefeat()
        {
            Restart(Fade());
        }

        private void Restart(IEnumerator routine)
        {
            if (_running != null)
            {
                StopCoroutine(_running);
                transform.localPosition = _home;
            }

            _running = StartCoroutine(routine);
        }

        private IEnumerator Tint(Color color)
        {
            if (body != null)
            {
                body.color = color;
            }

            yield break;
        }

        private IEnumerator Nudge(float distance)
        {
            var target = _home + new Vector3(distance, 0f, 0f);

            yield return Lerp(_home, target, beat);
            yield return Lerp(target, _home, beat);

            transform.localPosition = _home;
            _running = null;
        }

        private IEnumerator Flash(Color color)
        {
            if (body == null)
            {
                yield break;
            }

            body.color = color;
            yield return new WaitForSeconds(beat);
            body.color = idleColor;
            _running = null;
        }

        private IEnumerator Fade()
        {
            if (body == null)
            {
                yield break;
            }

            var elapsed = 0f;
            var start = body.color;

            while (elapsed < beat * 4f)
            {
                elapsed += Time.deltaTime;
                var alpha = Mathf.Lerp(start.a, 0.15f, elapsed / (beat * 4f));
                body.color = new Color(start.r, start.g, start.b, alpha);
                yield return null;
            }

            _running = null;
        }

        private IEnumerator Lerp(Vector3 from, Vector3 to, float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            transform.localPosition = to;
        }
    }
}
