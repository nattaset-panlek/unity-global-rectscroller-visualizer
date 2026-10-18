using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer
{
    internal static class VisualMeasurer
    {
        #region [Public Methods]
        public static bool CanMeasure(RectTransform prefab)
        {
            if (prefab == null) return false;
            foreach (ILayoutElement element in prefab.GetComponents<ILayoutElement>())
            {
                if (!(element is Image) && !(element is RawImage)) return true;
            }
            return false;
        }

        public static Vector2 Measure(RectTransform view, VisualLayoutKind kind, float availableWidth)
        {
            Vector2 size = view.rect.size;
            if (kind == VisualLayoutKind.Horizontal || kind == VisualLayoutKind.Flow)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(view);
                float width = LayoutUtility.GetPreferredWidth(view);
                if (width > 0f) size.x = kind == VisualLayoutKind.Flow ? Mathf.Min(width, availableWidth) : width;
                view.sizeDelta = new Vector2(size.x, view.sizeDelta.y);
            }
            if (kind == VisualLayoutKind.Vertical || kind == VisualLayoutKind.Flow)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(view);
                float height = LayoutUtility.GetPreferredHeight(view);
                if (height > 0f) size.y = height;
            }
            return size;
        }
        #endregion
    }
}
