using UnityEngine;

namespace GlobalVisualizer.Tests
{
    public sealed class VisualStateProbe : MonoBehaviour, IVisualItemState
    {
        #region [Variables/Fields]
        public int Index = -1;
        public bool Selected;
        public bool Focused;
        public int Calls;
        #endregion

        #region [Public Methods]
        public void OnStateChanged(VisualItemInfo info)
        {
            Index = info.Index;
            Selected = info.IsSelected;
            Focused = info.IsFocused;
            Calls++;
        }
        #endregion
    }
}
