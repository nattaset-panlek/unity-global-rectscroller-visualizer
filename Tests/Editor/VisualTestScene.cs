using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Tests
{
    internal sealed class VisualTestScene
    {
        #region [Variables/Fields]
        private readonly List<Object> _objects = new List<Object>();
        #endregion

        #region [Public Methods]
        public GlobalVisualizerView CreateView(Vector2 viewportSize, VisualLayoutSettings settings, RectTransform template = null)
        {
            var root = new GameObject("Visualizer", typeof(RectTransform));
            _objects.Add(root);
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = viewportSize;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(rootRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform));
            ((RectTransform)content.transform).SetParent(viewportRect, false);

            var scroll = root.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = (RectTransform)content.transform;

            var view = root.AddComponent<GlobalVisualizerView>();
            view.SetLayout(settings);
            view.ItemPrefab = template != null ? template : CreateTemplate<VisualTestItem>("Item", new Vector2(300f, 40f));
            return view;
        }

        public RectTransform CreateTemplate<T>(string name, Vector2 size) where T : Component
        {
            var template = new GameObject(name, typeof(RectTransform), typeof(T));
            _objects.Add(template);
            var rect = (RectTransform)template.transform;
            rect.sizeDelta = size;
            template.SetActive(false);
            return rect;
        }

        public RectTransform CreateEmptyTemplate()
        {
            var template = new GameObject("Empty", typeof(RectTransform));
            _objects.Add(template);
            template.SetActive(false);
            return (RectTransform)template.transform;
        }

        public void Dispose()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null) Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        public static List<int> Range(int count)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++) list.Add(i);
            return list;
        }
        #endregion
    }
}
