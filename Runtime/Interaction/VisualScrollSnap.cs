using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalVisualizer
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class VisualScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IScrollHandler
    {
        #region [Variables/Fields]
        private const float StopVelocity = 60f;
        private const float Sharpness = 14f;
        private GlobalVisualizerView _owner;
        private ScrollRect _scroll;
        private bool _dragging;
        private bool _pending;
        #endregion

        #region [Unity Methods]
        private void LateUpdate()
        {
            if (!_pending || _dragging || _owner == null || _scroll == null)
            {
                enabled = false;
                return;
            }
            if (_scroll.velocity.sqrMagnitude > StopVelocity * StopVelocity) return;

            _scroll.velocity = Vector2.zero;
            Vector2 current = _owner.ScrollPosition;
            Vector2 target = _owner.GetSnapPosition();
            Vector2 next = Vector2.Lerp(current, target, 1f - Mathf.Exp(-Sharpness * Time.unscaledDeltaTime));
            if ((next - target).sqrMagnitude < 0.25f)
            {
                next = target;
                _pending = false;
                enabled = false;
            }
            _owner.ScrollPosition = next;
        }
        #endregion

        #region [Public Methods]
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            _pending = false;
            enabled = false;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            Schedule();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!_dragging) Schedule();
        }

        internal void Setup(GlobalVisualizerView owner, ScrollRect scroll)
        {
            _owner = owner;
            _scroll = scroll;
            enabled = false;
        }
        #endregion

        #region [Private Methods]
        private void Schedule()
        {
            if (_owner == null || !_owner.SnapToItems) return;
            _pending = true;
            enabled = true;
        }
        #endregion
    }
}
