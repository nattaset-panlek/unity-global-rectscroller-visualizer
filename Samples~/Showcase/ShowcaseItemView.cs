using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseItemView : MonoBehaviour, IVisualItem<ShowcaseItem>, IVisualItemState
    {
        #region [Variables/Fields]
        [SerializeField] private Image _background;
        [SerializeField] private Image _swatch;
        [SerializeField] private Text _title;
        [SerializeField] private Text _subtitle;
        [SerializeField] private Text _index;
        [SerializeField] private Text _note;
        [SerializeField] private Text _tag;
        [SerializeField] private Outline _focus;
        #endregion

        #region [Properties]
        public ShowcaseItem Data { get; private set; }
        #endregion

        #region [Public Methods]
        public void Setup(Image background, Image swatch, Text title, Text subtitle, Text index, Text note, Text tag, Outline focus)
        {
            _background = background;
            _swatch = swatch;
            _title = title;
            _subtitle = subtitle;
            _index = index;
            _note = note;
            _tag = tag;
            _focus = focus;
        }

        public void Bind(ShowcaseItem data, VisualItemInfo info)
        {
            Data = data;
            if (_title != null) _title.text = data.Title;
            if (_subtitle != null) _subtitle.text = data.Subtitle;
            if (_index != null) _index.text = data.IndexLabel;
            if (_note != null) _note.text = data.Note;
            if (_tag != null) _tag.text = data.Tag;
            if (_swatch != null) _swatch.color = data.Color;
            OnStateChanged(info);
        }

        public void Unbind()
        {
            Data = null;
        }

        public void OnStateChanged(VisualItemInfo info)
        {
            if (_background != null) _background.color = info.IsSelected ? ShowcaseSkin.SelectedColor : Color.white;
            if (_focus != null) _focus.enabled = info.IsFocused;
        }
        #endregion
    }
}
