using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseSelfTestPage : ShowcasePage
    {
        #region [Variables/Fields]
        private readonly List<ShowcaseCheckResult> _results = new List<ShowcaseCheckResult>();
        private readonly List<GameObject> _rows = new List<GameObject>();
        private ShowcaseSelfTests _tests;
        private RectTransform _list;
        private RectTransform _sandbox;
        private Text _summary;
        private Button _run;
        private bool _running;
        #endregion

        #region [Properties]
        public override string Title => "Self Test";
        public bool IsRunning => _running;
        public IReadOnlyList<ShowcaseCheckResult> Results => _results;
        public int PassedCount { get; private set; }
        #endregion

        #region [Public Methods]
        public Coroutine Run()
        {
            if (_running) return null;
            return StartCoroutine(RunAll());
        }
        #endregion

        #region [Private Methods]
        protected override void Build(RectTransform root)
        {
            RectTransform page = Factory.Row(root, "Self Test", 12f, 12);
            ShowcaseFactory.Stretch(page);
            HorizontalLayoutGroup group = page.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.UpperLeft;
            group.childForceExpandHeight = true;

            RectTransform left = Factory.Column(page, "Results Panel", 6f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(left.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);
            Factory.Heading(left, "Self Test", 20);
            Factory.Label(left, "Each check builds a real Global Visualizer in the sandbox on the right, drives it from code and compares the result with what the package promises. Run it in the Editor and in your player builds (IL2CPP, mobile) to be sure the guarantees hold on your target.", 13);
            RectTransform bar = Factory.Row(left, "Run Bar", 8f);
            _run = Factory.Button(bar, "Run all checks", () => Run(), 150f);
            _summary = Factory.Label(bar, "Not run yet.", 14, FontStyle.Bold);
            _list = Factory.ScrollColumn(left, "Results", 4f, 4);

            RectTransform right = Factory.Column(page, "Sandbox Panel", 6f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(right.gameObject, minWidth: 340f, preferredWidth: 340f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Heading(right, "Sandbox", 16);
            Factory.Label(right, "The list each check is using. It is replaced for every check.", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);
            _sandbox = Factory.Panel(right, "Sandbox", Color.white);
            ShowcaseFactory.Layout(_sandbox.gameObject, minWidth: 320f, preferredWidth: 320f, minHeight: 420f, preferredHeight: 420f);
            _tests = new ShowcaseSelfTests(Factory, Templates, _sandbox);
        }

        private IEnumerator RunAll()
        {
            _running = true;
            _run.interactable = false;
            _results.Clear();
            for (int i = 0; i < _rows.Count; i++) Destroy(_rows[i]);
            _rows.Clear();
            PassedCount = 0;
            Canvas.ForceUpdateCanvases();
            yield return null;

            float total = 0f;
            for (int i = 0; i < _tests.Checks.Count; i++)
            {
                _summary.text = $"Running {i + 1} of {_tests.Checks.Count}...";
                yield return null;
                ShowcaseCheckResult result = _tests.Run(i);
                _results.Add(result);
                total += result.Milliseconds;
                if (result.Passed) PassedCount++;
                AddRow(result);
            }

            bool all = PassedCount == _tests.Checks.Count;
            _summary.text = $"{PassedCount} of {_tests.Checks.Count} passed in {total:0} ms";
            _summary.color = all ? ShowcaseSkin.GoodColor : ShowcaseSkin.BadColor;
            _run.interactable = true;
            _running = false;
        }

        private void AddRow(ShowcaseCheckResult result)
        {
            RectTransform row = Factory.Row(_list, result.Name, 10f, 8, Color.white);
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            _rows.Add(row.gameObject);
            Text badge = Factory.Label(row, result.Passed ? "PASS" : "FAIL", 14, FontStyle.Bold, result.Passed ? ShowcaseSkin.GoodColor : ShowcaseSkin.BadColor);
            ShowcaseFactory.Layout(badge.gameObject, minWidth: 46f, preferredWidth: 46f);
            RectTransform texts = Factory.Column(row, "Texts", 1f);
            ShowcaseFactory.Layout(texts.gameObject, flexibleWidth: 1f);
            Factory.Label(texts, result.Name, 14, FontStyle.Bold);
            if (!string.IsNullOrEmpty(result.Proves)) Factory.Label(texts, result.Proves, 12, FontStyle.Italic, ShowcaseSkin.MutedColor);
            Factory.Label(texts, result.Detail, 12);
            Text time = Factory.Label(row, $"{result.Milliseconds:0.0} ms", 12, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.UpperRight);
            ShowcaseFactory.Layout(time.gameObject, minWidth: 64f, preferredWidth: 64f);
        }
        #endregion
    }
}
