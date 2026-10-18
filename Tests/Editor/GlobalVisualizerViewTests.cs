using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GlobalVisualizer.Tests
{
    public sealed class GlobalVisualizerViewTests
    {
        #region [Variables/Fields]
        private readonly List<Object> _cleanup = new List<Object>();
        #endregion

        #region [Public Methods]
        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null) Object.DestroyImmediate(_cleanup[i]);
            }
            _cleanup.Clear();
        }

        [Test]
        public void SetCount_CreatesOnlyVisibleViews()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));

            view.SetCount(10000);

            Assert.AreEqual(10000, view.Count);
            Assert.AreEqual(14, view.ActiveViewCount);
            Assert.AreEqual(14, view.CreatedViewCount);
            Assert.AreEqual(new Vector2(300f, 400000f), view.Content.sizeDelta);
        }

        [Test]
        public void Scrolling_BindsTheVisibleIndices()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(10000);

            view.ScrollPosition = new Vector2(0f, 4000f);

            Assert.AreEqual(new VisualRange(97, 113), view.VisibleRange);
            RectTransform item = view.GetView(100);
            Assert.IsNotNull(item);
            Assert.AreEqual(100, item.GetComponent<VisualTestItem>().BoundValue);
            Assert.AreEqual(new Vector2(0f, -4000f), item.anchoredPosition);
            Assert.IsNull(view.GetView(0));
        }

        [Test]
        public void ScrollingTheWholeList_ReusesViews()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(10000);

            for (float y = 0f; y <= 399600f; y += 37f) view.ScrollPosition = new Vector2(0f, y);

            Assert.LessOrEqual(view.CreatedViewCount, 17);
            Assert.AreEqual(9999, view.VisibleRange.Last);
        }

        [Test]
        public void Scrolling_DoesNotAllocate()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(10000);
            for (int i = 0; i < 200; i++) view.ScrollPosition = new Vector2(0f, 100000f + i * 13f);

            const int steps = 20000;
            long allocated = long.MaxValue;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                int collections = GC.CollectionCount(0);
                long before = GC.GetTotalMemory(false);
                for (int i = 0; i < steps; i++) view.ScrollPosition = new Vector2(0f, 100000f + (i % 5000) * 13f);
                long growth = GC.GetTotalMemory(false) - before;
                if (GC.CollectionCount(0) == collections) allocated = Math.Min(allocated, Math.Max(0L, growth));
            }

            Assert.AreNotEqual(long.MaxValue, allocated, "a garbage collection ran during every attempt");
            Assert.Less(allocated, (long)steps, "the managed heap grew by at least 1 byte per scroll step in every attempt");
        }

        [Test]
        public void SetItems_BindsInterfaceAndCallback()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            var received = new List<int>();

            view.SetItems(new List<int> { 5, 6, 7 }, (go, value, index) => received.Add(value));

            Assert.AreEqual(6, view.GetView(1).GetComponent<VisualTestItem>().BoundValue);
            CollectionAssert.AreEqual(new[] { 5, 6, 7 }, received);
        }

        [Test]
        public void NotifyRemoved_UpdatesCountContentAndBindings()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            var data = new List<int>();
            for (int i = 0; i < 100; i++) data.Add(i);
            view.SetItems(data);

            data.RemoveRange(0, 10);
            view.NotifyRemoved(0, 10);

            Assert.AreEqual(90, view.Count);
            Assert.AreEqual(3600f, view.Content.sizeDelta.y);
            Assert.AreEqual(10, view.GetView(0).GetComponent<VisualTestItem>().BoundValue);
        }

        [Test]
        public void NotifyChanged_RebindsOnlyAVisibleView()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            var data = new List<int>();
            for (int i = 0; i < 1000; i++) data.Add(i);
            int binds = 0;
            view.SetItems(data, (go, value, index) => binds++);
            Assert.AreEqual(14, binds);

            view.NotifyChanged(3);
            view.NotifyChanged(500);

            Assert.AreEqual(15, binds);
        }

        [Test]
        public void ScrollTo_ShowsTheIndex()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(1000);

            view.ScrollTo(500);

            Assert.AreEqual(20000f, view.ScrollPosition.y);
            Assert.IsTrue(view.VisibleRange.Contains(500));
            Assert.IsNotNull(view.GetView(500));
        }

        [Test]
        public void Horizontal_StretchesHeightAndScrollsHorizontally()
        {
            GlobalVisualizerView view = CreateView(new Vector2(400f, 100f), VisualLayoutSettings.Horizontal(50f, 10f));

            view.SetCount(100);

            Assert.IsTrue(view.ScrollRect.horizontal);
            Assert.IsFalse(view.ScrollRect.vertical);
            Assert.AreEqual(9, view.ActiveViewCount);
            Assert.AreEqual(new Vector2(50f, 100f), view.GetView(1).sizeDelta);
            Assert.AreEqual(new Vector2(60f, 0f), view.GetView(1).anchoredPosition);
        }

        [Test]
        public void Grid_PlacesCellsByRowAndColumn()
        {
            GlobalVisualizerView view = CreateView(new Vector2(330f, 400f), VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f));

            view.SetCount(100);

            Assert.AreEqual(15, view.ActiveViewCount);
            Assert.AreEqual(new Vector2(110f, -110f), view.GetView(4).anchoredPosition);
        }

        [Test]
        public void ChangingThePrefab_RebuildsViewsFromTheNewPrefab()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(100);
            RectTransform other = CreateTemplate("OtherItem", new Vector2(300f, 80f));

            view.ItemPrefab = other;

            Assert.AreEqual("OtherItem", view.GetView(0).name);
            Assert.AreEqual(14, view.CreatedViewCount);
        }

        [Test]
        public void EmptyAndClear_LeaveNoActiveViews()
        {
            GlobalVisualizerView view = CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(0);
            Assert.AreEqual(0, view.ActiveViewCount);
            Assert.IsTrue(view.VisibleRange.IsEmpty);

            view.SetCount(50);
            view.Clear();

            Assert.AreEqual(0, view.ActiveViewCount);
            Assert.AreEqual(0, view.Count);
        }
        #endregion

        #region [Private Methods]
        private GlobalVisualizerView CreateView(Vector2 viewportSize, VisualLayoutSettings settings)
        {
            var root = new GameObject("Visualizer", typeof(RectTransform));
            _cleanup.Add(root);
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = viewportSize;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(rootRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(viewportRect, false);

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;

            var view = root.AddComponent<GlobalVisualizerView>();
            view.SetLayout(settings);
            view.ItemPrefab = CreateTemplate("Item", new Vector2(300f, 40f));
            return view;
        }

        private RectTransform CreateTemplate(string name, Vector2 size)
        {
            var template = new GameObject(name, typeof(RectTransform), typeof(VisualTestItem));
            _cleanup.Add(template);
            var rect = (RectTransform)template.transform;
            rect.sizeDelta = size;
            template.SetActive(false);
            return rect;
        }
        #endregion
    }
}
