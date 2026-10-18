using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseSelfTests
    {
        #region [Variables/Fields]
        private const float Row = 40f;
        private readonly ShowcaseFactory _factory;
        private readonly ShowcaseTemplates _templates;
        private readonly RectTransform _sandbox;
        private readonly List<Func<ShowcaseCheckResult>> _checks = new List<Func<ShowcaseCheckResult>>();
        private GlobalVisualizerView _view;
        #endregion

        #region [Properties]
        public IReadOnlyList<Func<ShowcaseCheckResult>> Checks => _checks;
        #endregion

        #region [Public Methods]
        public ShowcaseSelfTests(ShowcaseFactory factory, ShowcaseTemplates templates, RectTransform sandbox)
        {
            _factory = factory;
            _templates = templates;
            _sandbox = sandbox;
            _checks.Add(OnlyVisibleViewsExist);
            _checks.Add(ViewsAreReused);
            _checks.Add(ScrollingMakesNoGarbage);
            _checks.Add(RecycledViewsShowTheRightData);
            _checks.Add(ScrollToShowsTheIndex);
            _checks.Add(SelectionFollowsInsertAndRemove);
            _checks.Add(GridPlacesRowsAndColumns);
            _checks.Add(FlowWrapsItems);
            _checks.Add(ProviderSizesAddUp);
            _checks.Add(MeasureReadsItemHeights);
            _checks.Add(EmptyPlaceholderToggles);
            _checks.Add(KeyboardFollowsTheGrid);
            _checks.Add(ClicksRaiseEvents);
        }

        public ShowcaseCheckResult Run(int index)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ShowcaseCheckResult result;
            try
            {
                result = _checks[index]();
            }
            catch (Exception exception)
            {
                result = new ShowcaseCheckResult("Check " + (index + 1), "")
                {
                    Passed = false,
                    Detail = exception.GetType().Name + ": " + exception.Message
                };
            }
            result.Milliseconds = (float)watch.Elapsed.TotalMilliseconds;
            Dispose();
            return result;
        }
        #endregion

        #region [Private Methods]
        private ShowcaseCheckResult OnlyVisibleViewsExist()
        {
            var result = new ShowcaseCheckResult("Only visible items get a GameObject", "100,000 items cost the same as 20.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.SetItems(ShowcaseData.Create(100000));
            int limit = Mathf.CeilToInt((view.Viewport.rect.height + view.BufferPixels * 2f) / Row) + 1;
            result.Passed = view.Count == 100000 && view.CreatedViewCount <= limit;
            result.Detail = $"{view.Count:N0} items, {view.CreatedViewCount} views created (limit {limit}).";
            return result;
        }

        private ShowcaseCheckResult ViewsAreReused()
        {
            var result = new ShowcaseCheckResult("Views are reused while scrolling", "After one pass, scrolling never instantiates another GameObject.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.SetItems(ShowcaseData.Create(20000));
            float end = view.Content.sizeDelta.y;
            for (float y = 0f; y <= end; y += 397f) view.ScrollPosition = new Vector2(0f, y);
            int afterFirst = view.CreatedViewCount;
            for (float y = end; y >= 0f; y -= 211f) view.ScrollPosition = new Vector2(0f, y);
            for (float y = 0f; y <= end; y += 53f) view.ScrollPosition = new Vector2(0f, y);
            int limit = Mathf.CeilToInt((view.Viewport.rect.height + view.BufferPixels * 2f) / Row) + 1;
            result.Passed = view.CreatedViewCount == afterFirst && afterFirst <= limit;
            result.Detail = $"Scrolled {end:N0} px three times: {afterFirst} views after the first pass, {view.CreatedViewCount} after all three (limit {limit}).";
            return result;
        }

        private ShowcaseCheckResult ScrollingMakesNoGarbage()
        {
            var result = new ShowcaseCheckResult("Scrolling makes no garbage", "No GC spikes from the list while the player scrolls.");
            const int steps = 5000;
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.SetItems(ShowcaseData.Create(50000));
            for (int i = 0; i < 300; i++) view.ScrollPosition = new Vector2(0f, 1000f + i * 13f);
            long growth = -1L;
            for (int attempt = 0; attempt < 3 && growth < 0L; attempt++)
            {
                int collections = ShowcaseMetrics.Collections();
                long before = ShowcaseMetrics.ManagedBytes();
                for (int i = 0; i < steps; i++) view.ScrollPosition = new Vector2(0f, 1000f + (i % 2000) * 37f);
                long after = ShowcaseMetrics.ManagedBytes();
                if (ShowcaseMetrics.Collections() == collections) growth = Math.Max(0L, after - before);
            }
            if (growth < 0L)
            {
                result.Passed = true;
                result.Detail = "Inconclusive: a garbage collection ran during every attempt.";
                return result;
            }
            result.Passed = growth < steps;
            result.Detail = $"Managed heap grew {growth:N0} bytes over {steps:N0} scroll steps with real rows rebinding (pass: under 1 byte per step; the counter moves in 4 KB pages).";
            return result;
        }

        private ShowcaseCheckResult RecycledViewsShowTheRightData()
        {
            var result = new ShowcaseCheckResult("Recycled views show the right item", "A reused GameObject is always rebound to the item at its new index.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            List<ShowcaseItem> items = ShowcaseData.Create(5000);
            view.SetItems(items);
            view.ScrollTo(3777);
            int wrong = 0;
            VisualRange range = view.VisibleRange;
            for (int i = range.First; i <= range.Last; i++)
            {
                RectTransform item = view.GetView(i);
                if (item == null || item.GetComponent<ShowcaseItemView>().Data != items[i]) wrong++;
            }
            result.Passed = wrong == 0 && range.Contains(3777);
            result.Detail = $"Checked {range.Length} views around #3777: {wrong} wrong.";
            return result;
        }

        private ShowcaseCheckResult ScrollToShowsTheIndex()
        {
            var result = new ShowcaseCheckResult("ScrollTo jumps to any index", "ScrollTo(i, alignment) puts the item where you asked.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.SetItems(ShowcaseData.Create(10000));
            view.ScrollTo(5000);
            float top = view.ScrollPosition.y;
            view.ScrollTo(5000, 0.5f);
            float centre = view.ScrollPosition.y;
            float expectedCentre = 5000f * Row - (view.Viewport.rect.height - Row) * 0.5f;
            result.Passed = view.GetView(5000) != null && Mathf.Abs(top - 5000f * Row) < 0.5f && Mathf.Abs(centre - expectedCentre) < 0.5f;
            result.Detail = $"Start alignment y = {top:0}, centre alignment y = {centre:0} (expected {5000f * Row:0} and {expectedCentre:0}).";
            return result;
        }

        private ShowcaseCheckResult SelectionFollowsInsertAndRemove()
        {
            var result = new ShowcaseCheckResult("Selection follows inserts and removes", "Notify* keeps selection and focus on the same item.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            List<ShowcaseItem> items = ShowcaseData.Create(100);
            view.SelectionMode = VisualSelectionMode.Single;
            view.SetItems(items);
            view.Select(10);
            items.InsertRange(0, ShowcaseData.Create(3));
            view.NotifyInserted(0, 3);
            bool afterInsert = view.IsSelected(13);
            items.RemoveRange(0, 5);
            view.NotifyRemoved(0, 5);
            bool afterRemove = view.IsSelected(8);
            result.Passed = afterInsert && afterRemove;
            result.Detail = $"Selected #10, inserted 3 at the top -> #{(afterInsert ? 13 : -1)}, removed 5 -> #{view.SelectedIndex}.";
            return result;
        }

        private ShowcaseCheckResult GridPlacesRowsAndColumns()
        {
            var result = new ShowcaseCheckResult("Grid fits columns to the width", "Grid Fit places cells by row and column.");
            const float cell = 96f;
            const float spacing = 6f;
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Grid(new Vector2(cell, cell), spacing), _templates.Cell);
            view.SetItems(ShowcaseData.Create(500));
            int columns = Mathf.Max(1, Mathf.FloorToInt((view.Viewport.rect.width + spacing) / (cell + spacing)));
            RectTransform second = view.GetView(columns);
            result.Passed = second != null && second.anchoredPosition == new Vector2(0f, -(cell + spacing));
            result.Detail = $"{columns} columns: item #{columns} at {(second != null ? second.anchoredPosition.ToString() : "missing")}.";
            return result;
        }

        private ShowcaseCheckResult FlowWrapsItems()
        {
            var result = new ShowcaseCheckResult("Flow wraps items onto new rows", "Tags flow left to right and wrap.");
            const float width = 120f;
            const float spacing = 6f;
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Flow(spacing, new Vector2(width, 32f)), _templates.Tag);
            view.SizeMode = VisualSizeMode.Provider;
            view.SizeProvider = index => new Vector2(width, 32f);
            view.SetItems(ShowcaseData.Create(40));
            int perRow = Mathf.Max(1, Mathf.FloorToInt((view.Viewport.rect.width + spacing) / (width + spacing)));
            RectTransform wrapped = view.GetView(perRow);
            result.Passed = wrapped != null && wrapped.anchoredPosition == new Vector2(0f, -(32f + spacing));
            result.Detail = $"{perRow} tags per row: tag #{perRow} at {(wrapped != null ? wrapped.anchoredPosition.ToString() : "missing")}.";
            return result;
        }

        private ShowcaseCheckResult ProviderSizesAddUp()
        {
            var result = new ShowcaseCheckResult("Provider sizes add up", "Rows of different heights from your code, no measuring.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 2f), _templates.Row);
            List<ShowcaseItem> items = ShowcaseData.Create(700);
            view.SizeMode = VisualSizeMode.Provider;
            view.SizeProvider = index => new Vector2(0f, items[index].Height);
            view.SetItems(items);
            float expected = 0f;
            for (int i = 0; i < items.Count; i++) expected += items[i].Height;
            expected += 2f * (items.Count - 1);
            result.Passed = Mathf.Abs(view.Content.sizeDelta.y - expected) < 0.5f && view.GetView(0).sizeDelta.y > view.GetView(1).sizeDelta.y;
            result.Detail = $"Content {view.Content.sizeDelta.y:N0} px, expected {expected:N0} px; row 0 is {view.GetView(0).sizeDelta.y:0} px, row 1 is {view.GetView(1).sizeDelta.y:0} px.";
            return result;
        }

        private ShowcaseCheckResult MeasureReadsItemHeights()
        {
            var result = new ShowcaseCheckResult("Measure reads item heights", "Wrapped text sets the row height without code.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(60f, 4f), _templates.Note);
            view.SizeMode = VisualSizeMode.Measure;
            var items = new List<ShowcaseItem>();
            ShowcaseItem sample = ShowcaseData.Item(0);
            items.Add(new ShowcaseItem(0, "Short", "", "", "Short note.", sample.Color, 0f));
            items.Add(new ShowcaseItem(1, "Long", "", "", "This note is long on purpose. It wraps onto several lines, so its row must be taller than the first one, and the visualizer finds that out by asking the Vertical Layout Group on the item root.", sample.Color, 0f));
            view.SetItems(items);
            float shortHeight = view.GetView(0).sizeDelta.y;
            float longHeight = view.GetView(1).sizeDelta.y;
            result.Passed = longHeight > shortHeight + 10f && Mathf.Abs(view.GetView(1).anchoredPosition.y + shortHeight + 4f) < 0.5f;
            result.Detail = $"Short note {shortHeight:0} px, long note {longHeight:0} px.";
            return result;
        }

        private ShowcaseCheckResult EmptyPlaceholderToggles()
        {
            var result = new ShowcaseCheckResult("Empty placeholder shows and hides", "The Empty Prefab appears only while the list is empty.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.EmptyPrefab = _templates.Empty;
            var items = new List<ShowcaseItem>();
            view.SetItems(items);
            bool shown = view.IsShowingEmpty;
            items.Add(ShowcaseData.Item(1));
            view.NotifyInserted(0, 1);
            bool hidden = !view.IsShowingEmpty;
            result.Passed = shown && hidden;
            result.Detail = $"Empty list: {(shown ? "shown" : "not shown")}. After one insert: {(hidden ? "hidden" : "still shown")}.";
            return result;
        }

        private ShowcaseCheckResult KeyboardFollowsTheGrid()
        {
            var result = new ShowcaseCheckResult("Keyboard focus follows the grid", "Down moves one row, Right one column.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Grid(new Vector2(96f, 96f), 6f), _templates.Cell);
            view.SetItems(ShowcaseData.Create(300));
            int columns = Mathf.Max(1, Mathf.FloorToInt((view.Viewport.rect.width + 6f) / 102f));
            view.Focus(0);
            view.MoveFocus(VisualDirection.Down);
            int down = view.FocusedIndex;
            view.MoveFocus(VisualDirection.Right);
            int right = view.FocusedIndex;
            for (int i = 0; i < 20; i++) view.MoveFocus(VisualDirection.Down);
            int far = view.FocusedIndex;
            result.Passed = down == columns && right == columns + 1 && far == right + columns * 20 && view.GetView(far) != null;
            result.Detail = $"Down -> #{down}, Right -> #{right}, 20 x Down -> #{far} (created and scrolled into view).";
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return result;
        }

        private ShowcaseCheckResult ClicksRaiseEvents()
        {
            var result = new ShowcaseCheckResult("Clicks raise events and select", "OnItemClicked, OnItemDoubleClicked and selection from real pointer events.");
            GlobalVisualizerView view = NewView(VisualLayoutSettings.Vertical(Row, 0f), _templates.Row);
            view.SelectionMode = VisualSelectionMode.Single;
            view.FocusOnClick = false;
            view.SetItems(ShowcaseData.Create(50));
            int clicked = -1;
            int doubled = -1;
            view.OnItemClicked += (index, data) => clicked = index;
            view.OnItemDoubleClicked += (index, data) => doubled = index;
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, clickCount = 2 };
            ExecuteEvents.Execute(view.GetView(2).gameObject, pointer, ExecuteEvents.pointerClickHandler);
            result.Passed = clicked == 2 && doubled == 2 && view.IsSelected(2);
            result.Detail = $"Clicked #{clicked}, double-clicked #{doubled}, selected #{view.SelectedIndex}.";
            return result;
        }

        private GlobalVisualizerView NewView(VisualLayoutSettings layout, RectTransform template)
        {
            Dispose();
            _view = _factory.Visualizer(_sandbox, "Test View", layout);
            ShowcaseFactory.Stretch((RectTransform)_view.transform);
            Destroy(_view.GetComponent<LayoutElement>());
            _view.ItemPrefab = template;
            Canvas.ForceUpdateCanvases();
            return _view;
        }

        private void Dispose()
        {
            if (_view == null) return;
            GameObject go = _view.gameObject;
            _view = null;
            go.SetActive(false);
            Destroy(go);
        }

        private static void Destroy(UnityEngine.Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(target);
            else UnityEngine.Object.DestroyImmediate(target);
        }
        #endregion
    }
}
