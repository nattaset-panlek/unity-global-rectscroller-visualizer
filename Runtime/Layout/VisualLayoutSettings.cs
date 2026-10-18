using System;
using UnityEngine;

namespace GlobalVisualizer
{
    [Serializable]
    public sealed class VisualLayoutSettings
    {
        #region [Variables/Fields]
        [SerializeField] private VisualLayoutKind _kind = VisualLayoutKind.Vertical;

        [Tooltip("Item size in pixels. 0 on an axis uses the prefab size, except the cross axis of Vertical/Horizontal, where 0 stretches the item to the viewport.")]
        [SerializeField] private Vector2 _itemSize = Vector2.zero;

        [SerializeField] private Vector2 _spacing = new Vector2(8f, 8f);
        [SerializeField] private VisualPadding _padding;

        [Tooltip("Grid only. Fit = as many columns as fit the viewport width. FixedRows scrolls horizontally.")]
        [SerializeField] private VisualGridConstraint _gridConstraint = VisualGridConstraint.Fit;

        [SerializeField, Min(1)] private int _constraintCount = 1;

        [Tooltip("Vertical/Horizontal only. Where items that are narrower than the viewport sit on the cross axis.")]
        [SerializeField] private VisualAlignment _crossAlignment = VisualAlignment.Start;
        #endregion

        #region [Properties]
        public VisualLayoutKind Kind { get => _kind; set => _kind = value; }
        public Vector2 ItemSize { get => _itemSize; set => _itemSize = value; }
        public Vector2 Spacing { get => _spacing; set => _spacing = value; }
        public VisualPadding Padding { get => _padding; set => _padding = value; }
        public VisualGridConstraint GridConstraint { get => _gridConstraint; set => _gridConstraint = value; }
        public int ConstraintCount { get => _constraintCount; set => _constraintCount = Mathf.Max(1, value); }
        public VisualAlignment CrossAlignment { get => _crossAlignment; set => _crossAlignment = value; }
        #endregion

        #region [Public Methods]
        public static VisualLayoutSettings Vertical(float itemHeight, float spacing = 8f)
        {
            return new VisualLayoutSettings { Kind = VisualLayoutKind.Vertical, ItemSize = new Vector2(0f, itemHeight), Spacing = new Vector2(spacing, spacing) };
        }

        public static VisualLayoutSettings Horizontal(float itemWidth, float spacing = 8f)
        {
            return new VisualLayoutSettings { Kind = VisualLayoutKind.Horizontal, ItemSize = new Vector2(itemWidth, 0f), Spacing = new Vector2(spacing, spacing) };
        }

        public static VisualLayoutSettings Flow(float spacing = 8f, Vector2 fallbackItemSize = default)
        {
            return new VisualLayoutSettings { Kind = VisualLayoutKind.Flow, ItemSize = fallbackItemSize, Spacing = new Vector2(spacing, spacing) };
        }

        public static VisualLayoutSettings Grid(Vector2 cellSize, float spacing = 8f, VisualGridConstraint constraint = VisualGridConstraint.Fit, int constraintCount = 1)
        {
            return new VisualLayoutSettings { Kind = VisualLayoutKind.Grid, ItemSize = cellSize, Spacing = new Vector2(spacing, spacing), GridConstraint = constraint, ConstraintCount = constraintCount };
        }
        #endregion
    }
}
