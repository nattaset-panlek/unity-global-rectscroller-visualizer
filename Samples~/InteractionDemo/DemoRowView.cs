using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples
{
    public sealed class DemoRowView : MonoBehaviour, IVisualItem<int>, IVisualItemState
    {
        #region [Variables/Fields]
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;
        #endregion

        #region [Public Methods]
        public void Setup(Text label, Image background)
        {
            _label = label;
            _background = background;
        }

        public void Bind(int data, VisualItemInfo info)
        {
            if (_label != null) _label.text = data % 5 == 0 ? $"Row {data}  (tall row from the size provider)" : $"Row {data}";
        }

        public void Unbind()
        {
        }

        public void OnStateChanged(VisualItemInfo info)
        {
            if (_background == null) return;
            Color normal = info.Index % 2 == 0 ? new Color(0.92f, 0.92f, 0.92f) : new Color(0.86f, 0.86f, 0.86f);
            if (info.IsSelected) _background.color = new Color(0.36f, 0.62f, 0.95f);
            else _background.color = info.IsFocused ? new Color(0.8f, 0.86f, 0.95f) : normal;
        }
        #endregion
    }
}
