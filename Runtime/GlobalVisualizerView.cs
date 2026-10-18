using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollRect))]
    [AddComponentMenu("UI/Global Visualizer")]
    public sealed class GlobalVisualizerView : MonoBehaviour, IMoveHandler, ISubmitHandler
    {
        #region [Variables/Fields]
        private const int MaxMeasurePasses = 4;
        private const int MaxRevealPasses = 3;

        [Tooltip("Template for every item. Any prefab with a RectTransform works.")]
        [SerializeField] private RectTransform _itemPrefab;

        [SerializeField] private VisualLayoutSettings _layout = new VisualLayoutSettings();

        [Tooltip("Fixed: every item uses Item Size. Provider: sizes come from SizeProvider in code. Measure: each item is measured from its layout components after binding.")]
        [SerializeField] private VisualSizeMode _sizeMode = VisualSizeMode.Fixed;

        [Tooltip("Shown inside the viewport when the data source is set but empty. Optional.")]
        [SerializeField] private RectTransform _emptyPrefab;

        [Tooltip("Extra pixels around the viewport where items stay alive, so fast scrolling shows no gaps.")]
        [SerializeField, Min(0f)] private float _bufferPixels = 120f;

        [Tooltip("Turns ScrollRect.vertical / horizontal on or off to match the layout.")]
        [SerializeField] private bool _configureScrollRect = true;

        [Tooltip("Adds click, submit and keyboard/gamepad navigation to every item view.")]
        [SerializeField] private bool _handleInput = true;

        [Tooltip("A mouse click also selects the item in the EventSystem, so the keyboard or gamepad continues from it. Turn off when the same keys move the player.")]
        [SerializeField] private bool _focusOnClick = true;

        [SerializeField] private VisualSelectionMode _selectionMode = VisualSelectionMode.None;

        [Tooltip("After a drag or wheel scroll ends, glide so an item lines up with the viewport edge.")]
        [SerializeField] private bool _snapToItems;

        [Tooltip("Minimum angle in degrees between a drag and the scroll axis that starts an item drag instead of scrolling. Needs a DragHandler. 0 = always drag items.")]
        [SerializeField, Range(0f, 90f)] private float _itemDragAngle = 45f;

        public event Action<VisualRange> OnVisibleRangeChanged;
        public event Action<int, PointerEventData> OnItemClicked;
        public event Action<int, PointerEventData> OnItemDoubleClicked;
        public event Action<int, PointerEventData> OnItemRightClicked;
        public event Action<int> OnItemSubmitted;
        public event Action<int> OnFocusChanged;
        public event Action<VisualDirection> OnNavigateOut;
        public event Action OnSelectionChanged;

        private readonly Dictionary<int, RectTransform> _active = new Dictionary<int, RectTransform>();
        private readonly List<int> _leaving = new List<int>();
        private readonly Stack<RectTransform> _recycled = new Stack<RectTransform>();
        private readonly Dictionary<RectTransform, VisualItemInput> _inputs = new Dictionary<RectTransform, VisualItemInput>();
        private readonly Dictionary<RectTransform, VisualItemDrag> _drags = new Dictionary<RectTransform, VisualItemDrag>();
        private readonly Dictionary<RectTransform, IVisualItemState[]> _states = new Dictionary<RectTransform, IVisualItemState[]>();
        private readonly VisualSizer _sizer = new VisualSizer();
        private readonly VisualEmptyState _empty = new VisualEmptyState();
        private readonly VisualSelection _selection = new VisualSelection();
        private ScrollRect _scrollRect;
        private RectTransform _viewport;
        private RectTransform _content;
        private VisualLayout _engine;
        private VisualPool _pool;
        private VisualBinder _binder;
        private IVisualDragHandler _dragHandler;
        private VisualScrollSnap _snap;
        private UnityAction<Vector2> _scrollListener;
        private VisualRange _range = VisualRange.Empty;
        private Vector2 _lastViewportSize;
        private HideFlags _creationFlags = HideFlags.None;
        private int _focused = -1;
        private int _layoutVersion;
        private bool _initialized;
        private bool _listening;
        private bool _refreshQueued;
        #endregion

        #region [Properties]
        public int Count => _binder != null ? _binder.Count : 0;
        public VisualRange VisibleRange => _range;
        public int ActiveViewCount => _active.Count;
        public int CreatedViewCount => _pool != null ? _pool.CreatedCount : 0;
        public VisualLayoutSettings LayoutSettings => _layout;
        public bool IsShowingEmpty => _empty.IsShowing;
        public int FocusedIndex => _focused;
        public int SelectedIndex => _selection.First;
        public int SelectedCount => _selection.Count;
        internal bool IsEditorPreview { get; private set; }
        internal VisualSizeTable SizeTable => _sizer.Table;
        private bool UsesSizeTable => _sizeMode != VisualSizeMode.Fixed && _layout.Kind != VisualLayoutKind.Grid;
        private bool InputEnabled => _handleInput && !IsEditorPreview;

        public VisualLayout Layout
        {
            get
            {
                EnsureInitialized();
                EnsureEngine();
                return _engine;
            }
        }

        public ScrollRect ScrollRect
        {
            get
            {
                EnsureInitialized();
                return _scrollRect;
            }
        }

        public RectTransform Viewport
        {
            get
            {
                EnsureInitialized();
                return _viewport;
            }
        }

        public RectTransform Content
        {
            get
            {
                EnsureInitialized();
                return _content;
            }
        }

        public Vector2 ScrollPosition
        {
            get
            {
                EnsureInitialized();
                return new Vector2(-_content.anchoredPosition.x, _content.anchoredPosition.y);
            }
            set
            {
                EnsureInitialized();
                _content.anchoredPosition = new Vector2(-value.x, value.y);
                UpdateVisibleItems();
            }
        }

        public RectTransform ItemPrefab
        {
            get => _itemPrefab;
            set
            {
                if (_itemPrefab == value) return;
                ReleaseAll();
                DisposePool();
                _sizer.Reset();
                _itemPrefab = value;
                Refresh();
            }
        }

        public RectTransform EmptyPrefab
        {
            get => _emptyPrefab;
            set
            {
                if (_emptyPrefab == value) return;
                _emptyPrefab = value;
                if (_initialized) UpdateEmpty();
            }
        }

        public VisualSizeMode SizeMode
        {
            get => _sizeMode;
            set
            {
                if (_sizeMode == value) return;
                _sizeMode = value;
                _sizer.Reset();
                Refresh(true);
            }
        }

        public Func<int, Vector2> SizeProvider
        {
            get => _sizer.Provider;
            set
            {
                _sizer.Provider = value;
                _sizer.Reset();
                if (_initialized) Refresh(true);
            }
        }

        public float BufferPixels
        {
            get => _bufferPixels;
            set
            {
                _bufferPixels = Mathf.Max(0f, value);
                UpdateVisibleItems();
            }
        }

        public bool HandleInput
        {
            get => _handleInput;
            set => _handleInput = value;
        }

        public bool FocusOnClick
        {
            get => _focusOnClick;
            set => _focusOnClick = value;
        }

        public VisualSelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                _selectionMode = value;
                _selection.Mode = value;
                if (value == VisualSelectionMode.None) ClearSelection();
            }
        }

        public bool SnapToItems
        {
            get => _snapToItems;
            set
            {
                _snapToItems = value;
                EnsureSnap();
            }
        }

        public float ItemDragAngle
        {
            get => _itemDragAngle;
            set => _itemDragAngle = Mathf.Clamp(value, 0f, 90f);
        }

        public IVisualDragHandler DragHandler
        {
            get => _dragHandler;
            set
            {
                _dragHandler = value;
                foreach (KeyValuePair<int, RectTransform> pair in _active) AttachInput(pair.Value, pair.Key);
            }
        }
        #endregion

        #region [Unity Methods]
        private void Awake()
        {
            if (Application.isPlaying) EnsureInitialized();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying && _binder == null) return;
            EnsureInitialized();
            if (!_listening)
            {
                _scrollRect.onValueChanged.AddListener(_scrollListener);
                _listening = true;
            }
            EnsureSnap();
            Refresh();
        }

        private void OnDisable()
        {
            if (_listening && _scrollRect != null) _scrollRect.onValueChanged.RemoveListener(_scrollListener);
            _listening = false;
            CancelQueuedRefresh();
        }

        private void OnRectTransformDimensionsChange()
        {
            HandleResize();
        }

        private void OnDestroy()
        {
            CancelQueuedRefresh();
            if (!Application.isPlaying)
            {
                ForgetViews();
                return;
            }
            ReleaseAll();
            DisposePool();
            _empty.Dispose();
        }
        #endregion

        #region [Public Methods]
        public void SetItems<T>(IReadOnlyList<T> items, Action<GameObject, T, int> onBind = null)
        {
            EnsureInitialized();
            ReleaseAll();
            _binder = new ListVisualBinder<T>(items, onBind);
            _sizer.Reset();
            ClampInteraction();
            Refresh();
        }

        public void SetCount(int count, Action<GameObject, int> onBind = null)
        {
            Action<GameObject, int, int> bind = null;
            if (onBind != null) bind = (view, value, index) => onBind(view, index);
            SetItems(new VisualIndexList(count), bind);
        }

        public void Clear()
        {
            EnsureInitialized();
            ReleaseAll();
            _binder = null;
            _sizer.Reset();
            ClampInteraction();
            Refresh();
        }

        public void SetLayout(VisualLayoutSettings settings)
        {
            _layout = settings ?? new VisualLayoutSettings();
            _sizer.Reset();
            Refresh(true);
        }

        public void Refresh()
        {
            Refresh(false);
        }

        public void Refresh(bool rebindVisible)
        {
            EnsureInitialized();
            EnsureEngine();
            Vector2 viewport = ViewportSize();
            _lastViewportSize = viewport;
            RebuildEngine(viewport);
            if (_configureScrollRect)
            {
                _scrollRect.vertical = _engine.ScrollsVertically;
                _scrollRect.horizontal = !_engine.ScrollsVertically;
            }
            SetScroll(ScrollPosition, viewport);
            Sync(true, rebindVisible);
        }

        public void UpdateVisibleItems()
        {
            if (!_initialized || _engine == null) return;
            Sync(false, false);
        }

        public void InvalidateSizes()
        {
            _sizer.Reset();
            if (_initialized) Refresh(true);
        }

        public void NotifyChanged(int index)
        {
            NotifyRangeChanged(index, 1);
        }

        public void NotifyRangeChanged(int index, int count)
        {
            if (_binder == null || _engine == null) return;
            int end = Mathf.Min(index + count, Count);
            for (int i = Mathf.Max(0, index); i < end; i++)
            {
                if (UsesSizeTable) _sizer.Table.Invalidate(i);
                if (!_active.TryGetValue(i, out RectTransform view)) continue;
                _binder.Unbind(view);
                BindView(view, i);
            }
            if (!UsesSizeTable) return;
            Relayout();
            Sync(true, false);
        }

        public void NotifyInserted(int index, int count)
        {
            if (count <= 0) return;
            if (UsesSizeTable) _sizer.Table.Insert(index, count);
            bool selectionChanged = _selection.OnInserted(index, count);
            if (_focused >= index) _focused += count;
            Refresh(true);
            if (selectionChanged) OnSelectionChanged?.Invoke();
        }

        public void NotifyRemoved(int index, int count)
        {
            if (count <= 0) return;
            if (UsesSizeTable) _sizer.Table.Remove(index, count);
            bool selectionChanged = _selection.OnRemoved(index, count);
            if (_focused >= index + count) _focused -= count;
            else if (_focused >= index) _focused = Mathf.Min(index, Count - 1);
            Refresh(true);
            if (selectionChanged) OnSelectionChanged?.Invoke();
        }

        public void NotifyReset()
        {
            _sizer.Reset();
            bool selectionChanged = _selection.Clamp(Count);
            if (_focused >= Count) _focused = Count - 1;
            Refresh(true);
            if (selectionChanged) OnSelectionChanged?.Invoke();
        }

        public void ScrollTo(int index, float alignment = 0f)
        {
            EnsureInitialized();
            EnsureEngine();
            if (Count == 0) return;
            index = Mathf.Clamp(index, 0, Count - 1);
            _scrollRect.StopMovement();
            for (int pass = 0; pass < MaxRevealPasses; pass++)
            {
                int version = _layoutVersion;
                Vector2 position = _engine.GetScrollPosition(index, ViewportSize(), alignment);
                _content.anchoredPosition = new Vector2(-position.x, position.y);
                Sync(false, false);
                if (version == _layoutVersion) break;
            }
        }

        public void EnsureVisible(int index)
        {
            EnsureInitialized();
            EnsureEngine();
            if (Count == 0) return;
            index = Mathf.Clamp(index, 0, Count - 1);
            for (int pass = 0; pass < MaxRevealPasses; pass++)
            {
                int version = _layoutVersion;
                Vector2 viewport = ViewportSize();
                Vector2 scroll = ScrollPosition;
                Vector2 target = RevealPosition(_engine.GetItemRect(index), scroll, viewport);
                if (target == scroll) break;
                _scrollRect.StopMovement();
                SetScroll(target, viewport);
                Sync(false, false);
                if (version == _layoutVersion) break;
            }
        }

        public RectTransform GetView(int index)
        {
            return _active.TryGetValue(index, out RectTransform view) ? view : null;
        }

        public bool IsSelected(int index)
        {
            return _selection.Contains(index);
        }

        public void GetSelected(List<int> results)
        {
            if (results == null) return;
            _selection.CopyTo(results);
            results.Sort();
        }

        public void Select(int index, bool selected = true)
        {
            if (index < 0 || index >= Count) return;
            _selection.Mode = _selectionMode;
            if (_selection.Set(index, selected)) SelectionChanged();
        }

        public void ClearSelection()
        {
            if (_selection.Clear()) SelectionChanged();
        }

        public void Focus(int index)
        {
            SetFocus(index, true, true);
        }

        public bool MoveFocus(VisualDirection direction)
        {
            if (Count == 0) return false;
            if (_focused < 0 || _focused >= Count)
            {
                Focus(_range.IsEmpty ? 0 : _range.First);
                return true;
            }
            int next = Layout.GetNeighbor(_focused, direction);
            if (next < 0)
            {
                OnNavigateOut?.Invoke(direction);
                return false;
            }
            Focus(next);
            return true;
        }

        public void OnMove(AxisEventData eventData)
        {
            if (!InputEnabled || Count == 0) return;
            if (_focused >= 0 && _focused < Count)
            {
                HandleMove(_focused, eventData);
                return;
            }
            eventData.Use();
            Focus(_range.IsEmpty ? 0 : _range.First);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (InputEnabled && _focused >= 0 && _focused < Count) HandleSubmit(_focused);
        }

        internal void HandleResize()
        {
            if (!_initialized || !isActiveAndEnabled) return;
            if (!Application.isPlaying && _binder == null) return;
            if (ViewportSize() == _lastViewportSize) return;
            if (IsCanvasRebuilding()) QueueRefresh();
            else Refresh();
        }

        internal void HandlePointerClick(int index, PointerEventData eventData)
        {
            HandleClick(index, eventData, VisualInputModifiers.Additive, VisualInputModifiers.Range);
        }

        internal void HandleClick(int index, PointerEventData eventData, bool additive, bool range)
        {
            if (index < 0 || index >= Count) return;
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnItemRightClicked?.Invoke(index, eventData);
                return;
            }
            if (eventData.button != PointerEventData.InputButton.Left) return;
            SetFocus(index, false, _focusOnClick);
            ApplyClickSelection(index, additive, range);
            OnItemClicked?.Invoke(index, eventData);
            if (eventData.clickCount == 2) OnItemDoubleClicked?.Invoke(index, eventData);
        }

        internal void HandleMove(int index, AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.None) return;
            VisualDirection direction = ToDirection(eventData.moveDir);
            int next = Layout.GetNeighbor(index, direction);
            if (next < 0)
            {
                OnNavigateOut?.Invoke(direction);
                return;
            }
            eventData.Use();
            SetFocus(next, true, true);
        }

        internal void HandleSubmit(int index)
        {
            if (index < 0 || index >= Count) return;
            ApplyClickSelection(index, false, false);
            OnItemSubmitted?.Invoke(index);
        }

        internal void HandleSelect(int index)
        {
            if (index != _focused) SetFocus(index, false, false);
        }

        internal bool ShouldStartItemDrag(int index, PointerEventData eventData)
        {
            if (_dragHandler == null || index < 0 || index >= Count) return false;
            if (!_dragHandler.CanBeginDrag(Info(index))) return false;
            if (!CanScroll()) return true;
            Vector2 delta = eventData.position - eventData.pressPosition;
            float along = Mathf.Abs(_engine.ScrollsVertically ? delta.y : delta.x);
            float across = Mathf.Abs(_engine.ScrollsVertically ? delta.x : delta.y);
            float angle = Mathf.Atan2(across, along) * Mathf.Rad2Deg;
            return angle >= _itemDragAngle;
        }

        internal void HandleItemBeginDrag(int index, PointerEventData eventData)
        {
            _scrollRect.StopMovement();
            _dragHandler?.OnBeginDrag(Info(index), eventData);
        }

        internal void HandleItemDrag(int index, PointerEventData eventData)
        {
            _dragHandler?.OnDrag(Info(index), eventData);
        }

        internal void HandleItemEndDrag(int index, PointerEventData eventData)
        {
            _dragHandler?.OnEndDrag(Info(index), eventData);
        }

        internal Vector2 GetSnapPosition()
        {
            Vector2 viewport = ViewportSize();
            Vector2 scroll = ScrollPosition;
            if (_engine == null || Count == 0) return scroll;
            bool vertical = _engine.ScrollsVertically;
            Rect probe = vertical ? new Rect(scroll.x, scroll.y, viewport.x, 1f) : new Rect(scroll.x, scroll.y, 1f, viewport.y);
            VisualRange range = _engine.GetRange(probe);
            if (range.IsEmpty) return _engine.ClampScrollPosition(scroll, viewport);

            Rect rect = _engine.GetItemRect(range.First);
            float position = vertical ? scroll.y : scroll.x;
            float start = vertical ? rect.yMin : rect.xMin;
            float next = vertical ? rect.yMax + _layout.Spacing.y : rect.xMax + _layout.Spacing.x;
            float padding = vertical ? _layout.Padding.Top : _layout.Padding.Left;
            if (start <= padding + 0.5f) start = 0f;
            float target = position - start <= next - position ? start : next;
            Vector2 result = vertical ? new Vector2(scroll.x, target) : new Vector2(target, scroll.y);
            return _engine.ClampScrollPosition(result, viewport);
        }

        internal void BeginEditorPreview(int count)
        {
            if (Application.isPlaying) return;
            EnsureInitialized();
            ReleaseAll();
            DisposePool();
            _empty.Dispose();
            _creationFlags = HideFlags.DontSave;
            IsEditorPreview = true;
            SetCount(count);
        }

        internal void EndEditorPreview()
        {
            if (!IsEditorPreview) return;
            ReleaseAll();
            DisposePool();
            _empty.Dispose();
            _binder = null;
            _creationFlags = HideFlags.None;
            IsEditorPreview = false;
            Refresh();
        }
        #endregion

        #region [Private Methods]
        private void EnsureInitialized()
        {
            if (_initialized) return;
            _scrollRect = GetComponent<ScrollRect>();
            _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : (RectTransform)transform;
            _content = _scrollRect.content;
            if (_content == null)
            {
                _content = CreateContent(_viewport);
                _scrollRect.content = _content;
            }
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 1f);
            _content.anchoredPosition = Vector2.zero;
            DisableLayoutDrivers(_content);
            WatchViewport();
            _selection.Mode = _selectionMode;
            _scrollListener = OnScrollChanged;
            _initialized = true;
        }

        private void EnsureEngine()
        {
            if (_engine == null || _engine.Kind != _layout.Kind) _engine = VisualLayout.Create(_layout.Kind);
        }

        private void EnsurePool()
        {
            if (_pool != null && _pool.Prefab == _itemPrefab) return;
            ReleaseAll();
            DisposePool();
            _pool = new VisualPool(_itemPrefab, _content, _creationFlags);
        }

        private void EnsureSnap()
        {
            if (!_initialized) return;
            if (_snap == null) _snap = GetComponent<VisualScrollSnap>();
            if (_snap == null && _snapToItems && Application.isPlaying) _snap = gameObject.AddComponent<VisualScrollSnap>();
            if (_snap != null) _snap.Setup(this, _scrollRect);
        }

        private void DisposePool()
        {
            if (_pool != null)
            {
                _pool.Clear();
                _pool = null;
            }
            _inputs.Clear();
            _drags.Clear();
            _states.Clear();
            _binder?.ClearCache();
        }

        private void WatchViewport()
        {
            if (_viewport == null || _viewport == transform) return;
            VisualViewportWatcher watcher = _viewport.GetComponent<VisualViewportWatcher>();
            if (watcher == null)
            {
                watcher = _viewport.gameObject.AddComponent<VisualViewportWatcher>();
                watcher.hideFlags = HideFlags.DontSave | HideFlags.HideInInspector;
            }
            watcher.Attach(this);
        }

        private void QueueRefresh()
        {
            if (_refreshQueued) return;
            _refreshQueued = true;
            Canvas.willRenderCanvases += RunQueuedRefresh;
        }

        private void CancelQueuedRefresh()
        {
            if (!_refreshQueued) return;
            _refreshQueued = false;
            Canvas.willRenderCanvases -= RunQueuedRefresh;
        }

        private void RunQueuedRefresh()
        {
            CancelQueuedRefresh();
            if (this == null || !isActiveAndEnabled || !_initialized) return;
            if (IsCanvasRebuilding())
            {
                QueueRefresh();
                return;
            }
            Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private static bool IsCanvasRebuilding()
        {
            return CanvasUpdateRegistry.IsRebuildingLayout() || CanvasUpdateRegistry.IsRebuildingGraphics();
        }

        private void ForgetViews()
        {
            _active.Clear();
            _recycled.Clear();
            _inputs.Clear();
            _drags.Clear();
            _states.Clear();
            _pool = null;
            _empty.Forget();
        }

        private void RebuildEngine(Vector2 viewport)
        {
            if (UsesSizeTable) _sizer.Prepare(_sizeMode, _layout, PrefabSize(), Count, viewport.x);
            _engine.Rebuild(_layout, viewport, PrefabSize(), Count, UsesSizeTable ? _sizer.Table : null);
            _content.sizeDelta = _engine.ContentSize;
        }

        private void Relayout()
        {
            Vector2 viewport = ViewportSize();
            Vector2 scroll = ScrollPosition;
            bool vertical = _engine.ScrollsVertically;
            int anchor = -1;
            float offset = 0f;
            VisualRange visible = _engine.GetRange(new Rect(scroll.x, scroll.y, viewport.x, viewport.y));
            if (!visible.IsEmpty)
            {
                anchor = visible.First;
                Rect before = _engine.GetItemRect(anchor);
                offset = vertical ? before.y - scroll.y : before.x - scroll.x;
            }

            RebuildEngine(viewport);
            _layoutVersion++;

            if (anchor >= 0 && anchor < Count)
            {
                Rect after = _engine.GetItemRect(anchor);
                if (vertical) scroll.y = after.y - offset;
                else scroll.x = after.x - offset;
            }
            SetScroll(scroll, viewport);
        }

        private void Sync(bool reposition, bool rebind)
        {
            SyncPass(reposition, rebind);
            if (_sizeMode != VisualSizeMode.Measure || !UsesSizeTable || _active.Count == 0 || !_sizer.CanMeasure(_itemPrefab)) return;
            float available = Mathf.Max(1f, ViewportSize().x - _layout.Padding.Horizontal);
            for (int pass = 0; pass < MaxMeasurePasses; pass++)
            {
                if (!_sizer.MeasureActive(_active, _layout.Kind, available)) break;
                Relayout();
                SyncPass(true, false);
            }
        }

        private void SyncPass(bool reposition, bool rebind)
        {
            if (_binder == null || _itemPrefab == null || Count == 0)
            {
                ReleaseAll();
                UpdateEmpty();
                return;
            }

            UpdateEmpty();
            EnsurePool();
            VisualRange next = _engine.GetRange(VisibleArea());

            _leaving.Clear();
            foreach (KeyValuePair<int, RectTransform> pair in _active)
            {
                if (!next.Contains(pair.Key)) _leaving.Add(pair.Key);
            }
            for (int i = 0; i < _leaving.Count; i++)
            {
                int index = _leaving[i];
                RectTransform view = _active[index];
                _active.Remove(index);
                ReleaseView(view);
                _recycled.Push(view);
            }

            if (reposition || rebind)
            {
                foreach (KeyValuePair<int, RectTransform> pair in _active)
                {
                    if (reposition) Place(pair.Value, pair.Key);
                    if (!rebind) continue;
                    _binder.Unbind(pair.Value);
                    BindView(pair.Value, pair.Key);
                }
            }

            for (int index = next.First; index <= next.Last; index++)
            {
                if (_active.ContainsKey(index)) continue;
                RectTransform view = _recycled.Count > 0 ? _recycled.Pop() : _pool.Get();
                Place(view, index);
                if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                _active.Add(index, view);
                BindView(view, index);
            }

            while (_recycled.Count > 0) _pool.Release(_recycled.Pop());
            RestoreSystemFocus();
            SetRange(next);
        }

        private void BindView(RectTransform view, int index)
        {
            VisualItemInfo info = Info(index);
            AttachInput(view, index);
            _binder.Bind(view, info);
            NotifyState(view, info);
        }

        private void ReleaseView(RectTransform view)
        {
            _binder?.Unbind(view);
            if (_inputs.TryGetValue(view, out VisualItemInput input) && input != null) input.Attach(this, -1);
            if (_drags.TryGetValue(view, out VisualItemDrag drag) && drag != null) drag.Attach(this, -1);
            HandOffSystemFocus(view);
        }

        private void ReleaseAll()
        {
            if (_active.Count > 0)
            {
                foreach (KeyValuePair<int, RectTransform> pair in _active)
                {
                    ReleaseView(pair.Value);
                    _pool?.Release(pair.Value);
                }
                _active.Clear();
            }
            SetRange(VisualRange.Empty);
        }

        private void AttachInput(RectTransform view, int index)
        {
            if (!InputEnabled) return;
            if (!_inputs.TryGetValue(view, out VisualItemInput input))
            {
                input = view.GetComponent<VisualItemInput>();
                if (input == null) input = view.gameObject.AddComponent<VisualItemInput>();
                Selectable selectable = view.GetComponent<Selectable>();
                if (selectable != null) selectable.navigation = new Navigation { mode = Navigation.Mode.None };
                _inputs.Add(view, input);
            }
            input.Attach(this, index);

            if (_dragHandler == null) return;
            if (!_drags.TryGetValue(view, out VisualItemDrag drag))
            {
                drag = CreateDrag(view);
                _drags.Add(view, drag);
            }
            if (drag != null) drag.Attach(this, index);
        }

        private void NotifyState(RectTransform view, VisualItemInfo info)
        {
            if (!_states.TryGetValue(view, out IVisualItemState[] states))
            {
                states = view.GetComponentsInChildren<IVisualItemState>(true);
                _states.Add(view, states);
            }
            for (int i = 0; i < states.Length; i++) states[i].OnStateChanged(info);
        }

        private void UpdateViewState(int index)
        {
            if (_binder == null || !_active.TryGetValue(index, out RectTransform view)) return;
            VisualItemInfo info = Info(index);
            _binder.UpdateState(view, info);
            NotifyState(view, info);
        }

        private void SelectionChanged()
        {
            foreach (KeyValuePair<int, RectTransform> pair in _active)
            {
                VisualItemInfo info = Info(pair.Key);
                _binder.UpdateState(pair.Value, info);
                NotifyState(pair.Value, info);
            }
            OnSelectionChanged?.Invoke();
        }

        private void ApplyClickSelection(int index, bool additive, bool range)
        {
            _selection.Mode = _selectionMode;
            if (_selection.Click(index, additive && _selectionMode == VisualSelectionMode.Multi, range && _selectionMode == VisualSelectionMode.Multi)) SelectionChanged();
        }

        private void SetFocus(int index, bool reveal, bool selectInSystem)
        {
            if (Count == 0) return;
            index = Mathf.Clamp(index, 0, Count - 1);
            int previous = _focused;
            _focused = index;
            if (reveal) EnsureVisible(index);
            if (selectInSystem && InputEnabled) SelectInSystem(index);
            if (previous == index) return;
            UpdateViewState(previous);
            UpdateViewState(index);
            OnFocusChanged?.Invoke(index);
        }

        private void SelectInSystem(int index)
        {
            EventSystem system = EventSystem.current;
            if (system == null || system.alreadySelecting) return;
            RectTransform view = GetView(index);
            GameObject target = view != null ? view.gameObject : gameObject;
            if (system.currentSelectedGameObject != target) system.SetSelectedGameObject(target);
        }

        private void HandOffSystemFocus(RectTransform view)
        {
            if (!InputEnabled || _focused < 0) return;
            EventSystem system = EventSystem.current;
            if (system == null || system.alreadySelecting || system.currentSelectedGameObject != view.gameObject) return;
            system.SetSelectedGameObject(gameObject);
        }

        private void RestoreSystemFocus()
        {
            if (!InputEnabled || _focused < 0) return;
            EventSystem system = EventSystem.current;
            if (system == null || system.alreadySelecting || system.currentSelectedGameObject != gameObject) return;
            if (_active.TryGetValue(_focused, out RectTransform view)) system.SetSelectedGameObject(view.gameObject);
        }

        private void ClampInteraction()
        {
            int count = Count;
            if (_selection.Clamp(count)) OnSelectionChanged?.Invoke();
            if (_focused >= count) _focused = count - 1;
        }

        private VisualItemInfo Info(int index)
        {
            return new VisualItemInfo(index, this, _selection.Contains(index), index == _focused);
        }

        private VisualItemDrag CreateDrag(RectTransform view)
        {
            VisualItemDrag drag = view.GetComponent<VisualItemDrag>();
            if (drag != null) return drag;
            foreach (IDragHandler handler in view.GetComponents<IDragHandler>())
            {
                if (!(handler is VisualItemDrag)) return null;
            }
            return view.gameObject.AddComponent<VisualItemDrag>();
        }

        private bool CanScroll()
        {
            Vector2 viewport = ViewportSize();
            Vector2 content = _engine.ContentSize;
            return _engine.ScrollsVertically ? content.y > viewport.y + 0.5f : content.x > viewport.x + 0.5f;
        }

        private Vector2 RevealPosition(Rect rect, Vector2 scroll, Vector2 viewport)
        {
            Vector2 target = scroll;
            if (_engine.ScrollsVertically)
            {
                if (rect.yMin < scroll.y) target.y = rect.yMin;
                else if (rect.yMax > scroll.y + viewport.y) target.y = rect.yMax - viewport.y;
            }
            else
            {
                if (rect.xMin < scroll.x) target.x = rect.xMin;
                else if (rect.xMax > scroll.x + viewport.x) target.x = rect.xMax - viewport.x;
            }
            return _engine.ClampScrollPosition(target, viewport);
        }

        private void SetScroll(Vector2 position, Vector2 viewport)
        {
            Vector2 clamped = _engine.ClampScrollPosition(position, viewport);
            _content.anchoredPosition = new Vector2(-clamped.x, clamped.y);
        }

        private void UpdateEmpty()
        {
            _empty.Update(_binder != null && Count == 0 && _emptyPrefab != null, _emptyPrefab, _viewport, _creationFlags);
        }

        private void Place(RectTransform view, int index)
        {
            Rect rect = _engine.GetItemRect(index);
            view.anchoredPosition = new Vector2(rect.x, -rect.y);
            view.sizeDelta = rect.size;
        }

        private Rect VisibleArea()
        {
            Vector2 viewport = ViewportSize();
            float x = -_content.anchoredPosition.x - _bufferPixels;
            float y = _content.anchoredPosition.y - _bufferPixels;
            return new Rect(x, y, viewport.x + _bufferPixels * 2f, viewport.y + _bufferPixels * 2f);
        }

        private Vector2 ViewportSize()
        {
            return _viewport.rect.size;
        }

        private Vector2 PrefabSize()
        {
            return _itemPrefab != null ? _itemPrefab.rect.size : Vector2.zero;
        }

        private void SetRange(VisualRange next)
        {
            if (next.Equals(_range)) return;
            _range = next;
            OnVisibleRangeChanged?.Invoke(next);
        }

        private void OnScrollChanged(Vector2 normalizedPosition)
        {
            UpdateVisibleItems();
        }

        private static VisualDirection ToDirection(MoveDirection direction)
        {
            switch (direction)
            {
                case MoveDirection.Up: return VisualDirection.Up;
                case MoveDirection.Down: return VisualDirection.Down;
                case MoveDirection.Left: return VisualDirection.Left;
                default: return VisualDirection.Right;
            }
        }

        private static RectTransform CreateContent(RectTransform parent)
        {
            var content = new GameObject("Content", typeof(RectTransform));
            var rect = (RectTransform)content.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void DisableLayoutDrivers(RectTransform content)
        {
            LayoutGroup group = content.GetComponent<LayoutGroup>();
            if (group != null && group.enabled)
            {
                group.enabled = false;
                Debug.LogWarning($"Global Visualizer disabled {group.GetType().Name} on '{content.name}': item positions are computed by the visualizer.", content);
            }
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.enabled)
            {
                fitter.enabled = false;
                Debug.LogWarning($"Global Visualizer disabled ContentSizeFitter on '{content.name}': content size is computed by the visualizer.", content);
            }
        }
        #endregion
    }
}
