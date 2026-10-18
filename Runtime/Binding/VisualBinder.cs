using UnityEngine;

namespace GlobalVisualizer
{
    internal abstract class VisualBinder
    {
        #region [Properties]
        public abstract int Count { get; }
        #endregion

        #region [Public Methods]
        public abstract void Bind(RectTransform view, VisualItemInfo info);

        public abstract void Unbind(RectTransform view);

        public abstract void UpdateState(RectTransform view, VisualItemInfo info);

        public abstract void ClearCache();
        #endregion
    }
}
