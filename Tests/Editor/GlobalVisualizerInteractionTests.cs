using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GlobalVisualizer.Tests
{
    public sealed class GlobalVisualizerInteractionTests
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
        public void Click_SelectsSingleItemAndRaisesEvents()
        {
            GlobalVisualizerView view = CreateList(100);
            view.SelectionMode = VisualSelectionMode.Single;
            int clicked = -1;
            int changes = 0;
            view.OnItemClicked += (index, data) => clicked = index;
            view.OnSelectionChanged += () => changes++;

            view.HandleClick(3, Pointer(PointerEventData.InputButton.Left, 1), false, false);

            Assert.AreEqual(3, clicked);
            Assert.AreEqual(1, changes);
            Assert.AreEqual(3, view.SelectedIndex);
            Assert.AreEqual(3, view.FocusedIndex);
            Assert.IsTrue(Item(view, 3).LastInfo.IsSelected);
            Assert.IsTrue(Item(view, 3).LastInfo.IsFocused);

            view.HandleClick(5, Pointer(PointerEventData.InputButton.Left, 1), false, false);

            Assert.AreEqual(2, changes);
            Assert.IsFalse(view.IsSelected(3));
            Assert.IsTrue(view.IsSelected(5));
            Assert.IsFalse(Item(view, 3).LastInfo.IsSelected);
        }

        [Test]
        public void Click_MultiSelectionUsesRangeAndAdditive()
        {
            GlobalVisualizerView view = CreateList(100);
            view.SelectionMode = VisualSelectionMode.Multi;

            view.HandleClick(2, Pointer(PointerEventData.InputButton.Left, 1), false, false);
            view.HandleClick(5, Pointer(PointerEventData.InputButton.Left, 1), false, true);
            Assert.AreEqual(4, view.SelectedCount);

            view.HandleClick(3, Pointer(PointerEventData.InputButton.Left, 1), true, false);
            var selected = new List<int>();
            view.GetSelected(selected);

            CollectionAssert.AreEqual(new[] { 2, 4, 5 }, selected);
        }

        [Test]
        public void Click_WithoutSelectionModeOnlyRaisesEvents()
        {
            GlobalVisualizerView view = CreateList(100);
            int clicked = -1;
            view.OnItemClicked += (index, data) => clicked = index;

            view.HandleClick(4, Pointer(PointerEventData.InputButton.Left, 1), false, false);

            Assert.AreEqual(4, clicked);
            Assert.AreEqual(0, view.SelectedCount);
        }

        [Test]
        public void DoubleAndRightClick_RaiseTheirEvents()
        {
            GlobalVisualizerView view = CreateList(100);
            view.SelectionMode = VisualSelectionMode.Single;
            int doubled = -1;
            int right = -1;
            view.OnItemDoubleClicked += (index, data) => doubled = index;
            view.OnItemRightClicked += (index, data) => right = index;

            view.HandleClick(4, Pointer(PointerEventData.InputButton.Left, 2), false, false);
            view.HandleClick(6, Pointer(PointerEventData.InputButton.Right, 1), false, false);

            Assert.AreEqual(4, doubled);
            Assert.AreEqual(6, right);
            Assert.IsFalse(view.IsSelected(6));
        }

        [Test]
        public void Views_GetAnInputRelayUnlessDisabled()
        {
            GlobalVisualizerView view = CreateList(100);
            VisualItemInput input = view.GetView(2).GetComponent<VisualItemInput>();
            Assert.IsNotNull(input);
            Assert.AreEqual(2, input.Index);
            Assert.AreSame(view, input.Owner);

            GlobalVisualizerView plain = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            plain.HandleInput = false;
            plain.SetCount(10);

            Assert.IsNull(plain.GetView(0).GetComponent<VisualItemInput>());
        }

        [Test]
        public void Navigation_FollowsTheGridAndScrollsIntoView()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(330f, 400f), VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f));
            view.SetCount(100);
            int focusEvents = 0;
            view.OnFocusChanged += index => focusEvents++;

            view.Focus(0);
            Assert.IsTrue(view.MoveFocus(VisualDirection.Down));
            Assert.AreEqual(3, view.FocusedIndex);
            Assert.IsTrue(view.MoveFocus(VisualDirection.Right));
            Assert.AreEqual(4, view.FocusedIndex);
            Assert.AreEqual(3, focusEvents);

            var move = new AxisEventData(null) { moveDir = MoveDirection.Down };
            view.HandleMove(4, move);
            Assert.AreEqual(7, view.FocusedIndex);
            Assert.IsTrue(move.used);

            view.Focus(90);
            Assert.AreEqual(3000f, view.ScrollPosition.y);
            Assert.IsNotNull(view.GetView(90));
            Assert.IsTrue(Item(view, 90).LastInfo.IsFocused);
        }

        [Test]
        public void Navigation_ReportsLeavingTheEdge()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(330f, 400f), VisualLayoutSettings.Grid(new Vector2(100f, 100f), 10f));
            view.SetCount(10);
            VisualDirection? left = null;
            view.OnNavigateOut += direction => left = direction;

            view.Focus(2);

            Assert.IsFalse(view.MoveFocus(VisualDirection.Right));
            Assert.AreEqual(VisualDirection.Right, left);
            Assert.AreEqual(2, view.FocusedIndex);
        }

        [Test]
        public void Submit_SelectsAndRaisesTheEvent()
        {
            GlobalVisualizerView view = CreateList(100);
            view.SelectionMode = VisualSelectionMode.Single;
            int submitted = -1;
            view.OnItemSubmitted += index => submitted = index;

            view.Focus(4);
            view.OnSubmit(new BaseEventData(null));

            Assert.AreEqual(4, submitted);
            Assert.IsTrue(view.IsSelected(4));
        }

        [Test]
        public void InsertAndRemove_ShiftSelectionAndFocus()
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            List<int> data = VisualTestScene.Range(50);
            view.SetItems(data);
            view.SelectionMode = VisualSelectionMode.Single;
            view.Select(5);
            view.Focus(5);

            data.InsertRange(0, new[] { -1, -2 });
            view.NotifyInserted(0, 2);
            Assert.IsTrue(view.IsSelected(7));
            Assert.AreEqual(7, view.FocusedIndex);

            data.RemoveRange(0, 4);
            view.NotifyRemoved(0, 4);
            Assert.IsTrue(view.IsSelected(3));
            Assert.AreEqual(3, view.FocusedIndex);
        }

        [Test]
        public void StateListeners_ReceiveSelectionAndFocus()
        {
            RectTransform template = _scene.CreateTemplate<VisualStateProbe>("Probe", new Vector2(300f, 40f));
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f), template);
            view.SelectionMode = VisualSelectionMode.Multi;
            view.SetCount(20);

            view.Select(2);
            view.Focus(3);

            VisualStateProbe two = view.GetView(2).GetComponent<VisualStateProbe>();
            VisualStateProbe three = view.GetView(3).GetComponent<VisualStateProbe>();
            Assert.IsTrue(two.Selected);
            Assert.IsFalse(two.Focused);
            Assert.IsTrue(three.Focused);
            Assert.AreEqual(3, three.Index);
        }

        [Test]
        public void Drag_StartsAcrossTheScrollAxisOnly()
        {
            GlobalVisualizerView view = CreateList(1000);
            var pointer = new PointerEventData(null) { pressPosition = Vector2.zero, position = new Vector2(30f, 5f) };
            Assert.IsFalse(view.ShouldStartItemDrag(0, pointer));

            var handler = new VisualTestDragHandler();
            view.DragHandler = handler;
            Assert.IsTrue(view.ShouldStartItemDrag(0, pointer));

            pointer.position = new Vector2(5f, 30f);
            Assert.IsFalse(view.ShouldStartItemDrag(0, pointer));

            view.ItemDragAngle = 0f;
            Assert.IsTrue(view.ShouldStartItemDrag(0, pointer));

            handler.Allow = false;
            Assert.IsFalse(view.ShouldStartItemDrag(0, pointer));
        }

        [Test]
        public void Drag_RelayCallsTheHandler()
        {
            GlobalVisualizerView view = CreateList(1000);
            var handler = new VisualTestDragHandler();
            view.DragHandler = handler;
            VisualItemDrag drag = view.GetView(1).GetComponent<VisualItemDrag>();
            Assert.IsNotNull(drag);
            var pointer = new PointerEventData(null) { pressPosition = Vector2.zero, position = new Vector2(40f, 0f) };

            drag.OnBeginDrag(pointer);
            drag.OnDrag(pointer);
            drag.OnEndDrag(pointer);

            Assert.AreEqual(1, handler.BeginIndex);
            Assert.AreEqual(1, handler.DragCount);
            Assert.AreEqual(1, handler.EndIndex);
        }

        [Test]
        public void Drag_SkipsItemsThatHandleDragThemselves()
        {
            RectTransform template = _scene.CreateTemplate<VisualForeignDragItem>("Foreign", new Vector2(300f, 40f));
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f), template);
            view.DragHandler = new VisualTestDragHandler();

            view.SetCount(10);

            Assert.IsNull(view.GetView(0).GetComponent<VisualItemDrag>());
        }

        [Test]
        public void Drag_ShortListsAlwaysDragItems()
        {
            GlobalVisualizerView view = CreateList(3);
            view.DragHandler = new VisualTestDragHandler();
            var pointer = new PointerEventData(null) { pressPosition = Vector2.zero, position = new Vector2(0f, 40f) };

            Assert.IsTrue(view.ShouldStartItemDrag(1, pointer));
        }

        [Test]
        public void Snap_PicksTheNearestItemEdge()
        {
            GlobalVisualizerView view = CreateList(1000);

            view.ScrollPosition = new Vector2(0f, 4015f);
            Assert.AreEqual(4000f, view.GetSnapPosition().y);

            view.ScrollPosition = new Vector2(0f, 4025f);
            Assert.AreEqual(4040f, view.GetSnapPosition().y);

            view.ScrollPosition = new Vector2(0f, 10f);
            Assert.AreEqual(0f, view.GetSnapPosition().y);
        }

        [Test]
        public void EnsureVisible_LeavesVisibleItemsAlone()
        {
            GlobalVisualizerView view = CreateList(1000);
            view.ScrollPosition = new Vector2(0f, 400f);

            view.EnsureVisible(12);
            Assert.AreEqual(400f, view.ScrollPosition.y);

            view.EnsureVisible(30);
            Assert.AreEqual(840f, view.ScrollPosition.y);

            view.EnsureVisible(2);
            Assert.AreEqual(80f, view.ScrollPosition.y);
        }
        #endregion

        #region [Private Methods]
        private GlobalVisualizerView CreateList(int count)
        {
            GlobalVisualizerView view = _scene.CreateView(new Vector2(300f, 400f), VisualLayoutSettings.Vertical(40f, 0f));
            view.SetCount(count);
            return view;
        }

        private static VisualTestItem Item(GlobalVisualizerView view, int index)
        {
            return view.GetView(index).GetComponent<VisualTestItem>();
        }

        private static PointerEventData Pointer(PointerEventData.InputButton button, int clickCount)
        {
            return new PointerEventData(null) { button = button, clickCount = clickCount };
        }
        #endregion
    }
}
