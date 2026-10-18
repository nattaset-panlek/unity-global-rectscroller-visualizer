using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class FlowVisualLayout : VisualLayout
    {
        #region [Variables/Fields]
        private Rect[] _rects = new Rect[0];
        private int[] _rowStart = new int[0];
        private int[] _itemRow = new int[0];
        private float[] _rowTop = new float[0];
        private float[] _rowBottom = new float[0];
        #endregion

        #region [Properties]
        public override VisualLayoutKind Kind => VisualLayoutKind.Flow;
        public override bool ScrollsVertically => true;
        public override bool SupportsVariableSize => true;
        public int RowCount { get; private set; }
        #endregion

        #region [Public Methods]
        public override void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count, VisualSizeTable sizes)
        {
            Count = Mathf.Max(0, count);
            VisualPadding padding = settings.Padding;
            Vector2 spacing = settings.Spacing;
            float available = Mathf.Max(1f, viewportSize.x - padding.Horizontal);
            var fallback = new Vector2(ResolveLength(settings.ItemSize.x, prefabSize.x), ResolveLength(settings.ItemSize.y, prefabSize.y));
            bool variable = sizes != null && sizes.Count >= Count;
            EnsureCapacity(Count);

            float left = padding.Left;
            float x = left;
            float y = padding.Top;
            float rowHeight = 0f;
            RowCount = 0;

            for (int i = 0; i < Count; i++)
            {
                Vector2 size = variable ? sizes.Get(i) : fallback;
                if (size.x <= 0f) size.x = fallback.x;
                if (size.y <= 0f) size.y = fallback.y;
                float width = Mathf.Min(size.x, available);

                if (i == 0 || (x > left && x + width > left + available))
                {
                    if (i > 0)
                    {
                        _rowBottom[RowCount - 1] = y + rowHeight;
                        y += rowHeight + spacing.y;
                    }
                    _rowStart[RowCount] = i;
                    _rowTop[RowCount] = y;
                    RowCount++;
                    x = left;
                    rowHeight = 0f;
                }

                _rects[i] = new Rect(x, y, width, size.y);
                _itemRow[i] = RowCount - 1;
                x += width + spacing.x;
                rowHeight = Mathf.Max(rowHeight, size.y);
            }

            float bottom = padding.Top;
            if (RowCount > 0)
            {
                _rowBottom[RowCount - 1] = y + rowHeight;
                bottom = y + rowHeight;
            }
            ContentSize = new Vector2(padding.Horizontal + available, bottom + padding.Bottom);
        }

        public override Rect GetItemRect(int index)
        {
            return _rects[index];
        }

        public override VisualRange GetRange(Rect visibleArea)
        {
            if (Count == 0 || RowCount == 0) return VisualRange.Empty;
            int firstRow = FirstRowEndingAfter(visibleArea.yMin);
            int lastRow = LastRowStartingBefore(visibleArea.yMax);
            if (lastRow < firstRow) return VisualRange.Empty;
            int first = _rowStart[firstRow];
            int last = (lastRow + 1 < RowCount ? _rowStart[lastRow + 1] : Count) - 1;
            return new VisualRange(first, last);
        }

        public override int GetNeighbor(int index, VisualDirection direction)
        {
            if (Count == 0 || index < 0 || index >= Count) return -1;
            if (direction == VisualDirection.Left) return index > 0 ? index - 1 : -1;
            if (direction == VisualDirection.Right) return index < Count - 1 ? index + 1 : -1;

            int row = _itemRow[index] + (direction == VisualDirection.Up ? -1 : 1);
            if (row < 0 || row >= RowCount) return -1;
            float center = _rects[index].center.x;
            int start = _rowStart[row];
            int end = (row + 1 < RowCount ? _rowStart[row + 1] : Count) - 1;
            int best = start;
            float bestDistance = float.MaxValue;
            for (int i = start; i <= end; i++)
            {
                float distance = Mathf.Abs(_rects[i].center.x - center);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = i;
            }
            return best;
        }
        #endregion

        #region [Private Methods]
        private void EnsureCapacity(int count)
        {
            if (_rects.Length < count)
            {
                int size = Mathf.Max(count, _rects.Length * 2);
                _rects = new Rect[size];
                _itemRow = new int[size];
            }
            if (_rowStart.Length < count + 1)
            {
                int size = Mathf.Max(count + 1, _rowStart.Length * 2);
                _rowStart = new int[size];
                _rowTop = new float[size];
                _rowBottom = new float[size];
            }
        }

        private int FirstRowEndingAfter(float position)
        {
            int low = 0;
            int high = RowCount - 1;
            int result = RowCount;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_rowBottom[mid] > position)
                {
                    result = mid;
                    high = mid - 1;
                }
                else low = mid + 1;
            }
            return result;
        }

        private int LastRowStartingBefore(float position)
        {
            int low = 0;
            int high = RowCount - 1;
            int result = -1;
            while (low <= high)
            {
                int mid = (low + high) >> 1;
                if (_rowTop[mid] <= position)
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
