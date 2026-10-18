using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class HorizontalVisualLayout : VisualLayout
    {
        #region [Variables/Fields]
        private float _left;
        private float _top;
        private float _width;
        private float _height;
        private float _step;
        private float[] _offsets;
        private float[] _lengths;
        private bool _variable;
        #endregion

        #region [Properties]
        public override VisualLayoutKind Kind => VisualLayoutKind.Horizontal;
        public override bool ScrollsVertically => false;
        public override bool SupportsVariableSize => true;
        #endregion

        #region [Public Methods]
        public override void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count, VisualSizeTable sizes)
        {
            Count = Mathf.Max(0, count);
            VisualPadding padding = settings.Padding;
            float available = Mathf.Max(0f, viewportSize.y - padding.Vertical);
            _left = padding.Left;
            _height = settings.ItemSize.y > 0f ? settings.ItemSize.y : available;
            _top = padding.Top + AlignOffset(settings.CrossAlignment, available, _height);
            float contentHeight = settings.CrossAlignment == VisualAlignment.Start ? _height : Mathf.Max(_height, available);
            _width = ResolveLength(settings.ItemSize.x, prefabSize.x);
            _step = _width + settings.Spacing.x;
            _variable = sizes != null && sizes.Count >= Count;

            if (!_variable)
            {
                ContentSize = new Vector2(padding.Horizontal + Span(Count, _width, settings.Spacing.x), padding.Vertical + contentHeight);
                return;
            }

            _offsets = EnsureCapacity(_offsets, Count);
            _lengths = EnsureCapacity(_lengths, Count);
            float x = _left;
            for (int i = 0; i < Count; i++)
            {
                float length = sizes.Get(i).x;
                if (length <= 0f) length = _width;
                _offsets[i] = x;
                _lengths[i] = length;
                x += length + settings.Spacing.x;
            }
            float total = Count > 0 ? x - settings.Spacing.x - _left : 0f;
            ContentSize = new Vector2(padding.Horizontal + total, padding.Vertical + contentHeight);
        }

        public override Rect GetItemRect(int index)
        {
            if (_variable) return new Rect(_offsets[index], _top, _lengths[index], _height);
            return new Rect(_left + index * _step, _top, _width, _height);
        }

        public override VisualRange GetRange(Rect visibleArea)
        {
            if (Count == 0) return VisualRange.Empty;
            if (!_variable)
            {
                if (_step <= 0f) return VisualRange.Empty;
                int first = Mathf.FloorToInt((visibleArea.xMin - _left) / _step);
                int last = Mathf.FloorToInt((visibleArea.xMax - _left) / _step);
                return VisualRange.Clamp(first, last, Count);
            }
            int start = FirstEndingAfter(visibleArea.xMin);
            int end = LastStartingBefore(visibleArea.xMax);
            return end < start ? VisualRange.Empty : new VisualRange(start, end);
        }
        #endregion

        #region [Private Methods]
        private int FirstEndingAfter(float position)
        {
            int low = 0;
            int high = Count - 1;
            int result = Count;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_offsets[mid] + _lengths[mid] > position)
                {
                    result = mid;
                    high = mid - 1;
                }
                else low = mid + 1;
            }
            return result;
        }

        private int LastStartingBefore(float position)
        {
            int low = 0;
            int high = Count - 1;
            int result = -1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_offsets[mid] <= position)
                {
                    result = mid;
                    low = mid + 1;
                }
                else high = mid - 1;
            }
            return result;
        }
        #endregion
    }
}
