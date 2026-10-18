using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseTutorialPage : ShowcasePage
    {
        #region [Variables/Fields]
        private readonly List<ShowcaseLesson> _lessons = new List<ShowcaseLesson>();
        private readonly List<Button> _lessonButtons = new List<Button>();
        private List<ShowcaseItem> _items = new List<ShowcaseItem>();
        private GlobalVisualizerView _demo;
        private ShowcaseRangeBar _bar;
        private ShowcaseEventLog _log;
        private Text _progress;
        private Text _title;
        private Text _body;
        private Text _goals;
        private Text _code;
        private Text _stats;
        private RectTransform _actions;
        private Button _previous;
        private Button _next;
        private int _current = -1;
        private int _moves;
        private int _scrollIndex = 5000;
        private float _alignment = 0.5f;
        private bool _snapArmed;
        #endregion

        #region [Properties]
        public override string Title => "Learn";
        public int LessonCount => _lessons.Count;
        public int CurrentLesson => _current;
        public GlobalVisualizerView Demo => _demo;
        public ShowcaseLesson Lesson => _current >= 0 ? _lessons[_current] : null;
        #endregion

        #region [Public Methods]
        public override void OnShown()
        {
            if (_current < 0) ShowLesson(0);
        }

        public void ShowLesson(int index)
        {
            if (index < 0 || index >= _lessons.Count) return;
            _current = index;
            ShowcaseLesson lesson = _lessons[index];
            _title.text = lesson.Title;
            _body.text = lesson.Body;
            _code.text = lesson.Code;
            for (int i = _actions.childCount - 1; i >= 0; i--)
            {
                GameObject child = _actions.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            lesson.BuildActions?.Invoke(_actions);
            _moves = 0;
            _snapArmed = false;
            lesson.Enter?.Invoke();
            _previous.interactable = index > 0;
            _next.interactable = index < _lessons.Count - 1;
            RefreshGoals();
            _log.Add("Lesson " + (index + 1) + ": " + lesson.Title);
        }

        public void Achieve(string id)
        {
            ShowcaseLesson lesson = Lesson;
            if (lesson == null || !lesson.Achieve(id)) return;
            RefreshGoals();
            if (lesson.IsComplete) _log.Add("Lesson complete. Press Next.");
        }
        #endregion

        #region [Private Methods]
        protected override void Build(RectTransform root)
        {
            RectTransform page = Factory.Row(root, "Learn", 12f, 12);
            ShowcaseFactory.Stretch(page);
            HorizontalLayoutGroup pageGroup = page.GetComponent<HorizontalLayoutGroup>();
            pageGroup.childAlignment = TextAnchor.UpperLeft;
            pageGroup.childForceExpandHeight = true;

            RectTransform list = Factory.Column(page, "Lessons", 4f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(list.gameObject, minWidth: 220f, preferredWidth: 220f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Heading(list, "Lessons", 18);
            _progress = Factory.Label(list, "", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);

            RectTransform lessonArea = Factory.ScrollColumn(page, "Lesson", 8f, 8);
            _title = Factory.Heading(lessonArea, "", 22);
            _body = Factory.Label(lessonArea, "", 14);
            Factory.Label(lessonArea, "Try it", 15, FontStyle.Bold, ShowcaseSkin.AccentColor);
            _goals = Factory.Label(lessonArea, "", 14);
            _actions = Factory.Column(lessonArea, "Actions", 6f);
            Factory.Label(lessonArea, "Code", 15, FontStyle.Bold, ShowcaseSkin.AccentColor);
            _code = Factory.Code(lessonArea, "");
            RectTransform nav = Factory.Row(lessonArea, "Navigation", 8f);
            _previous = Factory.Button(nav, "< Previous", () => ShowLesson(_current - 1), 120f);
            Factory.Spacer(nav);
            _next = Factory.Button(nav, "Next >", () => ShowLesson(_current + 1), 120f);

            RectTransform demoArea = Factory.Column(page, "Live Demo", 6f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(demoArea.gameObject, minWidth: 420f, preferredWidth: 420f, flexibleWidth: 0f, flexibleHeight: 1f);
            Factory.Heading(demoArea, "Live demo", 18);
            _demo = Factory.Visualizer(demoArea, "Tutorial Demo", VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f));
            _demo.ItemPrefab = Templates.Row;
            _demo.EmptyPrefab = Templates.Empty;
            _stats = Factory.Label(demoArea, "", 12, FontStyle.Bold);
            _bar = ShowcaseRangeBar.Create(Factory, demoArea);
            Factory.Label(demoArea, "Events", 13, FontStyle.Bold);
            _log = ShowcaseEventLog.Create(Factory, Templates, demoArea, 130f);
            HookDemo();

            CreateLessons();
            for (int i = 0; i < _lessons.Count; i++)
            {
                int index = i;
                Button button = Factory.Button(list, "", () => ShowLesson(index));
                ShowcaseFactory.Layout(button.gameObject, minWidth: 0f, preferredWidth: 200f);
                Text label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(10f, label.rectTransform.offsetMin.y);
                _lessonButtons.Add(button);
            }
        }

        private void HookDemo()
        {
            _demo.OnVisibleRangeChanged += range =>
            {
                UpdateStats();
                if (_demo.Count >= 100000 && range.Last >= _demo.Count - 1) Achieve("reach-end");
                if (_snapArmed) Achieve("snap");
            };
            _demo.OnItemClicked += (index, data) => _log.Add($"OnItemClicked({index})");
            _demo.OnItemDoubleClicked += (index, data) =>
            {
                _log.Add($"OnItemDoubleClicked({index})");
                Achieve("double");
            };
            _demo.OnItemRightClicked += (index, data) =>
            {
                _log.Add($"OnItemRightClicked({index}) at {data.position}");
                Achieve("right");
            };
            _demo.OnItemSubmitted += index =>
            {
                _log.Add($"OnItemSubmitted({index})");
                Achieve("submit");
            };
            _demo.OnSelectionChanged += () =>
            {
                _log.Add($"OnSelectionChanged: {_demo.SelectedCount} selected");
                UpdateStats();
                if (_demo.SelectedCount >= 1) Achieve("select");
                if (_demo.SelectionMode == VisualSelectionMode.Multi && _demo.SelectedCount >= 3) Achieve("range");
            };
            _demo.OnFocusChanged += index =>
            {
                _log.Add($"OnFocusChanged({index})");
                _moves++;
                if (_moves >= 6) Achieve("move");
            };
            _demo.OnNavigateOut += direction => _log.Add($"OnNavigateOut({direction})");
        }

        private void CreateLessons()
        {
            _lessons.Add(new ShowcaseLesson("1. Bind your data",
                "A <b>GlobalVisualizerView</b> sits on a normal ScrollRect. Give it an item prefab and a list. It creates only the items that fit in the viewport (plus a buffer) and reuses them while you scroll, so 100 rows and 100,000 rows cost the same.\n\nWatch <b>alive</b> and <b>created</b> under the demo while you bind more items and scroll.",
                "view.ItemPrefab = rowPrefab;        // any prefab\nview.SetItems(items);               // any IReadOnlyList<T>\n\npublic class RowView : MonoBehaviour, IVisualItem<ShowcaseItem>\n{\n    public Text title;\n    public void Bind(ShowcaseItem item, VisualItemInfo info) => title.text = item.Title;\n    public void Unbind() { }\n}")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.None, 100),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Bind", 6f);
                    Factory.Button(row, "Bind 100", () => SetData(100));
                    Factory.Button(row, "Bind 10,000", () => SetData(10000));
                    Factory.Button(row, "Bind 100,000", () =>
                    {
                        SetData(100000);
                        Achieve("bind-100k");
                    });
                    Factory.Button(row, "Jump to the end", () => _demo.ScrollTo(_demo.Count - 1, 1f));
                }
            }.Goal("bind-100k", "Bind 100,000 items.").Goal("reach-end", "Reach the last item (drag, mouse wheel or Jump to the end). 'created' stays small."));

            _lessons.Add(new ShowcaseLesson("2. Change the data",
                "The view never polls your list. After you change it, tell the view what changed with one <b>Notify</b> call. Only visible items are rebound and nothing is recreated. Selection and focus move with inserted or removed items.",
                "items.Insert(0, item);   view.NotifyInserted(0, 1);\nitems.RemoveAt(0);       view.NotifyRemoved(0, 1);\nitems[3].Title = \"New\";  view.NotifyChanged(3);\nitems.Reverse();         view.NotifyReset();")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.Single, 1000),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Change", 6f);
                    Factory.Button(row, "Insert at top", () =>
                    {
                        _items.Insert(0, ShowcaseData.CreateNext());
                        _demo.NotifyInserted(0, 1);
                        _log.Add("NotifyInserted(0, 1)");
                        Achieve("insert");
                        UpdateStats();
                    });
                    Factory.Button(row, "Remove first", () =>
                    {
                        if (_items.Count == 0) return;
                        _items.RemoveAt(0);
                        _demo.NotifyRemoved(0, 1);
                        _log.Add("NotifyRemoved(0, 1)");
                        Achieve("remove");
                        UpdateStats();
                    });
                    Factory.Button(row, "Rename first visible", () =>
                    {
                        int index = _demo.VisibleRange.IsEmpty ? 0 : _demo.VisibleRange.First;
                        if (index >= _items.Count) return;
                        _items[index].Title = "Renamed " + DateTime.Now.ToString("HH:mm:ss");
                        _demo.NotifyChanged(index);
                        _log.Add($"NotifyChanged({index})");
                        Achieve("change");
                    });
                    Factory.Button(row, "Reverse (reset)", () =>
                    {
                        _items.Reverse();
                        _demo.NotifyReset();
                        _log.Add("NotifyReset()");
                        Achieve("reset");
                    });
                }
            }.Goal("insert", "Insert an item (select a row first and watch the selection move down).").Goal("remove", "Remove an item.").Goal("change", "Change one item.").Goal("reset", "Reset the whole list."));

            _lessons.Add(new ShowcaseLesson("3. Pick a layout",
                "The same data and the same pool can be shown as a vertical list, a horizontal strip, a grid that fits as many columns as the width allows, or a flow of tags that wrap onto new rows. Switch while the demo holds 2,000 items.",
                "view.SetLayout(VisualLayoutSettings.Vertical(44f, 4f));\nview.SetLayout(VisualLayoutSettings.Horizontal(96f, 6f));\nview.SetLayout(VisualLayoutSettings.Grid(new Vector2(96f, 96f), 6f));\nview.SizeMode = VisualSizeMode.Measure;     // tags are as wide as their text\nview.SetLayout(VisualLayoutSettings.Flow(6f));")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.Single, 2000),
                BuildActions = area => Factory.Segment(area, new[] { "Vertical", "Horizontal", "Grid", "Flow" }, 0, ApplyLayout)
            }.Goal("vertical", "Vertical").Goal("horizontal", "Horizontal").Goal("grid", "Grid").Goal("flow", "Flow"));

            _lessons.Add(new ShowcaseLesson("4. Items of different sizes",
                "<b>Fixed</b>: every item is Item Size (fastest).\n<b>Provider</b>: your code returns a size per index; here every 7th row is taller.\n<b>Measure</b>: the view binds the item, asks its Layout Group for the preferred size and caches it. Here each note is as tall as its text. Unmeasured items use the average so far, and the first visible item stays still while sizes are corrected.",
                "view.SizeMode = VisualSizeMode.Provider;\nview.SizeProvider = i => new Vector2(0f, items[i].Height);\n\nview.SizeMode = VisualSizeMode.Measure;   // item root needs a Layout Group\nview.InvalidateSizes();                   // after something changes every size")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.None, 5000),
                BuildActions = area => Factory.Segment(area, new[] { "Fixed", "Provider", "Measure" }, 0, ApplySizeMode)
            }.Goal("provider", "Try Provider and find a tall row.").Goal("measure", "Try Measure and scroll through notes of different lengths."));

            _lessons.Add(new ShowcaseLesson("5. Selection and clicks",
                "Set a Selection Mode and the view tracks selection by index, even for items that have no GameObject right now. Items read IsSelected and IsFocused in Bind or in IVisualItemState (this demo tints selected rows).\n\nMulti: Shift-click selects a range, Ctrl-click adds or removes one item.",
                "view.SelectionMode = VisualSelectionMode.Multi;\nview.OnItemClicked       += (i, e) => Debug.Log(items[i].Title);\nview.OnItemDoubleClicked += (i, e) => Open(items[i]);\nview.OnItemRightClicked  += (i, e) => ShowContextMenu(e.position);\nview.OnSelectionChanged  += () => label.text = view.SelectedCount + \" selected\";")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.Single, 500),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Selection", 6f);
                    Factory.Segment(row, new[] { "None", "Single", "Multi" }, 1, index =>
                    {
                        _demo.SelectionMode = (VisualSelectionMode)index;
                        _log.Add("SelectionMode = " + _demo.SelectionMode);
                    });
                    Factory.Button(row, "Clear selection", () => _demo.ClearSelection());
                }
            }.Goal("select", "Click a row to select it.").Goal("range", "Switch to Multi and Shift-click to select 3 or more rows.").Goal("double", "Double-click a row.").Goal("right", "Right-click a row."));

            _lessons.Add(new ShowcaseLesson("6. Keyboard and gamepad",
                "Press <b>Focus the grid</b>, then use the arrow keys (or a d-pad / stick). Focus follows the layout: rows and columns in a grid. It can move to items that do not exist yet; the view scrolls and creates them. Enter (or the gamepad South button) submits. At an edge, OnNavigateOut tells you so you can move focus elsewhere.",
                "view.Focus(0);                          // selects it in the EventSystem\nview.MoveFocus(VisualDirection.Down);   // same as pressing Down\nview.OnItemSubmitted  += i => Use(items[i]);\nview.OnNavigateOut    += dir => backButton.Select();\nview.FocusOnClick = false;              // when WASD also moves your player")
            {
                Enter = () => Configure(VisualLayoutSettings.Grid(new Vector2(ShowcaseTemplates.CellSize, ShowcaseTemplates.CellSize), 6f), Templates.Cell, VisualSizeMode.Fixed, VisualSelectionMode.Single, 300),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Focus", 6f);
                    Factory.Button(row, "Focus the grid", () =>
                    {
                        _moves = 0;
                        _demo.Focus(_demo.VisibleRange.IsEmpty ? 0 : _demo.VisibleRange.First);
                        Achieve("focus");
                    });
                    Factory.Button(row, "Move right (code)", () => _demo.MoveFocus(VisualDirection.Right));
                    Factory.Button(row, "Move down (code)", () => _demo.MoveFocus(VisualDirection.Down));
                }
            }.Goal("focus", "Press Focus the grid.").Goal("move", "Move the focus 5 times with the arrow keys.").Goal("submit", "Press Enter to submit the focused item."));

            _lessons.Add(new ShowcaseLesson("7. Scrolling helpers",
                "<b>ScrollTo(index, alignment)</b> jumps to any item: 0 = start, 0.5 = centre, 1 = end. <b>EnsureVisible</b> scrolls only as far as needed. <b>Snap To Items</b> glides to the nearest item edge after a drag or a wheel scroll. OnVisibleRangeChanged reports what is on screen.",
                "view.ScrollTo(5000, 0.5f);\nview.EnsureVisible(42);\nview.SnapToItems = true;\nview.OnVisibleRangeChanged += r => label.text = r.ToString();")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.Single, 10000),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Scroll To", 6f);
                    Factory.RowLabel(row, "Index", 14, FontStyle.Normal);
                    Factory.Input(row, "index", _scrollIndex.ToString(), InputField.ContentType.IntegerNumber, value =>
                    {
                        if (int.TryParse(value, out int parsed)) _scrollIndex = parsed;
                    }, 90f);
                    Factory.Segment(row, new[] { "Start", "Centre", "End" }, 1, index => _alignment = index * 0.5f);
                    RectTransform buttons = Factory.Row(area, "Scroll Buttons", 6f);
                    Factory.Button(buttons, "ScrollTo", () =>
                    {
                        _demo.ScrollTo(_scrollIndex, _alignment);
                        _demo.Select(Mathf.Clamp(_scrollIndex, 0, _demo.Count - 1));
                        _log.Add($"ScrollTo({_scrollIndex}, {_alignment:0.0})");
                        if (Mathf.Approximately(_alignment, 0.5f)) Achieve("scrollto");
                    });
                    Factory.Button(buttons, "EnsureVisible", () =>
                    {
                        _demo.EnsureVisible(_scrollIndex);
                        _log.Add($"EnsureVisible({_scrollIndex})");
                    });
                    Factory.Toggle(buttons, "Snap to items", false, on =>
                    {
                        _demo.SnapToItems = on;
                        _snapArmed = on;
                        _log.Add("SnapToItems = " + on);
                    });
                }
            }.Goal("scrollto", "Jump to item 5000 with the Centre alignment.").Goal("snap", "Turn on Snap to items, then scroll with the wheel or a drag and let go."));

            _lessons.Add(new ShowcaseLesson("8. Empty state",
                "Assign an <b>Empty Prefab</b> and it appears inside the viewport when the list is set but has no items. It hides by itself as soon as an item arrives. No extra code in your UI.",
                "view.EmptyPrefab = emptyPrefab;\nitems.Clear();\nview.NotifyReset();      // placeholder appears\nview.IsShowingEmpty      // true")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.None, 20),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Empty", 6f);
                    Factory.Button(row, "Clear all items", () =>
                    {
                        _items.Clear();
                        _demo.NotifyReset();
                        _log.Add("IsShowingEmpty = " + _demo.IsShowingEmpty);
                        if (_demo.IsShowingEmpty) Achieve("empty");
                        UpdateStats();
                    });
                    Factory.Button(row, "Add 20 items", () =>
                    {
                        int start = _items.Count;
                        for (int i = 0; i < 20; i++) _items.Add(ShowcaseData.CreateNext());
                        _demo.NotifyInserted(start, 20);
                        _log.Add("IsShowingEmpty = " + _demo.IsShowingEmpty);
                        if (!_demo.IsShowingEmpty && Lesson != null && Lesson.Goals[0].Done) Achieve("refill");
                        UpdateStats();
                    });
                }
            }.Goal("empty", "Clear the list and see the placeholder.").Goal("refill", "Add items again: the placeholder hides."));

            _lessons.Add(new ShowcaseLesson("9. Use it in your project",
                "1. In the Hierarchy, right-click > <b>Global Visualizer</b> > Vertical List, Horizontal List, Grid or Flow (Tags). You get a ScrollRect, the view and an Item Template.\n2. Right-click it again > <b>Create Item Template...</b> to generate a prefab and an IVisualItem<T> script for your data type.\n3. Call <b>SetItems</b> from your code, and a <b>Notify</b> call after each change.\n4. Do not put a LayoutGroup or ContentSizeFitter on the Content: the view places items itself.\n\nThen open the Benchmark to see why this matters, and the Self Test to check the guarantees in your build.",
                "public class Inventory : MonoBehaviour\n{\n    [SerializeField] GlobalVisualizerView view;\n    readonly List<Item> items = new List<Item>();\n\n    void Start() => view.SetItems(items);\n\n    public void Add(Item item)\n    {\n        items.Add(item);\n        view.NotifyInserted(items.Count - 1, 1);\n    }\n}")
            {
                Enter = () => Configure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed, VisualSelectionMode.Single, 1000),
                BuildActions = area =>
                {
                    RectTransform row = Factory.Row(area, "Next Steps", 6f);
                    Factory.Button(row, "Open the Benchmark", () =>
                    {
                        Achieve("benchmark");
                        App.ShowPage<ShowcaseBenchmarkPage>();
                    });
                    Factory.Button(row, "Open the Self Test", () =>
                    {
                        Achieve("selftest");
                        App.ShowPage<ShowcaseSelfTestPage>();
                    });
                    Factory.Button(row, "Open the Playground", () => App.ShowPage<ShowcasePlaygroundPage>());
                }
            }.Goal("benchmark", "Open the Benchmark.").Goal("selftest", "Open the Self Test."));
        }

        private void Configure(VisualLayoutSettings layout, RectTransform template, VisualSizeMode sizeMode, VisualSelectionMode selection, int count)
        {
            _demo.ClearSelection();
            _demo.SelectionMode = selection;
            _demo.SnapToItems = false;
            _demo.SizeProvider = sizeMode == VisualSizeMode.Provider ? (Func<int, Vector2>)ProvideSize : null;
            _demo.SizeMode = sizeMode;
            _demo.SetLayout(layout);
            _demo.ItemPrefab = template;
            SetData(count);
            _demo.ScrollPosition = Vector2.zero;
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(_demo.transform))
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void SetData(int count)
        {
            var watch = Stopwatch.StartNew();
            _items = ShowcaseData.Create(count);
            long generated = watch.ElapsedMilliseconds;
            watch.Restart();
            _demo.SetItems(_items);
            _log.Add($"SetItems({count:N0}): data {generated} ms, bind {watch.ElapsedMilliseconds} ms, {_demo.CreatedViewCount} views");
            UpdateStats();
        }

        private Vector2 ProvideSize(int index)
        {
            return new Vector2(0f, index < _items.Count ? _items[index].Height : ShowcaseTemplates.RowHeight);
        }

        private void ApplyLayout(int index)
        {
            float cell = ShowcaseTemplates.CellSize;
            switch (index)
            {
                case 1:
                    Reconfigure(VisualLayoutSettings.Horizontal(cell, 6f), Templates.Cell, VisualSizeMode.Fixed);
                    Achieve("horizontal");
                    break;
                case 2:
                    Reconfigure(VisualLayoutSettings.Grid(new Vector2(cell, cell), 6f), Templates.Cell, VisualSizeMode.Fixed);
                    Achieve("grid");
                    break;
                case 3:
                    Reconfigure(VisualLayoutSettings.Flow(6f, new Vector2(100f, 32f)), Templates.Tag, VisualSizeMode.Measure);
                    Achieve("flow");
                    break;
                default:
                    Reconfigure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed);
                    Achieve("vertical");
                    break;
            }
            _log.Add("SetLayout(" + _demo.LayoutSettings.Kind + ")");
        }

        private void ApplySizeMode(int index)
        {
            switch (index)
            {
                case 1:
                    Reconfigure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Provider);
                    Achieve("provider");
                    break;
                case 2:
                    Reconfigure(VisualLayoutSettings.Vertical(60f, 4f), Templates.Note, VisualSizeMode.Measure);
                    Achieve("measure");
                    break;
                default:
                    Reconfigure(VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f), Templates.Row, VisualSizeMode.Fixed);
                    break;
            }
            _log.Add("SizeMode = " + _demo.SizeMode);
        }

        private void Reconfigure(VisualLayoutSettings layout, RectTransform template, VisualSizeMode sizeMode)
        {
            _demo.SizeProvider = sizeMode == VisualSizeMode.Provider ? (Func<int, Vector2>)ProvideSize : null;
            _demo.SizeMode = sizeMode;
            _demo.SetLayout(layout);
            _demo.ItemPrefab = template;
            _demo.ScrollPosition = Vector2.zero;
            UpdateStats();
        }

        private void RefreshGoals()
        {
            ShowcaseLesson lesson = Lesson;
            if (lesson != null)
            {
                var text = new System.Text.StringBuilder();
                for (int i = 0; i < lesson.Goals.Count; i++)
                {
                    ShowcaseGoal goal = lesson.Goals[i];
                    text.Append(goal.Done ? "<color=#2a9445>[x]</color>  " : "[  ]  ").Append(goal.Text);
                    if (i < lesson.Goals.Count - 1) text.Append('\n');
                }
                _goals.text = text.ToString();
            }

            int done = 0;
            for (int i = 0; i < _lessons.Count; i++)
            {
                if (_lessons[i].IsComplete) done++;
                if (i >= _lessonButtons.Count) continue;
                Text label = _lessonButtons[i].GetComponentInChildren<Text>();
                label.text = (_lessons[i].IsComplete ? "[x] " : "[  ] ") + _lessons[i].Title;
                label.fontStyle = i == _current ? FontStyle.Bold : FontStyle.Normal;
                _lessonButtons[i].GetComponent<Image>().color = i == _current ? ShowcaseSkin.SelectedColor : Color.white;
            }
            _progress.text = $"{done} of {_lessons.Count} lessons complete";
        }

        private void UpdateStats()
        {
            if (_demo == null) return;
            _stats.text = $"{_demo.Count:N0} items  ·  alive {_demo.ActiveViewCount}  ·  created {_demo.CreatedViewCount}  ·  visible {_demo.VisibleRange}  ·  selected {_demo.SelectedCount}";
            _bar.Show(_demo.VisibleRange, _demo.Count);
        }
        #endregion
    }
}
