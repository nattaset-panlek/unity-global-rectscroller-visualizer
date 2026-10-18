using UnityEngine;
using UnityEngine.EventSystems;

namespace GlobalVisualizer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class VisualItemInput : MonoBehaviour, IPointerClickHandler, IMoveHandler, ISubmitHandler, ISelectHandler
    {
        #region [Variables/Fields]
        private GlobalVisualizerView _owner;
        private int _index = -1;
        #endregion

        #region [Properties]
        public int Index => _index;
        public GlobalVisualizerView Owner => _owner;
        #endregion

        #region [Public Methods]
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_owner != null && _index >= 0) _owner.HandlePointerClick(_index, eventData);
        }

        public void OnMove(AxisEventData eventData)
        {
            if (_owner != null && _index >= 0) _owner.HandleMove(_index, eventData);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (_owner != null && _index >= 0) _owner.HandleSubmit(_index);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_owner != null && _index >= 0) _owner.HandleSelect(_index);
        }

        internal void Attach(GlobalVisualizerView owner, int index)
        {
            _owner = owner;
            _index = index;
        }
        #endregion
    }
}
