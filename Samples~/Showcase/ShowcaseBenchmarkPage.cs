using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseBenchmarkPage : ShowcasePage
    {
        #region [Variables/Fields]
        private const int ScrollFrames = 180;
        private const int WarmupFrames = 3;
        private static readonly int[] Counts = { 1000, 5000, 10000, 25000 };
        private readonly List<GameObject> _rows = new List<GameObject>();
        private RectTransform _naiveHost;
        private RectTransform _visualHost;
        private RectTransform _results;
        private ShowcaseNaiveList _naive;
        private GlobalVisualizerView _visual;
        private Text _naiveInfo;
        private Text _visualInfo;
        private Text _status;
        private Text _verdict;
        private Text _footnote;
        private Button _run;
        private Button _build;
        private Button _clear;
        private int _count = 5000;
        private bool _grid;
        private bool _running;
        #endregion

        #region [Properties]
        public override string Title => "Benchmark";
        public bool IsRunning => _running;
        public ShowcaseBenchmarkResult LastNaive { get; private set; }
        public ShowcaseBenchmarkResult LastVisual { get; private set; }
        #endregion

        #region [Public Methods]
        public override void OnHidden()
        {
            if (_running) return;
            ClearBoth();
        }

        public void SetCount(int count)
        {
            _count = Mathf.Max(1, count);
        }

        public Coroutine Run()
        {
            if (_running) return null;
            return StartCoroutine(RunBenchmark());
        }
        #endregion

        #region [Private Methods]
        protected override void Build(RectTransform root)
        {
            RectTransform page = Factory.Column(root, "Benchmark", 8f, 12);
            ShowcaseFactory.Stretch(page);

            RectTransform intro = Factory.Column(page, "Intro", 4f);
            Factory.Heading(intro, "Naive ScrollRect vs Global Visualizer", 20);
            Factory.Label(intro,
                "<b>Naive</b> is the usual setup: ScrollRect + LayoutGroup + ContentSizeFitter, one GameObject per item. After a change it destroys every child and instantiates them all again, which is what most inventories do.  " +
                "<b>Global Visualizer</b> gets the same items and the same item prefab. Both are measured one after the other, alone on screen, with vSync off. Lower is better everywhere.", 13);

            RectTransform controls = Factory.Row(page, "Controls", 8f);
            Factory.RowLabel(controls, "Items");
            Factory.Segment(controls, new[] { "1,000", "5,000", "10,000", "25,000" }, 1, index => _count = Counts[index]);
            Factory.RowLabel(controls, "Layout");
            Factory.Segment(controls, new[] { "List", "Grid" }, 0, index =>
            {
                _grid = index == 1;
                ClearBoth();
                CreateViews();
            });
            _run = Factory.Button(controls, "Run benchmark", () => Run(), 140f);
            _build = Factory.Button(controls, "Build both (scroll them yourself)", BuildBoth);
            _clear = Factory.Button(controls, "Clear", ClearBoth, 70f);
            _status = Factory.Label(controls, "Pick a size and press Run benchmark.", 13, FontStyle.Italic, ShowcaseSkin.MutedColor);
            ShowcaseFactory.Layout(_status.gameObject, preferredWidth: 200f, flexibleWidth: 1f);

            RectTransform body = Factory.Row(page, "Body", 10f);
            ShowcaseFactory.Layout(body.gameObject, flexibleHeight: 1f);
            HorizontalLayoutGroup bodyGroup = body.GetComponent<HorizontalLayoutGroup>();
            bodyGroup.childAlignment = TextAnchor.UpperLeft;
            bodyGroup.childForceExpandHeight = true;

            _naiveHost = Factory.Column(body, "Naive Panel", 4f, 8, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(_naiveHost.gameObject, minWidth: 250f, preferredWidth: 250f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Label(_naiveHost, "Naive", 15, FontStyle.Bold, ShowcaseSkin.NaiveColor);
            _naiveInfo = Factory.Label(_naiveHost, "", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);

            _visualHost = Factory.Column(body, "Visualizer Panel", 4f, 8, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(_visualHost.gameObject, minWidth: 250f, preferredWidth: 250f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Label(_visualHost, "Global Visualizer", 15, FontStyle.Bold, ShowcaseSkin.AccentColor);
            _visualInfo = Factory.Label(_visualHost, "", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);

            RectTransform report = Factory.Column(body, "Report", 4f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(report.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);
            Factory.Heading(report, "Results", 18);
            _verdict = Factory.Label(report, "No run yet.", 14, FontStyle.Bold);
            _results = Factory.Column(report, "Rows", 3f);
            _footnote = Factory.Label(report, "", 11, FontStyle.Italic, ShowcaseSkin.MutedColor);
            Factory.Spacer(report, 0f, true);
            Factory.Label(report, Environment(), 11, FontStyle.Normal, ShowcaseSkin.MutedColor);

            CreateViews();
        }

        private void CreateViews()
        {
            if (_naive != null) Destroy(_naive.Scroll.gameObject);
            if (_visual != null) Destroy(_visual.gameObject);
            Vector2 cell = new Vector2(ShowcaseTemplates.CellSize, ShowcaseTemplates.CellSize);
            RectTransform template = _grid ? Templates.Cell : Templates.Row;
            _naive = new ShowcaseNaiveList(Factory, _naiveHost, template, _grid, cell, 4f);
            VisualLayoutSettings layout = _grid ? VisualLayoutSettings.Grid(cell, 4f) : VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f);
            _visual = Factory.Visualizer(_visualHost, _grid ? "Visualizer Grid" : "Visualizer List", layout);
            _visual.ItemPrefab = template;
            _visual.HandleInput = false;
            _naive.Scroll.transform.SetSiblingIndex(1);
            _visual.transform.SetSiblingIndex(1);
            UpdateInfo();
        }

        private void BuildBoth()
        {
            if (_running) return;
            ClearBoth();
            List<ShowcaseItem> items = ShowcaseData.Create(_count);
            var watch = Stopwatch.StartNew();
            _naive.Build(items);
            Canvas.ForceUpdateCanvases();
            float naiveMs = watch.ElapsedMilliseconds;
            watch.Restart();
            _visual.SetItems(new List<ShowcaseItem>(items));
            Canvas.ForceUpdateCanvases();
            float visualMs = watch.ElapsedMilliseconds;
            _status.text = $"Built {_count:N0} items: naive {naiveMs:0} ms, visualizer {visualMs:0} ms. Scroll both and watch the FPS in the header.";
            UpdateInfo();
        }

        private void ClearBoth()
        {
            if (_naive != null) _naive.Clear();
            if (_visual != null) _visual.Clear();
            UpdateInfo();
        }

        private IEnumerator RunBenchmark()
        {
            _running = true;
            SetButtons(false);
            int oldVSync = QualitySettings.vSyncCount;
            int oldTarget = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            if (App.FpsMeter != null) App.FpsMeter.enabled = false;

            ClearBoth();
            List<ShowcaseItem> source = ShowcaseData.Create(_count);
            ShowcaseItem extra = ShowcaseData.CreateNext();
            yield return Warmup(source);

            var naive = new ShowcaseBenchmarkResult("Naive");
            _visualHost.gameObject.SetActive(false);
            _status.text = $"Naive: building {_count:N0} items...";
            yield return null;
            yield return MeasureNaive(new List<ShowcaseItem>(source), extra, naive);
            _naive.Clear();
            _visualHost.gameObject.SetActive(true);

            var visual = new ShowcaseBenchmarkResult("Global Visualizer");
            _naiveHost.gameObject.SetActive(false);
            _status.text = $"Global Visualizer: building {_count:N0} items...";
            yield return null;
            yield return MeasureVisual(new List<ShowcaseItem>(source), extra, visual);
            _naiveHost.gameObject.SetActive(true);

            QualitySettings.vSyncCount = oldVSync;
            Application.targetFrameRate = oldTarget;
            if (App.FpsMeter != null) App.FpsMeter.enabled = true;
            LastNaive = naive;
            LastVisual = visual;
            ShowReport(naive, visual);
            _status.text = $"Done: {_count:N0} {(_grid ? "cells" : "rows")}. The visualizer list is still filled; press Build both to scroll them side by side.";
            UpdateInfo();
            SetButtons(true);
            _running = false;
        }

        private IEnumerator Warmup(List<ShowcaseItem> source)
        {
            _status.text = "Warming up...";
            List<ShowcaseItem> small = source.GetRange(0, Mathf.Min(50, source.Count));
            _naive.Build(small);
            _visual.SetItems(new List<ShowcaseItem>(small));
            Canvas.ForceUpdateCanvases();
            yield return null;
            _naive.Clear();
            _visual.Clear();
            yield return null;
            ShowcaseMetrics.Collect();
        }

        private IEnumerator MeasureNaive(List<ShowcaseItem> items, ShowcaseItem extra, ShowcaseBenchmarkResult result)
        {
            ShowcaseMetrics.Collect();
            long memory = ShowcaseMetrics.TotalMemory();
            long garbage = ShowcaseMetrics.ManagedBytes();
            int collections = ShowcaseMetrics.Collections();
            var watch = Stopwatch.StartNew();
            _naive.Build(items);
            Canvas.ForceUpdateCanvases();
            result.BuildMs = (float)watch.Elapsed.TotalMilliseconds;
            result.BuildGarbageKb = Mathf.Max(0f, (ShowcaseMetrics.ManagedBytes() - garbage) / 1024f);
            result.BuildCollected = ShowcaseMetrics.Collections() != collections;
            ShowcaseMetrics.Collect();
            result.MemoryMb = Mathf.Max(0f, (ShowcaseMetrics.TotalMemory() - memory) / (1024f * 1024f));
            result.Objects = _naive.ObjectCount;
            UpdateInfo();

            _status.text = "Naive: scrolling top to bottom...";
            yield return ScrollTest(_naive.Scroll, result);

            _status.text = "Naive: inserting one item...";
            yield return null;
            items.Insert(0, extra);
            watch.Restart();
            _naive.RebuildAll(items);
            Canvas.ForceUpdateCanvases();
            result.RebuildMs = (float)watch.Elapsed.TotalMilliseconds;
            yield return null;
            yield return null;
            watch.Restart();
            _naive.InsertFirst(extra);
            Canvas.ForceUpdateCanvases();
            result.InsertOneMs = (float)watch.Elapsed.TotalMilliseconds;
            yield return null;
        }

        private IEnumerator MeasureVisual(List<ShowcaseItem> items, ShowcaseItem extra, ShowcaseBenchmarkResult result)
        {
            ShowcaseMetrics.Collect();
            long memory = ShowcaseMetrics.TotalMemory();
            long garbage = ShowcaseMetrics.ManagedBytes();
            int collections = ShowcaseMetrics.Collections();
            var watch = Stopwatch.StartNew();
            _visual.SetItems(items);
            Canvas.ForceUpdateCanvases();
            result.BuildMs = (float)watch.Elapsed.TotalMilliseconds;
            result.BuildGarbageKb = Mathf.Max(0f, (ShowcaseMetrics.ManagedBytes() - garbage) / 1024f);
            result.BuildCollected = ShowcaseMetrics.Collections() != collections;
            ShowcaseMetrics.Collect();
            result.MemoryMb = Mathf.Max(0f, (ShowcaseMetrics.TotalMemory() - memory) / (1024f * 1024f));
            result.Objects = _visual.Content.GetComponentsInChildren<Transform>(true).Length - 1;
            UpdateInfo();

            _status.text = "Global Visualizer: scrolling top to bottom...";
            yield return ScrollTest(_visual.ScrollRect, result);
            result.Objects = Mathf.Max(result.Objects, _visual.Content.GetComponentsInChildren<Transform>(true).Length - 1);

            _status.text = "Global Visualizer: inserting one item...";
            yield return null;
            watch.Restart();
            items.Insert(0, extra);
            _visual.NotifyInserted(0, 1);
            Canvas.ForceUpdateCanvases();
            result.RebuildMs = (float)watch.Elapsed.TotalMilliseconds;
            result.InsertOneMs = result.RebuildMs;
            yield return null;
        }

        private IEnumerator ScrollTest(ScrollRect scroll, ShowcaseBenchmarkResult result)
        {
            scroll.verticalNormalizedPosition = 1f;
            for (int i = 0; i < WarmupFrames; i++) yield return null;
            double total = 0d;
            float worst = 0f;
            for (int frame = 0; frame < ScrollFrames; frame++)
            {
                scroll.verticalNormalizedPosition = 1f - (frame + 1f) / ScrollFrames;
                yield return null;
                float delta = Time.unscaledDeltaTime * 1000f;
                total += delta;
                if (delta > worst) worst = delta;
            }
            result.AverageFrameMs = (float)(total / ScrollFrames);
            result.WorstFrameMs = worst;
        }

        private void ShowReport(ShowcaseBenchmarkResult naive, ShowcaseBenchmarkResult visual)
        {
            for (int i = 0; i < _rows.Count; i++) Destroy(_rows[i]);
            _rows.Clear();
            Header();
            Row("Build (create + first layout)", naive.BuildMs, visual.BuildMs, "0.0", " ms");
            Row("GameObjects in the list", naive.Objects, visual.Objects, "N0", "");
            Row("Memory added (native + managed)", naive.MemoryMb, visual.MemoryMb, "0.0", " MB");
            Row("Managed heap growth while building" + Star(naive.BuildCollected || visual.BuildCollected), naive.BuildGarbageKb, visual.BuildGarbageKb, "0", " KB");
            Row("Average frame while scrolling", naive.AverageFrameMs, visual.AverageFrameMs, "0.00", " ms");
            Row("Worst frame while scrolling", naive.WorstFrameMs, visual.WorstFrameMs, "0.0", " ms");
            Row("Insert 1 item: rebuild the list", naive.RebuildMs, visual.RebuildMs, "0.00", " ms");
            Row("Insert 1 item: best-case naive", naive.InsertOneMs, visual.InsertOneMs, "0.00", " ms");

            bool collected = naive.BuildCollected || visual.BuildCollected;
            _footnote.text = "Heap growth is counted in 4 KB pages (GC.GetTotalMemory)." + (collected ? "  * A garbage collection ran during that measurement, so growth is under-reported." : "") +
                             "  Garbage while scrolling is not shown here: in the Editor every frame allocates for its own reasons. The Self Test measures the list's scrolling code alone (0 bytes).";
            float speed = visual.BuildMs > 0.01f ? naive.BuildMs / visual.BuildMs : 0f;
            _verdict.text = speed >= 1f
                ? $"Global Visualizer built {_count:N0} items {speed:0.#}x faster with {naive.Objects / Mathf.Max(1f, visual.Objects):0.#}x fewer GameObjects."
                : $"At {_count:N0} items the naive list built faster ({1f / Mathf.Max(0.0001f, speed):0.#}x). Try a bigger list.";
        }

        private static string Star(bool collected)
        {
            return collected ? " *" : "";
        }

        private void Header()
        {
            RectTransform row = Factory.Row(_results, "Header Row", 6f);
            _rows.Add(row.gameObject);
            Cell(row, "Metric", 250f, FontStyle.Bold, ShowcaseSkin.TextColor);
            Cell(row, "Naive", 90f, FontStyle.Bold, ShowcaseSkin.NaiveColor);
            Cell(row, "Visualizer", 90f, FontStyle.Bold, ShowcaseSkin.AccentColor);
            Cell(row, "Result", 150f, FontStyle.Bold, ShowcaseSkin.TextColor);
        }

        private void Row(string label, float naive, float visual, string format, string unit)
        {
            RectTransform row = Factory.Row(_results, label, 6f);
            _rows.Add(row.gameObject);
            Cell(row, label, 250f, FontStyle.Normal, ShowcaseSkin.TextColor);
            Cell(row, naive.ToString(format) + unit, 90f, FontStyle.Normal, ShowcaseSkin.TextColor);
            Cell(row, visual.ToString(format) + unit, 90f, FontStyle.Normal, ShowcaseSkin.TextColor);
            string verdict;
            Color color;
            if (Mathf.Abs(naive - visual) <= Mathf.Max(0.05f, 0.05f * Mathf.Max(naive, visual)))
            {
                verdict = "about the same";
                color = ShowcaseSkin.MutedColor;
            }
            else if (visual < naive)
            {
                verdict = visual <= 0.0001f ? "visualizer: zero" : $"{naive / visual:0.#}x better";
                color = ShowcaseSkin.GoodColor;
            }
            else
            {
                verdict = naive <= 0.0001f ? "naive: zero" : $"naive {visual / naive:0.#}x better";
                color = ShowcaseSkin.BadColor;
            }
            Cell(row, verdict, 150f, FontStyle.Bold, color);
            Bars(row, naive, visual);
        }

        private void Cell(RectTransform row, string text, float width, FontStyle style, Color color)
        {
            Text label = Factory.Label(row, text, 12, style, color);
            ShowcaseFactory.Layout(label.gameObject, minWidth: width, preferredWidth: width);
        }

        private void Bars(RectTransform row, float naive, float visual)
        {
            RectTransform area = Factory.Rect(row, "Bars");
            ShowcaseFactory.Layout(area.gameObject, minWidth: 60f, minHeight: 18f, preferredHeight: 18f, flexibleWidth: 1f);
            float max = Mathf.Max(0.0001f, Mathf.Max(naive, visual));
            Bar(area, naive / max, ShowcaseSkin.NaiveColor, 0.5f, 1f);
            Bar(area, visual / max, ShowcaseSkin.AccentColor, 0f, 0.5f);
        }

        private void Bar(RectTransform area, float fraction, Color color, float bottom, float top)
        {
            RectTransform bar = Factory.Rect(area, "Bar");
            bar.gameObject.AddComponent<Image>().color = color;
            bar.anchorMin = new Vector2(0f, bottom);
            bar.anchorMax = new Vector2(Mathf.Clamp(fraction, 0.004f, 1f), top);
            bar.offsetMin = new Vector2(0f, 1f);
            bar.offsetMax = new Vector2(0f, -1f);
        }

        private void UpdateInfo()
        {
            if (_naiveInfo == null || _naive == null || _visual == null) return;
            _naiveInfo.text = $"{_naive.ItemCount:N0} items  ·  {_naive.ObjectCount:N0} GameObjects";
            int objects = _visual.Content.GetComponentsInChildren<Transform>(true).Length - 1;
            _visualInfo.text = $"{_visual.Count:N0} items  ·  {objects:N0} GameObjects";
        }

        private void SetButtons(bool interactable)
        {
            _run.interactable = interactable;
            _build.interactable = interactable;
            _clear.interactable = interactable;
        }

        private static string Environment()
        {
            string where = Application.isEditor ? "Editor (Play mode: slower than a build, compare the ratios)" : "Player build";
            return $"{where}  ·  Unity {Application.unityVersion}  ·  {SystemInfo.processorType}  ·  {SystemInfo.graphicsDeviceName}";
        }
        #endregion
    }
}
