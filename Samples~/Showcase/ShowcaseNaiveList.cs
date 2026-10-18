using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseNaiveList
    {
        #region [Variables/Fields]
        private readonly RectTransform _template;
        #endregion

        #region [Properties]
        public ScrollRect Scroll { get; }
        public RectTransform Content { get; }
        public int ItemCount => Content.childCount;
        public int ObjectCount => Content.GetComponentsInChildren<Transform>(true).Length - 1;
        #endregion

        #region [Public Methods]
        public ShowcaseNaiveList(ShowcaseFactory factory, Transform parent, RectTransform template, bool grid, Vector2 cell, float spacing)
        {
            _template = template;
            Scroll = factory.NaiveScroll(parent, grid ? "Naive Grid" : "Naive List", grid, cell, spacing, out RectTransform content);
            Content = content;
        }

        public void Build(IReadOnlyList<ShowcaseItem> items)
        {
            for (int i = 0; i < items.Count; i++) Add(items[i], i);
        }

        public void RebuildAll(IReadOnlyList<ShowcaseItem> items)
        {
            for (int i = Content.childCount - 1; i >= 0; i--) Object.Destroy(Content.GetChild(i).gameObject);
            Build(items);
        }

        public void InsertFirst(ShowcaseItem item)
        {
            RectTransform view = Add(item, 0);
            view.SetAsFirstSibling();
        }

        public void Clear()
        {
            for (int i = Content.childCount - 1; i >= 0; i--)
            {
                GameObject child = Content.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }
        #endregion

        #region [Private Methods]
        private RectTransform Add(ShowcaseItem item, int index)
        {
            RectTransform view = Object.Instantiate(_template, Content, false);
            if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
            view.GetComponent<ShowcaseItemView>().Bind(item, new VisualItemInfo(index, null));
            return view;
        }
        #endregion
    }
}
