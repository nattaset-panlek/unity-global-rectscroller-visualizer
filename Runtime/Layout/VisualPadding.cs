using System;
using UnityEngine;

namespace GlobalVisualizer
{
    [Serializable]
    public struct VisualPadding
    {
        #region [Variables/Fields]
        [SerializeField] private float _left;
        [SerializeField] private float _right;
        [SerializeField] private float _top;
        [SerializeField] private float _bottom;
        #endregion

        #region [Properties]
        public float Left => _left;
        public float Right => _right;
        public float Top => _top;
        public float Bottom => _bottom;
        public float Horizontal => _left + _right;
        public float Vertical => _top + _bottom;
        #endregion

        #region [Public Methods]
        public VisualPadding(float left, float right, float top, float bottom)
        {
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        public VisualPadding(float all) : this(all, all, all, all)
        {
        }
        #endregion
    }
}
