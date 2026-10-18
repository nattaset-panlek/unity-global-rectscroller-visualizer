using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Tests
{
    [ExecuteAlways]
    public sealed class VisualMeasuredItem : MonoBehaviour, IVisualItem<int>, ILayoutElement
    {
        #region [Variables/Fields]
        public const float Unit = 30f;
        private float _preferredHeight = Unit;
        private float _preferredWidth = 60f;
        #endregion

        #region [Properties]
        public float minWidth => 0f;
        public float preferredWidth => _preferredWidth;
        public float flexibleWidth => 0f;
        public float minHeight => 0f;
        public float preferredHeight => _preferredHeight;
        public float flexibleHeight => 0f;
        public int layoutPriority => 1;
        #endregion

        #region [Public Methods]
        public static float HeightFor(int data)
        {
            return Unit * (1 + data % 3);
        }

        public static float WidthFor(int data)
        {
            return 60f + 20f * (data % 4);
        }

        public void Bind(int data, VisualItemInfo info)
        {
            _preferredHeight = HeightFor(data);
            _preferredWidth = WidthFor(data);
        }

        public void Unbind()
        {
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }
        #endregion
    }
}
