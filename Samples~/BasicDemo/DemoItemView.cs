using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples
{
    public sealed class DemoItemView : MonoBehaviour, IVisualItem<int>
    {
        #region [Variables/Fields]
        [SerializeField] private Text _label;
        [SerializeField] private Image _background;
        private string[] _labels;
        #endregion

        #region [Public Methods]
        public void Setup(Text label, Image background, string[] labels)
        {
            _label = label;
            _background = background;
            _labels = labels;
        }

        public void Bind(int data, VisualItemInfo info)
        {
            if (_label != null && _labels != null) _label.text = _labels[data];
            if (_background != null) _background.color = data % 2 == 0 ? new Color(0.92f, 0.92f, 0.92f) : new Color(0.85f, 0.85f, 0.85f);
        }

        public void Unbind()
        {
        }
        #endregion
    }
}
