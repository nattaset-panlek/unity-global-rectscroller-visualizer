using System;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    [Serializable]
    public sealed class ShowcaseSkin
    {
        #region [Variables/Fields]
        public static readonly Color PageColor = new Color(0.80f, 0.80f, 0.80f);
        public static readonly Color HeaderColor = new Color(0.22f, 0.22f, 0.22f);
        public static readonly Color PanelColor = new Color(0.95f, 0.95f, 0.95f);
        public static readonly Color TextColor = new Color(0.196f, 0.196f, 0.196f);
        public static readonly Color MutedColor = new Color(0.42f, 0.42f, 0.42f);
        public static readonly Color AccentColor = new Color(0.20f, 0.45f, 0.85f);
        public static readonly Color SelectedColor = new Color(0.74f, 0.84f, 1f);
        public static readonly Color FocusColor = new Color(0.20f, 0.45f, 0.85f);
        public static readonly Color GoodColor = new Color(0.16f, 0.58f, 0.27f);
        public static readonly Color BadColor = new Color(0.80f, 0.22f, 0.20f);
        public static readonly Color WarnColor = new Color(0.85f, 0.55f, 0.10f);
        public static readonly Color NaiveColor = new Color(0.85f, 0.45f, 0.25f);
        public static readonly Color CodeColor = new Color(0.16f, 0.17f, 0.20f);
        public static readonly Color CodeTextColor = new Color(0.86f, 0.89f, 0.93f);

        [SerializeField] private Sprite _standard;
        [SerializeField] private Sprite _background;
        [SerializeField] private Sprite _inputField;
        [SerializeField] private Sprite _knob;
        [SerializeField] private Sprite _checkmark;
        [SerializeField] private Sprite _dropdown;
        [SerializeField] private Sprite _mask;

        private Font _font;
        private Font _codeFont;
        #endregion

        #region [Properties]
        public Sprite Standard => _standard;
        public Sprite Background => _background;

        public Font Font
        {
            get
            {
                if (_font == null) _font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public Font CodeFont
        {
            get
            {
                if (_codeFont != null) return _codeFont;
                try
                {
                    _codeFont = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Menlo", "Courier New", "Liberation Mono", "DejaVu Sans Mono" }, 13);
                }
                catch (Exception)
                {
                    _codeFont = null;
                }
                if (_codeFont == null) _codeFont = Font;
                return _codeFont;
            }
        }

        public DefaultControls.Resources Resources => new DefaultControls.Resources
        {
            standard = _standard,
            background = _background,
            inputField = _inputField,
            knob = _knob,
            checkmark = _checkmark,
            dropdown = _dropdown,
            mask = _mask
        };
        #endregion

        #region [Public Methods]
        public void SetSprites(Sprite standard, Sprite background, Sprite inputField, Sprite knob, Sprite checkmark, Sprite dropdown, Sprite mask)
        {
            _standard = standard;
            _background = background;
            _inputField = inputField;
            _knob = knob;
            _checkmark = checkmark;
            _dropdown = dropdown;
            _mask = mask;
        }
        #endregion
    }
}
