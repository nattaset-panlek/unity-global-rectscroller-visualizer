using UnityEngine;

namespace GlobalVisualizer
{
    internal sealed class VisualEmptyState
    {
        #region [Variables/Fields]
        private RectTransform _view;
        private RectTransform _source;
        #endregion

        #region [Properties]
        public bool IsShowing => _view != null && _view.gameObject.activeSelf;
        public RectTransform View => _view;
        #endregion

        #region [Public Methods]
        public void Update(bool show, RectTransform prefab, Transform parent, HideFlags flags)
        {
            if (_view != null && _source != prefab) Dispose();
            if (!show || prefab == null)
            {
                if (_view != null && _view.gameObject.activeSelf) _view.gameObject.SetActive(false);
                return;
            }
            if (_view == null)
            {
                _view = Object.Instantiate(prefab, parent, false);
                _view.name = prefab.name;
                _source = prefab;
                VisualPool.ApplyFlags(_view, flags);
            }
            if (!_view.gameObject.activeSelf) _view.gameObject.SetActive(true);
            _view.SetAsLastSibling();
        }

        public void Forget()
        {
            _view = null;
            _source = null;
        }

        public void Dispose()
        {
            if (_view != null) VisualPool.DestroyObject(_view.gameObject);
            _view = null;
            _source = null;
        }
        #endregion
    }
}
