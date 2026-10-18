using UnityEngine;
using UnityEngine.EventSystems;

namespace GlobalVisualizer
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class VisualViewportWatcher : UIBehaviour
    {
        #region [Variables/Fields]
        private GlobalVisualizerView _owner;
        #endregion

        #region [Unity Methods]
        protected override void OnRectTransformDimensionsChange()
        {
            if (_owner != null) _owner.HandleResize();
        }
        #endregion

        #region [Public Methods]
        internal void Attach(GlobalVisualizerView owner)
        {
            _owner = owner;
        }
        #endregion
    }
}
