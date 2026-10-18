using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseFactory
    {
        #region [Variables/Fields]
        public const float ControlHeight = 30f;
        private readonly ShowcaseSkin _skin;
        #endregion

        #region [Properties]
        public ShowcaseSkin Skin => _skin;
        #endregion

        #region [Public Methods]
        public ShowcaseFactory(ShowcaseSkin skin)
        {
            _skin = skin ?? new ShowcaseSkin();
        }

        public RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        public RectTransform Panel(Transform parent, string name, Color color)
        {
            RectTransform rect = Rect(parent, name);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = _skin.Background;
            image.type = Image.Type.Sliced;
            image.color = color;
            return rect;
        }

        public RectTransform Column(Transform parent, string name, float spacing = 6f, int padding = 0, Color? background = null)
        {
            RectTransform rect = background.HasValue ? Panel(parent, name, background.Value) : Rect(parent, name);
            VerticalLayoutGroup group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            return rect;
        }

        public RectTransform Row(Transform parent, string name, float spacing = 6f, int padding = 0, Color? background = null)
        {
            RectTransform rect = background.HasValue ? Panel(parent, name, background.Value) : Rect(parent, name);
            HorizontalLayoutGroup group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = TextAnchor.MiddleLeft;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            return rect;
        }

        public Text Label(Transform parent, string text, int size = 14, FontStyle style = FontStyle.Normal, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rect = Rect(parent, "Text");
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = _skin.Font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color ?? ShowcaseSkin.TextColor;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public Text Heading(Transform parent, string text, int size = 20)
        {
            return Label(parent, text, size, FontStyle.Bold);
        }

        public Button Button(Transform parent, string label, UnityAction onClick, float width = 0f)
        {
            GameObject go = DefaultControls.CreateButton(_skin.Resources);
            go.name = "Button " + label;
            go.transform.SetParent(parent, false);
            Text text = go.GetComponentInChildren<Text>(true);
            text.text = label;
            text.fontSize = 14;
            text.font = _skin.Font;
            Button button = go.GetComponent<Button>();
            if (onClick != null) button.onClick.AddListener(onClick);
            float preferred = width > 0f ? width : Mathf.Max(64f, text.preferredWidth + 28f);
            Layout(go, minWidth: preferred, preferredWidth: preferred, minHeight: ControlHeight, preferredHeight: ControlHeight);
            return button;
        }

        public Toggle Toggle(Transform parent, string label, bool value, UnityAction<bool> onChanged)
        {
            GameObject go = DefaultControls.CreateToggle(_skin.Resources);
            go.name = "Toggle " + label;
            go.transform.SetParent(parent, false);
            Text text = go.GetComponentInChildren<Text>(true);
            text.text = label;
            text.font = _skin.Font;
            text.fontSize = 14;
            text.color = ShowcaseSkin.TextColor;
            Toggle toggle = go.GetComponent<Toggle>();
            toggle.isOn = value;
            if (onChanged != null) toggle.onValueChanged.AddListener(onChanged);
            Layout(go, minHeight: 22f, preferredHeight: 22f, preferredWidth: 180f, flexibleWidth: 1f);
            return toggle;
        }

        public Slider Slider(Transform parent, float min, float max, float value, bool whole, UnityAction<float> onChanged)
        {
            GameObject go = DefaultControls.CreateSlider(_skin.Resources);
            go.transform.SetParent(parent, false);
            Slider slider = go.GetComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;
            slider.value = value;
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            Layout(go, minWidth: 80f, minHeight: 20f, preferredHeight: 20f, flexibleWidth: 1f);
            return slider;
        }

        public InputField Input(Transform parent, string placeholder, string value, InputField.ContentType type, UnityAction<string> onEndEdit, float width = 0f)
        {
            GameObject go = DefaultControls.CreateInputField(_skin.Resources);
            go.transform.SetParent(parent, false);
            InputField input = go.GetComponent<InputField>();
            foreach (Text text in go.GetComponentsInChildren<Text>(true))
            {
                text.font = _skin.Font;
                text.fontSize = 14;
            }
            ((Text)input.placeholder).text = placeholder;
            input.contentType = type;
            input.text = value;
            if (onEndEdit != null) input.onEndEdit.AddListener(onEndEdit);
            if (width > 0f) Layout(go, minWidth: width, preferredWidth: width, minHeight: ControlHeight, preferredHeight: ControlHeight);
            else Layout(go, minWidth: 80f, minHeight: ControlHeight, preferredHeight: ControlHeight, flexibleWidth: 1f);
            return input;
        }

        public ShowcaseSegment Segment(Transform parent, string[] labels, int selected, Action<int> onSelect)
        {
            RectTransform row = Row(parent, "Segment", 2f);
            var segment = new ShowcaseSegment();
            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                Button button = Button(row, labels[i], () => segment.Click(index));
                segment.Add(button);
            }
            segment.Setup(onSelect, selected);
            return segment;
        }

        public Text Code(Transform parent, string code)
        {
            RectTransform panel = Column(parent, "Code", 0f, 10, ShowcaseSkin.CodeColor);
            Text text = Label(panel, code, 13, FontStyle.Normal, ShowcaseSkin.CodeTextColor, TextAnchor.UpperLeft);
            text.font = _skin.CodeFont;
            text.supportRichText = false;
            return text;
        }

        public RectTransform Spacer(Transform parent, float size = 0f, bool vertical = false)
        {
            RectTransform rect = Rect(parent, "Spacer");
            if (size > 0f) Layout(rect.gameObject, minWidth: size, minHeight: size, preferredWidth: size, preferredHeight: size);
            else if (vertical) Layout(rect.gameObject, flexibleHeight: 1f);
            else Layout(rect.gameObject, flexibleWidth: 1f);
            return rect;
        }

        public Text RowLabel(Transform parent, string text, int size = 14, FontStyle style = FontStyle.Bold)
        {
            Text label = Label(parent, text, size, style);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = label.preferredWidth + 2f;
            Layout(label.gameObject, minWidth: width, preferredWidth: width);
            return label;
        }

        public RectTransform ScrollColumn(Transform parent, string name, float spacing, int padding)
        {
            ScrollRect scroll = CreateScroll(parent, name, false);
            RectTransform content = scroll.content;
            VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content;
        }

        public GlobalVisualizerView Visualizer(Transform parent, string name, VisualLayoutSettings layout)
        {
            bool horizontal = layout.Kind == VisualLayoutKind.Horizontal || (layout.Kind == VisualLayoutKind.Grid && layout.GridConstraint == VisualGridConstraint.FixedRows);
            ScrollRect scroll = CreateScroll(parent, name, horizontal);
            GlobalVisualizerView view = scroll.gameObject.AddComponent<GlobalVisualizerView>();
            view.SetLayout(layout);
            return view;
        }

        public ScrollRect NaiveScroll(Transform parent, string name, bool grid, Vector2 cell, float spacing, out RectTransform content)
        {
            ScrollRect scroll = CreateScroll(parent, name, false);
            content = scroll.content;
            if (grid)
            {
                GridLayoutGroup group = content.gameObject.AddComponent<GridLayoutGroup>();
                group.cellSize = cell;
                group.spacing = new Vector2(spacing, spacing);
            }
            else
            {
                VerticalLayoutGroup group = content.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = spacing;
                group.childControlWidth = true;
                group.childControlHeight = false;
                group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
            }
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return scroll;
        }

        public static LayoutElement Layout(GameObject go, float minWidth = -1f, float minHeight = -1f, float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            if (minWidth >= 0f) element.minWidth = minWidth;
            if (minHeight >= 0f) element.minHeight = minHeight;
            if (preferredWidth >= 0f) element.preferredWidth = preferredWidth;
            if (preferredHeight >= 0f) element.preferredHeight = preferredHeight;
            if (flexibleWidth >= 0f) element.flexibleWidth = flexibleWidth;
            if (flexibleHeight >= 0f) element.flexibleHeight = flexibleHeight;
            return element;
        }

        public static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
        #endregion

        #region [Private Methods]
        private ScrollRect CreateScroll(Transform parent, string name, bool horizontal)
        {
            GameObject go = DefaultControls.CreateScrollView(_skin.Resources);
            go.name = name;
            go.transform.SetParent(parent, false);
            ScrollRect scroll = go.GetComponent<ScrollRect>();
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontal = horizontal;
            scroll.vertical = !horizontal;

            RectTransform viewport = scroll.viewport;
            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null) UnityEngine.Object.DestroyImmediate(mask);
            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null) UnityEngine.Object.DestroyImmediate(viewportImage);
            viewport.gameObject.AddComponent<RectMask2D>();

            Scrollbar unused = horizontal ? scroll.verticalScrollbar : scroll.horizontalScrollbar;
            if (unused != null) UnityEngine.Object.DestroyImmediate(unused.gameObject);
            if (horizontal) scroll.verticalScrollbar = null;
            else scroll.horizontalScrollbar = null;
            Scrollbar used = horizontal ? scroll.horizontalScrollbar : scroll.verticalScrollbar;
            if (horizontal) scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            else scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            if (used != null)
            {
                RectTransform bar = (RectTransform)used.transform;
                if (horizontal) viewport.offsetMin = new Vector2(viewport.offsetMin.x, bar.sizeDelta.y);
                else viewport.offsetMax = new Vector2(-bar.sizeDelta.x, viewport.offsetMax.y);
            }
            Layout(go, minWidth: 120f, minHeight: 80f, flexibleWidth: 1f, flexibleHeight: 1f);
            return scroll;
        }
        #endregion
    }
}
