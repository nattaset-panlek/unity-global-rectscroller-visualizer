using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseTemplates
    {
        #region [Variables/Fields]
        public const float RowHeight = 44f;
        public const float CellSize = 96f;
        private readonly ShowcaseFactory _factory;
        #endregion

        #region [Properties]
        public RectTransform Root { get; }
        public RectTransform Row { get; }
        public RectTransform Cell { get; }
        public RectTransform Tag { get; }
        public RectTransform Note { get; }
        public RectTransform LogLine { get; }
        public RectTransform Empty { get; }
        #endregion

        #region [Public Methods]
        public ShowcaseTemplates(ShowcaseFactory factory, Transform parent)
        {
            _factory = factory;
            Root = factory.Rect(parent, "[Templates]");
            Root.gameObject.SetActive(false);
            Row = CreateRow();
            Cell = CreateCell();
            Tag = CreateTag();
            Note = CreateNote();
            LogLine = CreateLogLine();
            Empty = CreateEmpty();
        }
        #endregion

        #region [Private Methods]
        private RectTransform CreateRow()
        {
            RectTransform root = Card("Row Template", new Vector2(360f, RowHeight), out Image background, out Outline focus);
            Image swatch = Swatch(root, new Vector2(8f, 0f), new Vector2(28f, 28f), new Vector2(0f, 0.5f));
            Text title = Label(root, 14, FontStyle.Bold, ShowcaseSkin.TextColor, TextAnchor.LowerLeft);
            Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(44f, 0f), new Vector2(-70f, -2f));
            Text subtitle = Label(root, 12, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.UpperLeft);
            Place(subtitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(44f, 2f), new Vector2(-70f, 0f));
            Text index = Label(root, 12, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.MiddleRight);
            Place(index.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-66f, 0f), new Vector2(-10f, 0f));
            root.gameObject.AddComponent<ShowcaseItemView>().Setup(background, swatch, title, subtitle, index, null, null, focus);
            return root;
        }

        private RectTransform CreateCell()
        {
            RectTransform root = Card("Cell Template", new Vector2(CellSize, CellSize), out Image background, out Outline focus);
            Image swatch = Swatch(root, new Vector2(0f, -12f), new Vector2(36f, 36f), new Vector2(0.5f, 1f));
            Text title = Label(root, 12, FontStyle.Bold, ShowcaseSkin.TextColor, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 14f), new Vector2(-4f, 46f));
            Text index = Label(root, 11, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.LowerCenter);
            Place(index.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(4f, 2f), new Vector2(-4f, 16f));
            root.gameObject.AddComponent<ShowcaseItemView>().Setup(background, swatch, title, null, index, null, null, focus);
            return root;
        }

        private RectTransform CreateTag()
        {
            RectTransform root = Card("Tag Template", new Vector2(100f, 32f), out Image background, out Outline focus);
            HorizontalLayoutGroup group = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(10, 12, 6, 6);
            group.spacing = 6f;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Image swatch = _factory.Rect(root, "Dot").gameObject.AddComponent<Image>();
            swatch.sprite = _factory.Skin.Standard;
            ShowcaseFactory.Layout(swatch.gameObject, minWidth: 10f, minHeight: 10f, preferredWidth: 10f, preferredHeight: 10f);
            Text tag = Label(root, 14, FontStyle.Normal, ShowcaseSkin.TextColor, TextAnchor.MiddleLeft);
            tag.horizontalOverflow = HorizontalWrapMode.Overflow;
            root.gameObject.AddComponent<ShowcaseItemView>().Setup(background, swatch, null, null, null, null, tag, focus);
            return root;
        }

        private RectTransform CreateNote()
        {
            RectTransform root = Card("Note Template", new Vector2(360f, 60f), out Image background, out Outline focus);
            VerticalLayoutGroup group = root.gameObject.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(12, 12, 8, 10);
            group.spacing = 2f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            Text title = Label(root, 14, FontStyle.Bold, ShowcaseSkin.TextColor, TextAnchor.UpperLeft);
            Text note = Label(root, 13, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.UpperLeft);
            root.gameObject.AddComponent<ShowcaseItemView>().Setup(background, null, title, null, null, note, null, focus);
            return root;
        }

        private RectTransform CreateLogLine()
        {
            RectTransform root = _factory.Rect(Root, "Log Line Template");
            root.sizeDelta = new Vector2(300f, 20f);
            Text text = Label(root, 12, FontStyle.Normal, ShowcaseSkin.TextColor, TextAnchor.MiddleLeft);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(6f, 0f), new Vector2(-6f, 0f));
            root.gameObject.AddComponent<ShowcaseTextView>().Setup(text);
            return root;
        }

        private RectTransform CreateEmpty()
        {
            RectTransform root = _factory.Rect(Root, "Empty Template");
            ShowcaseFactory.Stretch(root);
            Text text = Label(root, 16, FontStyle.Italic, ShowcaseSkin.MutedColor, TextAnchor.MiddleCenter);
            text.text = "Nothing here yet.\nThis is the Empty Prefab: it shows while the list is empty.";
            Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));
            return root;
        }

        private RectTransform Card(string name, Vector2 size, out Image background, out Outline focus)
        {
            RectTransform root = _factory.Rect(Root, name);
            root.sizeDelta = size;
            background = root.gameObject.AddComponent<Image>();
            background.sprite = _factory.Skin.Standard;
            background.type = Image.Type.Sliced;
            focus = root.gameObject.AddComponent<Outline>();
            focus.effectColor = ShowcaseSkin.FocusColor;
            focus.effectDistance = new Vector2(2f, -2f);
            focus.enabled = false;
            return root;
        }

        private Image Swatch(RectTransform parent, Vector2 position, Vector2 size, Vector2 anchor)
        {
            Image image = _factory.Rect(parent, "Swatch").gameObject.AddComponent<Image>();
            image.sprite = _factory.Skin.Standard;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return image;
        }

        private Text Label(RectTransform parent, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            return _factory.Label(parent, "", size, style, color, anchor);
        }

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
        #endregion
    }
}
