using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples
{
    public sealed class DemoTagView : MonoBehaviour, IVisualItem<string>, IVisualItemState
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

        public void Bind(string data, VisualItemInfo info)
        {
            if (_label != null) _label.text = data;
        }

        public void Unbind()
        {
        }

        public void OnStateChanged(VisualItemInfo info)
        {
            if (_background == null) return;
            if (info.IsSelected) _background.color = new Color(0.36f, 0.62f, 0.95f);
            else _background.color = info.IsFocused ? new Color(0.8f, 0.86f, 0.95f) : new Color(0.9f, 0.9f, 0.9f);
        }
        #endregion
    }
}
