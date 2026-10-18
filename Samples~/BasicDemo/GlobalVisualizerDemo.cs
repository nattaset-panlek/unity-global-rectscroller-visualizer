using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples
{
    [AddComponentMenu("Global Visualizer/Samples/Basic Demo")]
    public sealed class GlobalVisualizerDemo : MonoBehaviour
    {
        #region [Variables/Fields]
        [SerializeField, Min(1)] private int _verticalCount = 10000;
        [SerializeField, Min(1)] private int _horizontalCount = 500;
        [SerializeField, Min(1)] private int _gridCount = 2000;

        private Font _font;
        private string[] _labels;
        private Text _stats;
        private GlobalVisualizerView _vertical;
        #endregion

        #region [Unity Methods]
        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            int max = Mathf.Max(_verticalCount, Mathf.Max(_horizontalCount, _gridCount));
            _labels = new string[max];
            for (int i = 0; i < max; i++) _labels[i] = "Item " + (i + 1).ToString("00000");

            RectTransform canvas = CreateCanvas();
            EnsureEventSystem();

            _stats = CreateText(canvas, "Stats", 22, TextAnchor.UpperLeft);
            Place(_stats.rectTransform, new Vector2(40f, -20f), new Vector2(1200f, 40f));

            _vertical = CreateVisualizer(canvas, "Vertical List", new Vector2(40f, -80f), new Vector2(500f, 900f), VisualLayoutSettings.Vertical(56f, 6f), new Vector2(500f, 56f));
            _vertical.SetCount(_verticalCount);
            _vertical.OnVisibleRangeChanged += OnVerticalRangeChanged;

            GlobalVisualizerView horizontal = CreateVisualizer(canvas, "Horizontal List", new Vector2(580f, -80f), new Vector2(1280f, 140f), VisualLayoutSettings.Horizontal(180f, 8f), new Vector2(180f, 140f));
            horizontal.SetCount(_horizontalCount);

            GlobalVisualizerView grid = CreateVisualizer(canvas, "Grid", new Vector2(580f, -260f), new Vector2(1280f, 720f), VisualLayoutSettings.Grid(new Vector2(120f, 120f), 10f), new Vector2(120f, 120f));
            grid.SetCount(_gridCount);

            OnVerticalRangeChanged(_vertical.VisibleRange);
        }

        private void OnDestroy()
        {
            if (_vertical != null) _vertical.OnVisibleRangeChanged -= OnVerticalRangeChanged;
        }
        #endregion

        #region [Private Methods]
        private void OnVerticalRangeChanged(VisualRange range)
        {
            if (_stats == null) return;
            _stats.text = $"Vertical: {_verticalCount:N0} items, {_vertical.ActiveViewCount} views alive, {_vertical.CreatedViewCount} created, visible {range}";
        }

        private RectTransform CreateCanvas()
        {
            var go = new GameObject("[Demo] Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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

        private GlobalVisualizerView CreateVisualizer(RectTransform parent, string name, Vector2 position, Vector2 size, VisualLayoutSettings layout, Vector2 itemSize)
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
            view.ItemPrefab = CreateItemTemplate(rootRect, itemSize);
            view.SetLayout(layout);
            return view;
        }

        private RectTransform CreateItemTemplate(RectTransform parent, Vector2 size)
        {
            var go = new GameObject("Item Template", typeof(RectTransform), typeof(Image), typeof(DemoItemView));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            Text label = CreateText(rect, "Label", 20, TextAnchor.MiddleCenter);
            label.color = new Color(0.1f, 0.1f, 0.1f);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            go.GetComponent<DemoItemView>().Setup(label, go.GetComponent<Image>(), _labels);
            go.SetActive(false);
            return rect;
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
