using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class VisualItemDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region [Variables/Fields]
        private GlobalVisualizerView _owner;
        private int _index = -1;
        private bool _draggingItem;
        private bool _forwarding;
        #endregion

        #region [Properties]
        public bool IsDraggingItem => _draggingItem;
        #endregion

        #region [Public Methods]
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            Forward(eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _draggingItem = _owner != null && _index >= 0 && _owner.ShouldStartItemDrag(_index, eventData);
            if (_draggingItem)
            {
                _owner.HandleItemBeginDrag(_index, eventData);
                return;
            }
            _forwarding = true;
            Forward(eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_draggingItem)
            {
                if (_owner != null) _owner.HandleItemDrag(_index, eventData);
                return;
            }
            if (_forwarding) Forward(eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggingItem)
            {
                _draggingItem = false;
                if (_owner != null) _owner.HandleItemEndDrag(_index, eventData);
                return;
            }
            if (!_forwarding) return;
            _forwarding = false;
            Forward(eventData, ExecuteEvents.endDragHandler);
        }

        internal void Attach(GlobalVisualizerView owner, int index)
        {
            _owner = owner;
            _index = index;
        }
        #endregion

        #region [Private Methods]
        private void Forward<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function) where T : IEventSystemHandler
        {
            if (_owner == null) return;
            ScrollRect scroll = _owner.ScrollRect;
            if (scroll != null) ExecuteEvents.Execute(scroll.gameObject, eventData, function);
        }
        #endregion
    }
}
