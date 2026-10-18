using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseWelcomePage : ShowcasePage
    {
        #region [Variables/Fields]
        private const int DemoCount = 100000;
        private GlobalVisualizerView _demo;
        private ShowcaseRangeBar _bar;
        private Text _stats;
        private List<ShowcaseItem> _items;
        #endregion

        #region [Properties]
        public override string Title => "Welcome";
        public GlobalVisualizerView Demo => _demo;
        #endregion

        #region [Unity Methods]
        private void OnDestroy()
        {
            if (_demo != null) _demo.OnVisibleRangeChanged -= OnRangeChanged;
        }
        #endregion

        #region [Public Methods]
        public override void OnShown()
        {
            if (_items != null) return;
            _items = ShowcaseData.Create(DemoCount);
            _demo.SetItems(_items);
            OnRangeChanged(_demo.VisibleRange);
        }
        #endregion

        #region [Private Methods]
        protected override void Build(RectTransform root)
        {
            RectTransform page = Factory.Row(root, "Welcome", 16f, 20);
            ShowcaseFactory.Stretch(page);
            page.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperLeft;
            page.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            RectTransform left = Factory.ScrollColumn(page, "Intro", 10f, 6);
            Factory.Label(left, ProductTitle(), 30, FontStyle.Bold);
            Factory.Label(left, "Virtualized lists, grids and tag flows for Unity uGUI.  Version " + ShowcaseApp.Version + "  ·  by " + ShowcaseApp.Author, 14, FontStyle.Normal, ShowcaseSkin.MutedColor);
            Factory.Label(left,
                "A normal ScrollRect creates one GameObject for every item and lets a LayoutGroup place them all. " +
                "That is fine for 50 items and painful for 5,000: long load times, a big memory bill and slow scrolling.\n\n" +
                "Global Visualizer only creates the items that fit in the viewport (plus a small buffer), positions them with math " +
                "instead of LayoutGroups, and reuses the same GameObjects while you scroll. Your data can be any list and your item can be any prefab.", 15);

            Factory.Heading(left, "Explore", 18);
            Card(left, "Learn", "Nine short interactive lessons. Each one explains a feature, shows the code and gives you something to try.", () => App.ShowPage<ShowcaseTutorialPage>());
            Card(left, "Playground", "Every setting on one screen: layouts, size modes, selection, snapping, data operations and live stats.", () => App.ShowPage<ShowcasePlaygroundPage>());
            Card(left, "Benchmark", "Naive ScrollRect + LayoutGroup against Global Visualizer with the same data. Build time, GameObjects, memory, frame time and the cost of a change.", () => App.ShowPage<ShowcaseBenchmarkPage>());
            Card(left, "Self Test", "Runs the package's guarantees in this build (virtualization, reuse, zero garbage while scrolling, layouts, navigation, events) and shows pass or fail.", () => App.ShowPage<ShowcaseSelfTestPage>());

            Factory.Heading(left, "Quick start", 18);
            Factory.Code(left,
                "// 1. Hierarchy: right-click > Global Visualizer > Vertical List\n" +
                "// 2. Give it data from any script:\n" +
                "view.SetItems(myItems, (go, item, index) =>\n" +
                "    go.GetComponentInChildren<Text>().text = item.Name);\n" +
                "// 3. After you change the list:\n" +
                "view.NotifyInserted(index, 1);   // or NotifyRemoved / NotifyChanged / NotifyReset");

            RectTransform right = Factory.Column(page, "How It Works", 8f, 12, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(right.gameObject, minWidth: 440f, preferredWidth: 440f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Heading(right, "How it works", 18);
            Factory.Label(right, "This list holds " + DemoCount.ToString("N0") + " items. Scroll it fast: the counter below stays tiny because only the rows you can see exist.", 13, FontStyle.Normal, ShowcaseSkin.MutedColor);
            _demo = Factory.Visualizer(right, "Welcome Demo", VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f));
            _demo.ItemPrefab = Templates.Row;
            _demo.SelectionMode = VisualSelectionMode.Single;
            _demo.OnVisibleRangeChanged += OnRangeChanged;
            _bar = ShowcaseRangeBar.Create(Factory, right);
            _stats = Factory.Label(right, "", 13, FontStyle.Bold);
        }

        private void Card(RectTransform parent, string title, string text, UnityEngine.Events.UnityAction open)
        {
            RectTransform row = Factory.Row(parent, title + " Card", 12f, 10, ShowcaseSkin.PanelColor);
            Factory.Button(row, "Open " + title, open, 150f);
            Text label = Factory.Label(row, text, 13);
            ShowcaseFactory.Layout(label.gameObject, flexibleWidth: 1f);
        }

        private void OnRangeChanged(VisualRange range)
        {
            if (_demo == null) return;
            _bar.Show(range, _demo.Count);
            _stats.text = $"{_demo.Count:N0} items  ·  {_demo.ActiveViewCount} GameObjects alive  ·  {_demo.CreatedViewCount} ever created";
        }

        private static string ProductTitle()
        {
            return ShowcaseApp.ProductName;
        }
        #endregion
    }
}
