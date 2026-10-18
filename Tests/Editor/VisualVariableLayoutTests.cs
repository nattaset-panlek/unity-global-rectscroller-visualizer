using NUnit.Framework;
using UnityEngine;

namespace GlobalVisualizer.Tests
{
    public sealed class VisualVariableLayoutTests
    {
        #region [Public Methods]
        [Test]
        public void Vertical_VariableHeightsUseOffsets()
        {
            VisualSizeTable sizes = Table(new Vector2(0f, 40f), new Vector2(0f, 80f), new Vector2(0f, 40f), new Vector2(0f, 80f));
            var layout = VisualLayout.Create(VisualLayoutKind.Vertical);

            layout.Rebuild(VisualLayoutSettings.Vertical(40f, 10f), new Vector2(300f, 200f), Vector2.zero, 4, sizes);

            Assert.AreEqual(270f, layout.ContentSize.y);
            Assert.AreEqual(new Rect(0f, 50f, 300f, 80f), layout.GetItemRect(1));
            Assert.AreEqual(new Rect(0f, 190f, 300f, 80f), layout.GetItemRect(3));
            Assert.AreEqual(new VisualRange(1, 1), layout.GetRange(new Rect(0f, 60f, 300f, 40f)));
            Assert.IsTrue(layout.GetRange(new Rect(0f, 135f, 300f, 3f)).IsEmpty);
            Assert.AreEqual(new VisualRange(1, 3), layout.GetRange(new Rect(0f, 100f, 300f, 100f)));
        }

        [Test]
        public void Vertical_UnknownSizesUseTheEstimate()
        {
            var sizes = new VisualSizeTable { Estimate = new Vector2(0f, 50f) };
            sizes.Resize(10);
            sizes.Set(0, new Vector2(0f, 100f));
            var layout = VisualLayout.Create(VisualLayoutKind.Vertical);

            layout.Rebuild(VisualLayoutSettings.Vertical(50f, 0f), new Vector2(300f, 200f), Vector2.zero, 10, sizes);

            Assert.AreEqual(100f + 9f * 50f, layout.ContentSize.y);
            Assert.AreEqual(150f, layout.GetItemRect(2).y);
        }

        [Test]
        public void Horizontal_VariableWidths()
        {
            VisualSizeTable sizes = Table(new Vector2(100f, 0f), new Vector2(50f, 0f), new Vector2(200f, 0f));
            var layout = VisualLayout.Create(VisualLayoutKind.Horizontal);

            layout.Rebuild(VisualLayoutSettings.Horizontal(80f, 0f), new Vector2(120f, 100f), Vector2.zero, 3, sizes);

            Assert.AreEqual(350f, layout.ContentSize.x);
            Assert.AreEqual(new Rect(150f, 0f, 200f, 100f), layout.GetItemRect(2));
            Assert.AreEqual(new VisualRange(1, 2), layout.GetRange(new Rect(110f, 0f, 120f, 100f)));
        }

        [Test]
        public void Flow_WrapsRowsAndFindsRanges()
        {
            FlowVisualLayout layout = BuildFlow();

            Assert.AreEqual(3, layout.RowCount);
            Assert.AreEqual(new Rect(110f, 0f, 150f, 40f), layout.GetItemRect(1));
            Assert.AreEqual(new Rect(0f, 50f, 80f, 40f), layout.GetItemRect(2));
            Assert.AreEqual(new Rect(90f, 50f, 120f, 40f), layout.GetItemRect(3));
            Assert.AreEqual(new Rect(0f, 100f, 200f, 40f), layout.GetItemRect(4));
            Assert.AreEqual(140f, layout.ContentSize.y);
            Assert.AreEqual(new VisualRange(2, 3), layout.GetRange(new Rect(0f, 45f, 300f, 50f)));
        }

        [Test]
        public void Flow_ClampsItemsWiderThanTheViewport()
        {
            VisualSizeTable sizes = Table(new Vector2(500f, 40f), new Vector2(50f, 40f));
            var layout = new FlowVisualLayout();

            layout.Rebuild(VisualLayoutSettings.Flow(10f), new Vector2(300f, 200f), Vector2.zero, 2, sizes);

            Assert.AreEqual(300f, layout.GetItemRect(0).width);
            Assert.AreEqual(new Rect(0f, 50f, 50f, 40f), layout.GetItemRect(1));
        }

        [Test]
        public void Flow_NeighborsFollowRowsAndCenters()
        {
            FlowVisualLayout layout = BuildFlow();

            Assert.AreEqual(1, layout.GetNeighbor(3, VisualDirection.Up));
            Assert.AreEqual(3, layout.GetNeighbor(1, VisualDirection.Down));
            Assert.AreEqual(4, layout.GetNeighbor(3, VisualDirection.Down));
            Assert.AreEqual(1, layout.GetNeighbor(2, VisualDirection.Left));
            Assert.AreEqual(-1, layout.GetNeighbor(4, VisualDirection.Right));
            Assert.AreEqual(-1, layout.GetNeighbor(0, VisualDirection.Up));
        }

        [Test]
        public void Grid_NeighborsStopAtEdges()
        {
            var layout = VisualLayout.Create(VisualLayoutKind.Grid);
            layout.Rebuild(VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f), new Vector2(330f, 400f), Vector2.zero, 10);

            Assert.AreEqual(1, layout.GetNeighbor(4, VisualDirection.Up));
            Assert.AreEqual(3, layout.GetNeighbor(4, VisualDirection.Left));
            Assert.AreEqual(7, layout.GetNeighbor(4, VisualDirection.Down));
            Assert.AreEqual(-1, layout.GetNeighbor(2, VisualDirection.Right));
            Assert.AreEqual(-1, layout.GetNeighbor(8, VisualDirection.Down));
        }

        [Test]
        public void Vertical_NeighborsAreLinear()
        {
            var layout = VisualLayout.Create(VisualLayoutKind.Vertical);
            layout.Rebuild(VisualLayoutSettings.Vertical(40f, 0f), new Vector2(300f, 400f), Vector2.zero, 5);

            Assert.AreEqual(3, layout.GetNeighbor(2, VisualDirection.Down));
            Assert.AreEqual(1, layout.GetNeighbor(2, VisualDirection.Up));
            Assert.AreEqual(-1, layout.GetNeighbor(2, VisualDirection.Left));
            Assert.AreEqual(-1, layout.GetNeighbor(4, VisualDirection.Down));
        }

        [Test]
        public void CrossAlignment_CentersNarrowItems()
        {
            var vertical = VisualLayoutSettings.Vertical(40f, 0f);
            vertical.ItemSize = new Vector2(100f, 40f);
            vertical.CrossAlignment = VisualAlignment.Center;
            var layout = VisualLayout.Create(VisualLayoutKind.Vertical);
            layout.Rebuild(vertical, new Vector2(300f, 200f), Vector2.zero, 3);
            Assert.AreEqual(new Rect(100f, 40f, 100f, 40f), layout.GetItemRect(1));
            Assert.AreEqual(300f, layout.ContentSize.x);

            var horizontal = VisualLayoutSettings.Horizontal(50f, 0f);
            horizontal.ItemSize = new Vector2(50f, 60f);
            horizontal.CrossAlignment = VisualAlignment.End;
            layout = VisualLayout.Create(VisualLayoutKind.Horizontal);
            layout.Rebuild(horizontal, new Vector2(300f, 100f), Vector2.zero, 3);
            Assert.AreEqual(new Rect(50f, 40f, 50f, 60f), layout.GetItemRect(1));
        }

        [Test]
        public void SizeTable_InsertAndRemoveKeepKnownSizes()
        {
            VisualSizeTable sizes = Table(new Vector2(0f, 10f), new Vector2(0f, 20f), new Vector2(0f, 30f));

            sizes.Insert(1, 2);
            Assert.AreEqual(5, sizes.Count);
            Assert.IsFalse(sizes.IsKnown(1));
            Assert.AreEqual(20f, sizes.Get(3).y);

            sizes.Remove(0, 2);
            Assert.AreEqual(3, sizes.Count);
            Assert.AreEqual(2, sizes.KnownCount);
            Assert.AreEqual(25f, sizes.KnownAverage.y);

            sizes.Invalidate(1);
            Assert.AreEqual(1, sizes.KnownCount);
            Assert.AreEqual(30f, sizes.KnownAverage.y);
        }

        [Test]
        public void Selection_RangeAdditiveAndShifts()
        {
            var selection = new VisualSelection { Mode = VisualSelectionMode.Multi };
            selection.Click(2, false, false);
            selection.Click(5, false, true);
            Assert.AreEqual(4, selection.Count);

            selection.Click(3, true, false);
            Assert.IsFalse(selection.Contains(3));
            Assert.AreEqual(3, selection.Count);

            selection.OnInserted(0, 10);
            Assert.IsTrue(selection.Contains(12));
            Assert.IsTrue(selection.Contains(14));
            Assert.IsTrue(selection.Contains(15));

            selection.OnRemoved(12, 3);
            Assert.AreEqual(1, selection.Count);
            Assert.IsTrue(selection.Contains(12));

            selection.Mode = VisualSelectionMode.Single;
            selection.Click(7, true, true);
            Assert.AreEqual(1, selection.Count);
            Assert.AreEqual(7, selection.First);
        }
        #endregion

        #region [Private Methods]
        private static FlowVisualLayout BuildFlow()
        {
            VisualSizeTable sizes = Table(new Vector2(100f, 40f), new Vector2(150f, 40f), new Vector2(80f, 40f), new Vector2(120f, 40f), new Vector2(200f, 40f));
            var layout = new FlowVisualLayout();
            layout.Rebuild(VisualLayoutSettings.Flow(10f), new Vector2(300f, 200f), Vector2.zero, 5, sizes);
            return layout;
        }

        private static VisualSizeTable Table(params Vector2[] values)
        {
            var table = new VisualSizeTable();
            table.Resize(values.Length);
            for (int i = 0; i < values.Length; i++) table.Set(i, values[i]);
            return table;
        }
        #endregion
    }
}
