using CodexOfEchoes.Core.Grid;
using CodexOfEchoes.Game.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodexOfEchoes.Game.View
{
    /// <summary>One tile. Clicking it produces the same command a keypress would.</summary>
    public sealed class TileView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text letterLabel;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private Color normalColor = new Color(0.93f, 0.90f, 0.82f);
        [SerializeField] private Color selectedColor = new Color(0.98f, 0.78f, 0.35f);

        private TileInputRouter _router;

        public int Index { get; private set; }

        public void Bind(int index, LetterTile tile, TileInputRouter router)
        {
            Index = index;
            _router = router;

            if (letterLabel != null)
            {
                letterLabel.text = tile.Letter.ToUpperInvariant();
            }

            if (valueLabel != null)
            {
                valueLabel.text = tile.Value.ToString();
            }

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedColor : normalColor;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_router != null)
            {
                _router.OnTileClicked(Index);
            }
        }
    }
}
