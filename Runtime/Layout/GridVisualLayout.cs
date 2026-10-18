using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class GridVisualLayout : VisualLayout
    {
        #region [Variables/Fields]
        private Vector2 _cell;
        private Vector2 _spacing;
        private VisualPadding _padding;
        private bool _horizontal;
        #endregion

        #region [Properties]
        public override VisualLayoutKind Kind => VisualLayoutKind.Grid;
        public override bool ScrollsVertically => !_horizontal;
        public int Columns { get; private set; }
        public int Rows { get; private set; }
        public Vector2 CellSize => _cell;
        #endregion

        #region [Public Methods]
        public override void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count, VisualSizeTable sizes)
        {
            Count = Mathf.Max(0, count);
            _padding = settings.Padding;
            _spacing = settings.Spacing;
            _cell = new Vector2(ResolveLength(settings.ItemSize.x, prefabSize.x), ResolveLength(settings.ItemSize.y, prefabSize.y));
            _horizontal = settings.GridConstraint == VisualGridConstraint.FixedRows;

            if (_horizontal)
            {
                Rows = Mathf.Max(1, settings.ConstraintCount);
                Columns = Count == 0 ? 0 : Mathf.CeilToInt(Count / (float)Rows);
            }
            else
            {
                Columns = settings.GridConstraint == VisualGridConstraint.FixedColumns
                    ? Mathf.Max(1, settings.ConstraintCount)
                    : Mathf.Max(1, Mathf.FloorToInt((viewportSize.x - _padding.Horizontal + _spacing.x) / (_cell.x + _spacing.x)));
                Rows = Count == 0 ? 0 : Mathf.CeilToInt(Count / (float)Columns);
            }

            ContentSize = new Vector2(_padding.Horizontal + Span(Columns, _cell.x, _spacing.x), _padding.Vertical + Span(Rows, _cell.y, _spacing.y));
        }

        public override Rect GetItemRect(int index)
        {
            int column = _horizontal ? index / Rows : index % Columns;
            int row = _horizontal ? index % Rows : index / Columns;
            return new Rect(_padding.Left + column * (_cell.x + _spacing.x), _padding.Top + row * (_cell.y + _spacing.y), _cell.x, _cell.y);
        }

        public override VisualRange GetRange(Rect visibleArea)
        {
            if (Count == 0) return VisualRange.Empty;
            if (_horizontal)
            {
                float stepX = _cell.x + _spacing.x;
                int firstColumn = Mathf.Max(0, Mathf.FloorToInt((visibleArea.xMin - _padding.Left) / stepX));
                int lastColumn = Mathf.Min(Columns - 1, Mathf.FloorToInt((visibleArea.xMax - _padding.Left) / stepX));
                if (lastColumn < firstColumn) return VisualRange.Empty;
                return VisualRange.Clamp(firstColumn * Rows, (lastColumn + 1) * Rows - 1, Count);
            }

            float stepY = _cell.y + _spacing.y;
            int firstRow = Mathf.Max(0, Mathf.FloorToInt((visibleArea.yMin - _padding.Top) / stepY));
            int lastRow = Mathf.Min(Rows - 1, Mathf.FloorToInt((visibleArea.yMax - _padding.Top) / stepY));
            if (lastRow < firstRow) return VisualRange.Empty;
            return VisualRange.Clamp(firstRow * Columns, (lastRow + 1) * Columns - 1, Count);
        }

        public override int GetNeighbor(int index, VisualDirection direction)
        {
            if (Count == 0 || index < 0 || index >= Count) return -1;
            int column = _horizontal ? index / Rows : index % Columns;
            int row = _horizontal ? index % Rows : index / Columns;
            switch (direction)
            {
                case VisualDirection.Left: column--; break;
                case VisualDirection.Right: column++; break;
                case VisualDirection.Up: row--; break;
                default: row++; break;
            }
            if (column < 0 || row < 0 || column >= Columns || row >= Rows) return -1;
            int next = _horizontal ? column * Rows + row : row * Columns + column;
            return next < Count ? next : -1;
        }
        #endregion
    }
}
