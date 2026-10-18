using NUnit.Framework;
using UnityEngine;

namespace GlobalVisualizer.Tests
{
    public sealed class VisualLayoutTests
    {
        #region [Public Methods]
        [Test]
        public void Vertical_ContentSizeAndItemRects()
        {
            var settings = new VisualLayoutSettings { Kind = VisualLayoutKind.Vertical, ItemSize = new Vector2(0f, 40f), Spacing = new Vector2(0f, 4f), Padding = new VisualPadding(5f, 5f, 10f, 10f) };
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(300f, 200f), Vector2.zero, 10);

            Assert.AreEqual(new Vector2(300f, 456f), layout.ContentSize);
            Assert.AreEqual(new Rect(5f, 142f, 290f, 40f), layout.GetItemRect(3));
            Assert.IsTrue(layout.ScrollsVertically);
        }

        [Test]
        public void Vertical_VisibleRange()
        {
            var settings = new VisualLayoutSettings { Kind = VisualLayoutKind.Vertical, ItemSize = new Vector2(0f, 40f), Spacing = new Vector2(0f, 4f), Padding = new VisualPadding(5f, 5f, 10f, 10f) };
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(300f, 200f), Vector2.zero, 10);

            VisualRange range = layout.GetRange(new Rect(0f, 100f, 300f, 200f));

            Assert.AreEqual(2, range.First);
            Assert.AreEqual(6, range.Last);
        }

        [Test]
        public void Vertical_UsesPrefabHeightWhenItemSizeIsZero()
        {
            var settings = new VisualLayoutSettings { Kind = VisualLayoutKind.Vertical, Spacing = Vector2.zero };
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(200f, 100f), new Vector2(50f, 30f), 3);

            Assert.AreEqual(new Rect(0f, 60f, 200f, 30f), layout.GetItemRect(2));
        }

        [Test]
        public void Horizontal_ContentSizeRectsAndRange()
        {
            var settings = new VisualLayoutSettings { Kind = VisualLayoutKind.Horizontal, ItemSize = new Vector2(50f, 0f), Spacing = new Vector2(10f, 0f), Padding = new VisualPadding(10f) };
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(400f, 100f), Vector2.zero, 20);

            Assert.IsFalse(layout.ScrollsVertically);
            Assert.AreEqual(new Vector2(20f + 20f * 50f + 19f * 10f, 100f), layout.ContentSize);
            Assert.AreEqual(new Rect(130f, 10f, 50f, 80f), layout.GetItemRect(2));

            VisualRange range = layout.GetRange(new Rect(200f, 0f, 400f, 100f));
            Assert.AreEqual(3, range.First);
            Assert.AreEqual(9, range.Last);
        }

        [Test]
        public void Grid_FitColumnsFromViewportWidth()
        {
            var settings = VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f);
            var layout = (GridVisualLayout)VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(330f, 400f), Vector2.zero, 10);

            Assert.AreEqual(3, layout.Columns);
            Assert.AreEqual(4, layout.Rows);
            Assert.AreEqual(new Vector2(320f, 430f), layout.ContentSize);
            Assert.AreEqual(new Rect(110f, 110f, 100f, 100f), layout.GetItemRect(4));
        }

        [Test]
        public void Grid_FixedColumnsVisibleRangeCoversWholeRows()
        {
            var settings = VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f, VisualGridConstraint.FixedColumns, 2);
            var layout = (GridVisualLayout)VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(1000f, 300f), Vector2.zero, 9);

            Assert.AreEqual(2, layout.Columns);
            Assert.AreEqual(5, layout.Rows);

            VisualRange range = layout.GetRange(new Rect(0f, 115f, 1000f, 200f));
            Assert.AreEqual(2, range.First);
            Assert.AreEqual(5, range.Last);
        }

        [Test]
        public void Grid_FixedRowsScrollsHorizontally()
        {
            var settings = VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f, VisualGridConstraint.FixedRows, 2);
            var layout = (GridVisualLayout)VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(300f, 300f), Vector2.zero, 7);

            Assert.IsFalse(layout.ScrollsVertically);
            Assert.AreEqual(4, layout.Columns);
            Assert.AreEqual(new Rect(220f, 110f, 100f, 100f), layout.GetItemRect(5));

            VisualRange range = layout.GetRange(new Rect(225f, 0f, 200f, 300f));
            Assert.AreEqual(4, range.First);
            Assert.AreEqual(6, range.Last);
        }

        [Test]
        public void EmptyCount_GivesEmptyRangeAndPaddingOnlyContent()
        {
            var settings = new VisualLayoutSettings { Kind = VisualLayoutKind.Vertical, ItemSize = new Vector2(0f, 40f), Padding = new VisualPadding(0f, 0f, 6f, 4f) };
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(100f, 100f), Vector2.zero, 0);

            Assert.IsTrue(layout.GetRange(new Rect(0f, 0f, 100f, 100f)).IsEmpty);
            Assert.AreEqual(10f, layout.ContentSize.y);
        }

        [Test]
        public void ScrollPosition_IsClampedToContent()
        {
            var settings = VisualLayoutSettings.Vertical(40f, 0f);
            var layout = VisualLayout.Create(settings.Kind);
            layout.Rebuild(settings, new Vector2(100f, 400f), Vector2.zero, 100);

            Assert.AreEqual(new Vector2(0f, 2000f), layout.GetScrollPosition(50, new Vector2(100f, 400f), 0f));
            Assert.AreEqual(new Vector2(0f, 3600f), layout.GetScrollPosition(99, new Vector2(100f, 400f), 0f));
            Assert.AreEqual(new Vector2(0f, 1820f), layout.GetScrollPosition(50, new Vector2(100f, 400f), 0.5f));
        }
        #endregion
    }
}
