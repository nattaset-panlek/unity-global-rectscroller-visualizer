using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseTextView : MonoBehaviour, IVisualItem<string>
    {
        #region [Variables/Fields]
        [SerializeField] private Text _text;
        #endregion

        #region [Public Methods]
        public void Setup(Text text)
        {
            _text = text;
        }

        public void Bind(string data, VisualItemInfo info)
        {
            if (_text != null) _text.text = data;
        }

        public void Unbind()
        {
        }
        #endregion
    }
}
