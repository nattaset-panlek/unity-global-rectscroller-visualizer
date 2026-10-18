using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class VisualPool
    {
        #region [Variables/Fields]
        private readonly RectTransform _prefab;
        private readonly Transform _parent;
        private readonly HideFlags _hideFlags;
        private readonly Stack<RectTransform> _free = new Stack<RectTransform>();
        private readonly List<RectTransform> _created = new List<RectTransform>();
        #endregion

        #region [Properties]
        public RectTransform Prefab => _prefab;
        public int CreatedCount => _created.Count;
        public int FreeCount => _free.Count;
        #endregion

        #region [Public Methods]
        public VisualPool(RectTransform prefab, Transform parent, HideFlags hideFlags = HideFlags.None)
        {
            _prefab = prefab;
            _parent = parent;
            _hideFlags = hideFlags;
        }

        public RectTransform Get()
        {
            return _free.Count > 0 ? _free.Pop() : Create();
        }

        public void Release(RectTransform view)
        {
            if (view == null) return;
            if (view.gameObject.activeSelf) view.gameObject.SetActive(false);
            _free.Push(view);
        }

        public void Warm(int count)
        {
            while (_created.Count < count) Release(Create());
        }

        public void Clear()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null) DestroyObject(_created[i].gameObject);
            }
            _created.Clear();
            _free.Clear();
        }
        #endregion

        #region [Private Methods]
        private RectTransform Create()
        {
            RectTransform view = Object.Instantiate(_prefab, _parent, false);
            view.name = _prefab.name;
            view.anchorMin = new Vector2(0f, 1f);
            view.anchorMax = new Vector2(0f, 1f);
            view.pivot = new Vector2(0f, 1f);
            ApplyFlags(view, _hideFlags);
            _created.Add(view);
            return view;
        }

        internal static void ApplyFlags(RectTransform view, HideFlags flags)
        {
            if (flags == HideFlags.None) return;
            foreach (Transform child in view.GetComponentsInChildren<Transform>(true)) child.gameObject.hideFlags = flags;
        }

        internal static void DestroyObject(Object target)
        {
            if (Application.isPlaying) Object.Destroy(target);
            else Object.DestroyImmediate(target);
        }
        #endregion
    }
}
