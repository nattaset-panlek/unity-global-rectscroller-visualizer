using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GlobalVisualizer.Tests
{
    public sealed class GlobalVisualizerSizeTests
    {
        #region [Variables/Fields]
        private readonly VisualTestScene _scene = new VisualTestScene();
        #endregion

        #region [Public Methods]
        [TearDown]
        public void TearDown()
        {
            _scene.Dispose();
        }

        [Test]
        public void Provider_UsesSizesFromCode()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SizeMode = VisualSizeMode.Provider;
            view.SizeProvider = index => new Vector2(0f, index % 2 == 0 ? 40f : 80f);

            view.SetCount(100);

            Assert.AreEqual(6000f, view.Content.sizeDelta.y);
            Assert.AreEqual(80f, view.GetView(1).sizeDelta.y);
            Assert.AreEqual(new Vector2(0f, -120f), view.GetView(2).anchoredPosition);
        }

        [Test]
        public void Measure_ReadsPreferredSizesOfBoundViews()
        {
            GlobalVisualizerView view = CreateMeasuredView();

            view.SetCount(300);

            Assert.AreEqual(30f, view.GetView(0).sizeDelta.y);
            Assert.AreEqual(60f, view.GetView(1).sizeDelta.y);
            Assert.AreEqual(90f, view.GetView(2).sizeDelta.y);
            Assert.AreEqual(new Vector2(0f, -30f), view.GetView(1).anchoredPosition);
            Assert.AreEqual(new Vector2(0f, -90f), view.GetView(2).anchoredPosition);
            Assert.Less(view.ActiveViewCount, 18);
        }

        [Test]
        public void Measure_ScrollToKeepsTheTargetAtTheTop()
        {
            GlobalVisualizerView view = CreateMeasuredView();
            view.SetCount(1000);

            view.ScrollTo(500);

            Rect rect = view.Layout.GetItemRect(500);
            Assert.AreEqual(rect.y, view.ScrollPosition.y, 0.5f);
            Assert.AreEqual(VisualMeasuredItem.HeightFor(500), view.GetView(500).sizeDelta.y);
            Assert.AreEqual(-rect.y, view.GetView(500).anchoredPosition.y, 0.01f);
        }

        [Test]
        public void Measure_NotifyChangedMeasuresAgain()
        {
            GlobalVisualizerView view = CreateMeasuredView();
            var data = new List<int> { 0, 1, 2, 0, 1, 2 };
            view.SetItems(data);
            Assert.AreEqual(60f, view.GetView(1).sizeDelta.y);

            data[1] = 2;
            view.NotifyChanged(1);

            Assert.AreEqual(90f, view.GetView(1).sizeDelta.y);
            Assert.AreEqual(new Vector2(0f, -120f), view.GetView(2).anchoredPosition);
        }

        [Test]
        public void Flow_PlacesViewsInRows()
        {
            var widths = new[] { 100f, 150f, 80f, 120f, 200f };
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Flow(10f, new Vector2(100f, 40f)));
            view.SizeMode = VisualSizeMode.Provider;
            view.SizeProvider = index => new Vector2(widths[index], 40f);

            view.SetCount(5);

            Assert.AreEqual(new Vector2(110f, 0f), view.GetView(1).anchoredPosition);
            Assert.AreEqual(new Vector2(0f, -50f), view.GetView(2).anchoredPosition);
            Assert.AreEqual(new Vector2(200f, 40f), view.GetView(4).sizeDelta);
            Assert.AreEqual(140f, view.Content.sizeDelta.y);
            Assert.IsTrue(view.ScrollRect.vertical);
        }

        [Test]
        public void Grid_IgnoresSizeModes()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(330f, 400f), VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f));
            view.SizeMode = VisualSizeMode.Provider;
            view.SizeProvider = index => new Vector2(500f, 500f);

            view.SetCount(9);

            Assert.AreEqual(new Vector2(100f, 100f), view.GetView(4).sizeDelta);
            Assert.AreEqual(320f, view.Content.sizeDelta.y);
        }

        [Test]
        public void EmptyPlaceholder_ShowsOnlyForAnEmptySource()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.EmptyPrefab = _scene.CreateEmptyTemplate();
            Assert.IsFalse(view.IsShowingEmpty);

            view.SetCount(0);
            Assert.IsTrue(view.IsShowingEmpty);
            Transform placeholder = view.Viewport.Find("Empty");
            Assert.IsNotNull(placeholder);
            Assert.IsTrue(placeholder.gameObject.activeSelf);

            view.SetCount(5);
            Assert.IsFalse(view.IsShowingEmpty);
            Assert.AreEqual(5, view.ActiveViewCount);

            view.SetItems(new List<int>());
            Assert.IsTrue(view.IsShowingEmpty);

            view.Clear();
            Assert.IsFalse(view.IsShowingEmpty);
        }
        #endregion

        #region [Private Methods]
        private GlobalVisualizerView CreateMeasuredView()
        {
            RectTransform template = _scene.CreateTemplate<VisualMeasuredItem>("Measured", new Vector2(300f, VisualMeasuredItem.Unit));
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(0f, 0f), template);
            view.SizeMode = VisualSizeMode.Measure;
            return view;
        }
        #endregion
    }
}
