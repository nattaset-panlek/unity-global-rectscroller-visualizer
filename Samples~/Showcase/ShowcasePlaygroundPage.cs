using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcasePlaygroundPage : ShowcasePage
    {
        #region [Variables/Fields]
        private static readonly int[] Counts = { 0, 10, 1000, 10000, 100000 };
        private List<ShowcaseItem> _items = new List<ShowcaseItem>();
        private GlobalVisualizerView _demo;
        private ShowcaseRangeBar _bar;
        private ShowcaseEventLog _log;
        private Text _stats;
        private Text _sizeNote;
        private Slider _itemSize;
        private Text _itemSizeLabel;
        private ShowcaseSegment _sizeSegment;
        private VisualLayoutKind _kind = VisualLayoutKind.Vertical;
        private VisualSizeMode _sizeMode = VisualSizeMode.Fixed;
        private VisualAlignment _alignment = VisualAlignment.Start;
        private VisualGridConstraint _constraint = VisualGridConstraint.Fit;
        private float _size = ShowcaseTemplates.RowHeight;
        private float _spacing = 4f;
        private float _padding = 4f;
        private float _crossSize;
        private int _constraintCount = 3;
        private int _scrollIndex = 500;
        private float _scrollAlignment;
        private bool _built;
        #endregion

        #region [Properties]
        public override string Title => "Playground";
        public GlobalVisualizerView Demo => _demo;
        #endregion

        #region [Public Methods]
        public override void OnShown()
        {
            if (_built) return;
            _built = true;
            ApplyLayout();
            SetData(10000);
        }
        #endregion

        #region [Private Methods]
        protected override void Build(RectTransform root)
        {
            RectTransform page = Factory.Row(root, "Playground", 12f, 12);
            ShowcaseFactory.Stretch(page);
            HorizontalLayoutGroup group = page.GetComponent<HorizontalLayoutGroup>();
            group.childAlignment = TextAnchor.UpperLeft;
            group.childForceExpandHeight = true;

            RectTransform controls = Factory.ScrollColumn(page, "Controls", 6f, 10);
            ShowcaseFactory.Layout(controls.parent.parent.gameObject, minWidth: 360f, preferredWidth: 360f, flexibleWidth: 0f);
            BuildData(controls);
            BuildLayout(controls);
            BuildSizes(controls);
            BuildInteraction(controls);
            BuildScrolling(controls);

            RectTransform demoArea = Factory.Column(page, "Live View", 6f, 10, ShowcaseSkin.PanelColor);
            ShowcaseFactory.Layout(demoArea.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);
            Factory.Heading(demoArea, "Live view", 18);
            _demo = Factory.Visualizer(demoArea, "Playground View", VisualLayoutSettings.Vertical(ShowcaseTemplates.RowHeight, 4f));
            _demo.ItemPrefab = Templates.Row;
            _demo.EmptyPrefab = Templates.Empty;
            _demo.SelectionMode = VisualSelectionMode.Single;
            _stats = Factory.Label(demoArea, "", 12, FontStyle.Bold);
            _bar = ShowcaseRangeBar.Create(Factory, demoArea);
            Factory.Label(demoArea, "Events", 13, FontStyle.Bold);
            _log = ShowcaseEventLog.Create(Factory, Templates, demoArea, 120f);

            _demo.OnVisibleRangeChanged += range => UpdateStats();
            _demo.OnSelectionChanged += () =>
            {
                _log.Add($"OnSelectionChanged: {_demo.SelectedCount} selected");
                UpdateStats();
            };
            _demo.OnFocusChanged += index =>
            {
                _log.Add($"OnFocusChanged({index})");
                UpdateStats();
            };
            _demo.OnItemClicked += (index, data) => _log.Add($"OnItemClicked({index})");
            _demo.OnItemDoubleClicked += (index, data) => _log.Add($"OnItemDoubleClicked({index})");
            _demo.OnItemRightClicked += (index, data) => _log.Add($"OnItemRightClicked({index})");
            _demo.OnItemSubmitted += index => _log.Add($"OnItemSubmitted({index})");
            _demo.OnNavigateOut += direction => _log.Add($"OnNavigateOut({direction})");
        }

        private void BuildData(RectTransform parent)
        {
            Section(parent, "Data");
            string[] labels = { "0", "10", "1k", "10k", "100k" };
            Factory.Segment(parent, labels, 3, index => SetData(Counts[index]));
            RectTransform row = Factory.Row(parent, "Data Ops", 4f);
            Factory.Button(row, "Insert", () =>
            {
                _items.Insert(0, ShowcaseData.CreateNext());
                _demo.NotifyInserted(0, 1);
                _log.Add("NotifyInserted(0, 1)");
                UpdateStats();
            });
            Factory.Button(row, "Remove", () =>
            {
                if (_items.Count == 0) return;
                _items.RemoveAt(0);
                _demo.NotifyRemoved(0, 1);
                _log.Add("NotifyRemoved(0, 1)");
                UpdateStats();
            });
            Factory.Button(row, "Change", () =>
            {
                int index = _demo.VisibleRange.IsEmpty ? 0 : _demo.VisibleRange.First;
                if (index >= _items.Count) return;
                _items[index].Title = "Changed " + DateTime.Now.ToString("HH:mm:ss");
                _demo.NotifyChanged(index);
                _log.Add($"NotifyChanged({index})");
            });
            Factory.Button(row, "Shuffle", () =>
            {
                var random = new System.Random();
                for (int i = _items.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    ShowcaseItem swap = _items[i];
                    _items[i] = _items[j];
                    _items[j] = swap;
                }
                _demo.NotifyReset();
                _log.Add("NotifyReset()");
            });
        }

        private void BuildLayout(RectTransform parent)
        {
            Section(parent, "Layout");
            Factory.Segment(parent, new[] { "Vertical", "Horizontal", "Grid", "Flow" }, 0, index =>
            {
                _kind = (VisualLayoutKind)index;
                _size = _kind == VisualLayoutKind.Vertical ? ShowcaseTemplates.RowHeight : _kind == VisualLayoutKind.Flow ? 32f : ShowcaseTemplates.CellSize;
                _itemSize.SetValueWithoutNotify(_size);
                if (_kind == VisualLayoutKind.Flow) SelectSizeMode(VisualSizeMode.Measure);
                else if (_sizeMode == VisualSizeMode.Measure && _kind != VisualLayoutKind.Vertical) SelectSizeMode(VisualSizeMode.Fixed);
                ApplyLayout();
            });
            _itemSizeLabel = SliderRow(parent, "Item size", 20f, 200f, _size, value =>
            {
                _size = value;
                ApplyLayout();
            }, out _itemSize);
            SliderRow(parent, "Spacing", 0f, 30f, _spacing, value =>
            {
                _spacing = value;
                ApplyLayout();
            }, out _);
            SliderRow(parent, "Padding", 0f, 40f, _padding, value =>
            {
                _padding = value;
                ApplyLayout();
            }, out _);
            SliderRow(parent, "Cross size (0 = stretch)", 0f, 400f, _crossSize, value =>
            {
                _crossSize = value;
                ApplyLayout();
            }, out _);
            Factory.Label(parent, "Cross alignment (when the cross size is set)", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);
            Factory.Segment(parent, new[] { "Start", "Center", "End" }, 0, index =>
            {
                _alignment = (VisualAlignment)index;
                ApplyLayout();
            });
            Factory.Label(parent, "Grid constraint", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);
            Factory.Segment(parent, new[] { "Fit", "Columns", "Rows" }, 0, index =>
            {
                _constraint = (VisualGridConstraint)index;
                ApplyLayout();
            });
            SliderRow(parent, "Columns / rows", 1f, 10f, _constraintCount, value =>
            {
                _constraintCount = Mathf.RoundToInt(value);
                ApplyLayout();
            }, out _, true);
        }

        private void BuildSizes(RectTransform parent)
        {
            Section(parent, "Size mode");
            _sizeSegment = Factory.Segment(parent, new[] { "Fixed", "Provider", "Measure" }, 0, index =>
            {
                _sizeMode = (VisualSizeMode)index;
                ApplyLayout();
            });
            _sizeNote = Factory.Label(parent, "", 12, FontStyle.Normal, ShowcaseSkin.MutedColor);
        }

        private void BuildInteraction(RectTransform parent)
        {
            Section(parent, "Interaction");
            Factory.Segment(parent, new[] { "No selection", "Single", "Multi" }, 1, index =>
            {
                _demo.SelectionMode = (VisualSelectionMode)index;
                _log.Add("SelectionMode = " + _demo.SelectionMode);
                UpdateStats();
            });
            Factory.Toggle(parent, "Handle input (clicks, keys)", true, on => _demo.HandleInput = on);
            Factory.Toggle(parent, "Focus on click", true, on => _demo.FocusOnClick = on);
            Factory.Toggle(parent, "Snap to items", false, on => _demo.SnapToItems = on);
            Factory.Toggle(parent, "Empty placeholder", true, on =>
            {
                _demo.EmptyPrefab = on ? Templates.Empty : null;
                UpdateStats();
            });
            SliderRow(parent, "Buffer pixels", 0f, 600f, 120f, value =>
            {
                _demo.BufferPixels = value;
                UpdateStats();
            }, out _, true);
        }

        private void BuildScrolling(RectTransform parent)
        {
            Section(parent, "Scrolling and focus");
            RectTransform row = Factory.Row(parent, "Scroll Index", 4f);
            Factory.RowLabel(row, "Index", 13, FontStyle.Normal);
            Factory.Input(row, "index", _scrollIndex.ToString(), InputField.ContentType.IntegerNumber, value =>
            {
                if (int.TryParse(value, out int parsed)) _scrollIndex = parsed;
            }, 80f);
            Factory.Segment(row, new[] { "0", "0.5", "1" }, 0, index => _scrollAlignment = index * 0.5f);
            RectTransform buttons = Factory.Row(parent, "Scroll Buttons", 4f);
            Factory.Button(buttons, "ScrollTo", () =>
            {
                _demo.ScrollTo(_scrollIndex, _scrollAlignment);
                _log.Add($"ScrollTo({_scrollIndex}, {_scrollAlignment:0.0})");
            });
            Factory.Button(buttons, "EnsureVisible", () =>
            {
                _demo.EnsureVisible(_scrollIndex);
                _log.Add($"EnsureVisible({_scrollIndex})");
            });
            Factory.Button(buttons, "Focus", () => _demo.Focus(_scrollIndex));
            Factory.Button(buttons, "Select", () => _demo.Select(_scrollIndex));
        }

        private void Section(RectTransform parent, string title)
        {
            Factory.Spacer(parent, 4f);
            Factory.Label(parent, title, 15, FontStyle.Bold, ShowcaseSkin.AccentColor);
        }

        private Text SliderRow(RectTransform parent, string label, float min, float max, float value, Action<float> onChanged, out Slider slider, bool whole = false)
        {
            RectTransform row = Factory.Row(parent, label, 6f);
            Text name = Factory.Label(row, label, 12);
            ShowcaseFactory.Layout(name.gameObject, minWidth: 150f, preferredWidth: 150f);
            Text number = null;
            slider = Factory.Slider(row, min, max, value, whole, v =>
            {
                if (number != null) number.text = v.ToString(whole ? "0" : "0.#");
                onChanged(v);
            });
            number = Factory.Label(row, value.ToString(whole ? "0" : "0.#"), 12, FontStyle.Normal, ShowcaseSkin.MutedColor, TextAnchor.MiddleRight);
            ShowcaseFactory.Layout(number.gameObject, minWidth: 36f, preferredWidth: 36f);
            return number;
        }

        private void SelectSizeMode(VisualSizeMode mode)
        {
            _sizeMode = mode;
            _sizeSegment.Select((int)mode, false);
        }

        private void ApplyLayout()
        {
            if (_demo == null) return;
            var padding = new VisualPadding(_padding);
            VisualLayoutSettings settings;
            RectTransform template;
            switch (_kind)
            {
                case VisualLayoutKind.Horizontal:
                    settings = VisualLayoutSettings.Horizontal(_size, _spacing);
                    settings.ItemSize = new Vector2(_size, _crossSize);
                    template = Templates.Cell;
                    break;
                case VisualLayoutKind.Grid:
                    settings = VisualLayoutSettings.Grid(new Vector2(_size, _size), _spacing, _constraint, _constraintCount);
                    template = Templates.Cell;
                    break;
                case VisualLayoutKind.Flow:
                    settings = VisualLayoutSettings.Flow(_spacing, new Vector2(100f, _size));
                    template = Templates.Tag;
                    break;
                default:
                    settings = VisualLayoutSettings.Vertical(_size, _spacing);
                    settings.ItemSize = new Vector2(_crossSize, _size);
                    template = _sizeMode == VisualSizeMode.Measure ? Templates.Note : Templates.Row;
                    break;
            }
            settings.Padding = padding;
            settings.CrossAlignment = _alignment;

            _demo.SizeProvider = _sizeMode == VisualSizeMode.Provider ? (Func<int, Vector2>)ProvideSize : null;
            _demo.SizeMode = _sizeMode;
            _demo.SetLayout(settings);
            _demo.ItemPrefab = template;
            _sizeNote.text = SizeNote();
            if (_itemSizeLabel != null) _itemSizeLabel.text = _size.ToString("0");
            UpdateStats();
        }

        private string SizeNote()
        {
            if (_kind == VisualLayoutKind.Grid) return "Grid uses one cell size, so the size mode is ignored.";
            switch (_sizeMode)
            {
                case VisualSizeMode.Provider: return "Provider: every 7th item is 84 px, the rest use Item size.";
                case VisualSizeMode.Measure: return _kind == VisualLayoutKind.Vertical ? "Measure: notes take the height of their wrapped text." : "Measure: each item is as long as its content.";
                default: return "Fixed: every item uses Item size.";
            }
        }

        private Vector2 ProvideSize(int index)
        {
            float length = index < _items.Count && _items[index].Height > ShowcaseTemplates.RowHeight ? 84f : _size;
            return new Vector2(_kind == VisualLayoutKind.Horizontal ? length : 0f, length);
        }

        private void SetData(int count)
        {
            var watch = Stopwatch.StartNew();
            _items = ShowcaseData.Create(count);
            long generated = watch.ElapsedMilliseconds;
            watch.Restart();
            _demo.SetItems(_items);
            _log.Add($"SetItems({count:N0}): data {generated} ms, bind {watch.ElapsedMilliseconds} ms");
            UpdateStats();
        }

        private void UpdateStats()
        {
            if (_demo == null || _stats == null) return;
            Vector2 content = _demo.Content.sizeDelta;
            _stats.text = $"{_demo.Count:N0} items  ·  visible {_demo.VisibleRange}  ·  alive {_demo.ActiveViewCount} / created {_demo.CreatedViewCount}\n" +
                          $"selected {_demo.SelectedCount}  ·  focused {_demo.FocusedIndex}  ·  content {content.x:0} x {content.y:0}  ·  empty placeholder {(_demo.IsShowingEmpty ? "shown" : "hidden")}";
            _bar.Show(_demo.VisibleRange, _demo.Count);
        }
        #endregion
    }
}
