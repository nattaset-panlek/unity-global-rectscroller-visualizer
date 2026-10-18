using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class VerticalVisualLayout : VisualLayout
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
        public override VisualLayoutKind Kind => VisualLayoutKind.Vertical;
        public override bool ScrollsVertically => true;
        public override bool SupportsVariableSize => true;
        #endregion

        #region [Public Methods]
        public override void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count, VisualSizeTable sizes)
        {
            Count = Mathf.Max(0, count);
            VisualPadding padding = settings.Padding;
            float available = Mathf.Max(0f, viewportSize.x - padding.Horizontal);
            _top = padding.Top;
            _width = settings.ItemSize.x > 0f ? settings.ItemSize.x : available;
            _left = padding.Left + AlignOffset(settings.CrossAlignment, available, _width);
            float contentWidth = settings.CrossAlignment == VisualAlignment.Start ? _width : Mathf.Max(_width, available);
            _height = ResolveLength(settings.ItemSize.y, prefabSize.y);
            _step = _height + settings.Spacing.y;
            _variable = sizes != null && sizes.Count >= Count;

            if (!_variable)
            {
                ContentSize = new Vector2(padding.Horizontal + contentWidth, padding.Vertical + Span(Count, _height, settings.Spacing.y));
                return;
            }

            _offsets = EnsureCapacity(_offsets, Count);
            _lengths = EnsureCapacity(_lengths, Count);
            float y = _top;
            for (int i = 0; i < Count; i++)
            {
                float length = sizes.Get(i).y;
                if (length <= 0f) length = _height;
                _offsets[i] = y;
                _lengths[i] = length;
                y += length + settings.Spacing.y;
            }
            float total = Count > 0 ? y - settings.Spacing.y - _top : 0f;
            ContentSize = new Vector2(padding.Horizontal + contentWidth, padding.Vertical + total);
        }

        public override Rect GetItemRect(int index)
        {
            if (_variable) return new Rect(_left, _offsets[index], _width, _lengths[index]);
            return new Rect(_left, _top + index * _step, _width, _height);
        }

        public override VisualRange GetRange(Rect visibleArea)
        {
            if (Count == 0) return VisualRange.Empty;
            if (!_variable)
            {
                if (_step <= 0f) return VisualRange.Empty;
                int first = Mathf.FloorToInt((visibleArea.yMin - _top) / _step);
                int last = Mathf.FloorToInt((visibleArea.yMax - _top) / _step);
                return VisualRange.Clamp(first, last, Count);
            }
            int start = FirstEndingAfter(visibleArea.yMin);
            int end = LastStartingBefore(visibleArea.yMax);
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
