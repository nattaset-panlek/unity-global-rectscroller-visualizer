using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer
{
    internal sealed class ListVisualBinder<T> : VisualBinder
    {
        #region [Variables/Fields]
        private readonly IReadOnlyList<T> _items;
        private readonly Action<GameObject, T, int> _onBind;
        private readonly Dictionary<RectTransform, IVisualItem<T>[]> _receivers = new Dictionary<RectTransform, IVisualItem<T>[]>();
        #endregion

        #region [Properties]
        public override int Count => _items != null ? _items.Count : 0;
        #endregion

        #region [Public Methods]
        public ListVisualBinder(IReadOnlyList<T> items, Action<GameObject, T, int> onBind)
        {
            _items = items;
            _onBind = onBind;
        }

        public override void Bind(RectTransform view, VisualItemInfo info)
        {
            T data = _items[info.Index];
            IVisualItem<T>[] receivers = GetReceivers(view);
            for (int i = 0; i < receivers.Length; i++) receivers[i].Bind(data, info);
            _onBind?.Invoke(view.gameObject, data, info.Index);
        }

        public override void Unbind(RectTransform view)
        {
            if (!_receivers.TryGetValue(view, out IVisualItem<T>[] receivers)) return;
            for (int i = 0; i < receivers.Length; i++) receivers[i].Unbind();
        }

        public override void UpdateState(RectTransform view, VisualItemInfo info)
        {
            T data = _items[info.Index];
            IVisualItem<T>[] receivers = GetReceivers(view);
            for (int i = 0; i < receivers.Length; i++)
            {
                if (receivers[i] is IVisualItemState) continue;
                receivers[i].Unbind();
                receivers[i].Bind(data, info);
            }
            _onBind?.Invoke(view.gameObject, data, info.Index);
        }

        public override void ClearCache()
        {
            _receivers.Clear();
        }
        #endregion

        #region [Private Methods]
        private IVisualItem<T>[] GetReceivers(RectTransform view)
        {
            if (!_receivers.TryGetValue(view, out IVisualItem<T>[] receivers))
            {
                receivers = view.GetComponentsInChildren<IVisualItem<T>>(true);
                _receivers.Add(view, receivers);
            }
            return receivers;
        }
        #endregion
    }
}
