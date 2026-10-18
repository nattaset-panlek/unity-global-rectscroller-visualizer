using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Global Visualizer/Samples/Showcase")]
    public sealed class ShowcaseApp : MonoBehaviour
    {
        #region [Variables/Fields]
        public const string Version = "1.0.0";
        public const string Author = "Axnise";
        public const string ProductName = "Global Visualizer";

        [SerializeField] private ShowcaseSkin _skin = new ShowcaseSkin();
        [SerializeField, Min(0)] private int _startPage;

        public event Action<int> OnPageChanged;

        private readonly List<ShowcasePage> _pages = new List<ShowcasePage>();
        private readonly List<Button> _tabs = new List<Button>();
        private ShowcaseFactory _factory;
        private ShowcaseTemplates _templates;
        private Canvas _canvas;
        private RectTransform _pageHost;
        private ShowcaseFpsMeter _fps;
        private int _current = -1;
        #endregion

        #region [Properties]
        public ShowcaseSkin Skin => _skin;
        public ShowcaseFactory Factory => _factory;
        public ShowcaseTemplates Templates => _templates;
        public Canvas Canvas => _canvas;
        public ShowcaseFpsMeter FpsMeter => _fps;
        public bool IsBuilt => _canvas != null;
        public int PageCount => _pages.Count;
        public int CurrentPage => _current;
        #endregion

        #region [Unity Methods]
        private void Start()
        {
            Build();
        }
        #endregion

        #region [Public Methods]
        public void Build()
        {
            if (IsBuilt) return;
            _factory = new ShowcaseFactory(_skin);
            EnsureEventSystem();
            _canvas = CreateCanvas();
            RectTransform root = (RectTransform)_canvas.transform;
            _templates = new ShowcaseTemplates(_factory, root);

            RectTransform background = _factory.Rect(root, "Background");
            ShowcaseFactory.Stretch(background);
            background.gameObject.AddComponent<Image>().color = ShowcaseSkin.PageColor;

            RectTransform frame = _factory.Column(root, "Frame", 0f);
            ShowcaseFactory.Stretch(frame);
            BuildHeader(frame);

            _pageHost = _factory.Rect(frame, "Pages");
            ShowcaseFactory.Layout(_pageHost.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);

            AddPage<ShowcaseWelcomePage>();
            AddPage<ShowcaseTutorialPage>();
            AddPage<ShowcasePlaygroundPage>();
            AddPage<ShowcaseBenchmarkPage>();
            AddPage<ShowcaseSelfTestPage>();
            ShowPage(Mathf.Clamp(_startPage, 0, _pages.Count - 1));
        }

        public void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Count || index == _current) return;
            if (_current >= 0)
            {
                _pages[_current].OnHidden();
                _pages[_current].gameObject.SetActive(false);
            }
            _current = index;
            _pages[index].gameObject.SetActive(true);
            _pages[index].OnShown();
            for (int i = 0; i < _tabs.Count; i++) PaintTab(_tabs[i], i == index);
            OnPageChanged?.Invoke(index);
        }

        public void ShowPage<T>() where T : ShowcasePage
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] is T) ShowPage(i);
            }
        }

        public T GetPage<T>() where T : ShowcasePage
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] is T page) return page;
            }
            return null;
        }
        #endregion

        #region [Private Methods]
        private Canvas CreateCanvas()
        {
            var go = new GameObject("[Showcase] Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private void BuildHeader(RectTransform frame)
        {
            RectTransform header = _factory.Row(frame, "Header", 8f, 0, ShowcaseSkin.HeaderColor);
            header.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(14, 14, 8, 8);
            header.GetComponent<Image>().sprite = null;
            ShowcaseFactory.Layout(header.gameObject, minHeight: 48f, preferredHeight: 48f, flexibleHeight: 0f);

            RectTransform titles = _factory.Column(header, "Titles", 0f);
            _factory.Label(titles, ProductName + " Showcase", 18, FontStyle.Bold, Color.white);
            _factory.Label(titles, "v" + Version + "  ·  by " + Author, 11, FontStyle.Normal, new Color(0.75f, 0.75f, 0.75f));
            ShowcaseFactory.Layout(titles.gameObject, minWidth: 250f, preferredWidth: 250f);

            string[] names = { "Welcome", "Learn", "Playground", "Benchmark", "Self Test" };
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                _tabs.Add(_factory.Button(header, names[i], () => ShowPage(index), 104f));
            }
            _factory.Spacer(header);
            Text fps = _factory.Label(header, "", 12, FontStyle.Normal, Color.white, TextAnchor.MiddleRight);
            ShowcaseFactory.Layout(fps.gameObject, minWidth: 250f, preferredWidth: 250f);
            _fps = header.gameObject.AddComponent<ShowcaseFpsMeter>();
            _fps.Setup(fps);
        }

        private void AddPage<T>() where T : ShowcasePage
        {
            RectTransform host = _factory.Rect(_pageHost, typeof(T).Name);
            ShowcaseFactory.Stretch(host);
            T page = host.gameObject.AddComponent<T>();
            page.Initialize(this);
            host.gameObject.SetActive(false);
            _pages.Add(page);
        }

        private static void PaintTab(Button tab, bool selected)
        {
            tab.GetComponent<Image>().color = selected ? ShowcaseSkin.AccentColor : Color.white;
            Text text = tab.GetComponentInChildren<Text>(true);
            text.color = selected ? Color.white : ShowcaseSkin.TextColor;
            text.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("[Showcase] EventSystem", typeof(EventSystem));
#if GV_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }
        #endregion
    }
}
