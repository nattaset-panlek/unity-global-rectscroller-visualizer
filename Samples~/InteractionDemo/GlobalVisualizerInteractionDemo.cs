using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples
{
    [AddComponentMenu("Global Visualizer/Samples/Interaction Demo")]
    public sealed class GlobalVisualizerInteractionDemo : MonoBehaviour
    {
        #region [Variables/Fields]
        private static readonly string[] Words =
        {
            "Fire", "Ice", "Projectile", "Area", "Chain", "Critical Strike", "Duration", "Mana Leech",
            "Knockback", "Echo", "Cascade", "Pierce", "Holy", "Ghost", "Summon", "Aura", "Channel", "Toggle"
        };

        [SerializeField, Min(1)] private int _rowCount = 2000;
        [SerializeField, Min(1)] private int _tagCount = 300;

        private readonly List<string> _tags = new List<string>();
        private readonly List<int> _rows = new List<int>();
        private Font _font;
        private Text _status;
        private GlobalVisualizerView _list;
        private GlobalVisualizerView _flow;
        #endregion

        #region [Unity Methods]
        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < _rowCount; i++) _rows.Add(i);
            for (int i = 0; i < _tagCount; i++) _tags.Add(Words[i % Words.Length] + (i >= Words.Length ? " " + (i / Words.Length + 1) : string.Empty));

            RectTransform canvas = CreateCanvas();
            EnsureEventSystem();

            _status = CreateText(canvas, "Status", 22, TextAnchor.UpperLeft);
            Place(_status.rectTransform, new Vector2(40f, -20f), new Vector2(1800f, 40f));

            _list = CreateView(canvas, "Selectable List", new Vector2(40f, -80f), new Vector2(560f, 900f), VisualLayoutSettings.Vertical(48f, 4f));
            _list.ItemPrefab = CreateRowTemplate((RectTransform)_list.transform);
            _list.EmptyPrefab = CreateEmptyTemplate((RectTransform)_list.transform);
            _list.SelectionMode = VisualSelectionMode.Multi;
            _list.SnapToItems = true;
            _list.SizeMode = VisualSizeMode.Provider;
            _list.SizeProvider = index => new Vector2(0f, _rows[index] % 5 == 0 ? 88f : 48f);
            _list.SetItems(_rows);
            _list.OnItemClicked += (index, data) => Show($"Clicked row {_rows[index]}. Shift = range, Ctrl = add. Arrow keys or a gamepad move the focus.");
            _list.OnItemDoubleClicked += (index, data) => Show($"Double-clicked row {_rows[index]}");
            _list.OnItemRightClicked += (index, data) => Show($"Right-clicked row {_rows[index]}: open your context menu here");
            _list.OnItemSubmitted += index => Show($"Submitted row {_rows[index]}");
            _list.OnSelectionChanged += () => Show($"{_list.SelectedCount} rows selected");

            _flow = CreateView(canvas, "Tag Flow", new Vector2(640f, -80f), new Vector2(1240f, 760f), VisualLayoutSettings.Flow(8f, new Vector2(100f, 40f)));
            _flow.ItemPrefab = CreateTagTemplate((RectTransform)_flow.transform);
            _flow.SelectionMode = VisualSelectionMode.Single;
            _flow.SizeMode = VisualSizeMode.Measure;
            _flow.SetItems(_tags);
            _flow.OnSelectionChanged += () => Show(_flow.SelectedIndex >= 0 ? $"Tag: {_tags[_flow.SelectedIndex]}" : "No tag selected");

            CreateButton(canvas, "Empty / Fill", new Vector2(640f, -870f), ToggleRows);
            CreateButton(canvas, "Jump to row 1000", new Vector2(900f, -870f), () => _list.ScrollTo(1000, 0.5f));
            CreateButton(canvas, "Focus list", new Vector2(1160f, -870f), () => _list.Focus(_list.VisibleRange.IsEmpty ? 0 : _list.VisibleRange.First));
            Show("Click, Shift+click, Ctrl+click, double-click or right-click rows. Tags are measured from their text.");
        }
        #endregion

        #region [Private Methods]
        private void ToggleRows()
        {
            if (_rows.Count > 0) _rows.Clear();
            else for (int i = 0; i < _rowCount; i++) _rows.Add(i);
            _list.NotifyReset();
            Show(_rows.Count == 0 ? "The list is empty, so the placeholder is shown." : $"{_rows.Count} rows");
        }

        private void Show(string message)
        {
            if (_status != null) _status.text = message;
        }

        private GlobalVisualizerView CreateView(RectTransform parent, string name, Vector2 position, Vector2 size, VisualLayoutSettings layout)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(parent, false);
            Place(rootRect, position, size);
            root.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(rootRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(6f, 6f);
            viewportRect.offsetMax = new Vector2(-6f, -6f);

            var content = new GameObject("Content", typeof(RectTransform));
            ((RectTransform)content.transform).SetParent(viewportRect, false);

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = (RectTransform)content.transform;
            scroll.scrollSensitivity = 40f;

            var view = root.AddComponent<GlobalVisualizerView>();
            view.SetLayout(layout);
            return view;
        }

        private RectTransform CreateRowTemplate(RectTransform parent)
        {
            var go = new GameObject("Row Template", typeof(RectTransform), typeof(Image), typeof(DemoRowView));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(540f, 48f);
            Text label = CreateText(rect, "Label", 20, TextAnchor.MiddleLeft);
            label.color = new Color(0.1f, 0.1f, 0.1f);
            Stretch(label.rectTransform, 16f);
            go.GetComponent<DemoRowView>().Setup(label, go.GetComponent<Image>());
            go.SetActive(false);
            return rect;
        }

        private RectTransform CreateTagTemplate(RectTransform parent)
        {
            var go = new GameObject("Tag Template", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(DemoTagView));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(100f, 40f);
            HorizontalLayoutGroup group = go.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(16, 16, 8, 8);
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
            Text label = CreateText(rect, "Label", 20, TextAnchor.MiddleCenter);
            label.color = new Color(0.1f, 0.1f, 0.1f);
            go.GetComponent<DemoTagView>().Setup(label, go.GetComponent<Image>());
            go.SetActive(false);
            return rect;
        }

        private RectTransform CreateEmptyTemplate(RectTransform parent)
        {
            var go = new GameObject("Empty Template", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect, 0f);
            Text label = CreateText(rect, "Label", 24, TextAnchor.MiddleCenter);
            label.text = "Nothing here. Press Empty / Fill.";
            label.color = new Color(0.7f, 0.7f, 0.7f);
            Stretch(label.rectTransform, 12f);
            go.SetActive(false);
            return rect;
        }

        private void CreateButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Place(rect, position, new Vector2(240f, 48f));
            go.GetComponent<Button>().onClick.AddListener(onClick);
            Text text = CreateText(rect, "Label", 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.1f, 0.1f, 0.1f);
            Stretch(text.rectTransform, 4f);
        }

        private RectTransform CreateCanvas()
        {
            var go = new GameObject("[Demo] Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return (RectTransform)go.transform;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("[Demo] EventSystem", typeof(EventSystem));
#if GV_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private Text CreateText(RectTransform parent, string name, int size, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, 0f);
            rect.offsetMax = new Vector2(-inset, 0f);
        }

        private static void Place(RectTransform rect, Vector2 topLeft, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
        }
        #endregion
    }
}
