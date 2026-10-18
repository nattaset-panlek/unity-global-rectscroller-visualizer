using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseSegment
    {
        #region [Variables/Fields]
        private readonly List<Button> _buttons = new List<Button>();
        private Action<int> _onSelect;
        #endregion

        #region [Properties]
        public int Selected { get; private set; } = -1;
        public int Count => _buttons.Count;
        #endregion

        #region [Public Methods]
        public void Add(Button button)
        {
            _buttons.Add(button);
        }

        public void Setup(Action<int> onSelect, int selected)
        {
            _onSelect = onSelect;
            Select(selected, false);
        }

        public void Click(int index)
        {
            Select(index, true);
        }

        public void Select(int index, bool notify)
        {
            Selected = index;
            for (int i = 0; i < _buttons.Count; i++) Paint(_buttons[i], i == index);
            if (notify) _onSelect?.Invoke(index);
        }

        public void SetInteractable(int index, bool interactable)
        {
            if (index >= 0 && index < _buttons.Count) _buttons[index].interactable = interactable;
        }
        #endregion

        #region [Private Methods]
        private static void Paint(Button button, bool selected)
        {
            Image image = button.GetComponent<Image>();
            if (image != null) image.color = selected ? ShowcaseSkin.AccentColor : Color.white;
            Text text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.color = selected ? Color.white : ShowcaseSkin.TextColor;
                text.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }
        #endregion
    }
}
