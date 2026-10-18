using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseRangeBar : MonoBehaviour
    {
        #region [Variables/Fields]
        private RectTransform _window;
        private Text _label;
        #endregion

        #region [Public Methods]
        public static ShowcaseRangeBar Create(ShowcaseFactory factory, Transform parent)
        {
            RectTransform column = factory.Column(parent, "Range Bar", 2f);
            RectTransform track = factory.Panel(column, "Track", new Color(0.70f, 0.70f, 0.70f));
            ShowcaseFactory.Layout(track.gameObject, minHeight: 12f, preferredHeight: 12f, flexibleWidth: 1f);
            RectTransform window = factory.Panel(track, "Alive Window", ShowcaseSkin.AccentColor);
            window.anchorMin = new Vector2(0f, 0f);
            window.anchorMax = new Vector2(0f, 1f);
            window.offsetMin = Vector2.zero;
            window.offsetMax = Vector2.zero;
            Text label = factory.Label(column, "", 11, FontStyle.Normal, ShowcaseSkin.MutedColor);
            ShowcaseRangeBar bar = column.gameObject.AddComponent<ShowcaseRangeBar>();
            bar._window = window;
            bar._label = label;
            return bar;
        }

        public void Show(VisualRange range, int count)
        {
            if (_window == null) return;
            if (count <= 0 || range.IsEmpty)
            {
                _window.anchorMin = new Vector2(0f, 0f);
                _window.anchorMax = new Vector2(0f, 1f);
                if (_label != null) _label.text = count <= 0 ? "No items" : "Nothing alive";
                return;
            }
            float min = (float)range.First / count;
            float max = Mathf.Max(min + 0.004f, (float)(range.Last + 1) / count);
            _window.anchorMin = new Vector2(min, 0f);
            _window.anchorMax = new Vector2(Mathf.Min(1f, max), 1f);
            if (_label != null) _label.text = $"Blue = items that have a GameObject right now: #{range.First} to #{range.Last} of {count:N0}";
        }
        #endregion
    }
}
