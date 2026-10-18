using UnityEngine.EventSystems;

namespace GlobalVisualizer.Tests
{
    public sealed class VisualTestDragHandler : IVisualDragHandler
    {
        #region [Variables/Fields]
        public bool Allow = true;
        public int BeginIndex = -1;
        public int DragCount;
        public int EndIndex = -1;
        #endregion

        #region [Public Methods]
        public bool CanBeginDrag(VisualItemInfo item)
        {
            return Allow;
        }

        public void OnBeginDrag(VisualItemInfo item, PointerEventData eventData)
        {
            BeginIndex = item.Index;
        }

        public void OnDrag(VisualItemInfo item, PointerEventData eventData)
        {
            DragCount++;
        }

        public void OnEndDrag(VisualItemInfo item, PointerEventData eventData)
        {
            EndIndex = item.Index;
        }
        #endregion
    }
}
