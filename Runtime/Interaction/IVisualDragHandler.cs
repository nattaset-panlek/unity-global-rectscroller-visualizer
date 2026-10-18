using UnityEngine.EventSystems;

namespace GlobalVisualizer
{
    public interface IVisualDragHandler
    {
        bool CanBeginDrag(VisualItemInfo item);

        void OnBeginDrag(VisualItemInfo item, PointerEventData eventData);

        void OnDrag(VisualItemInfo item, PointerEventData eventData);

        void OnEndDrag(VisualItemInfo item, PointerEventData eventData);
    }
}
