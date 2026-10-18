using UnityEngine;

namespace GlobalVisualizer.Tests
{
    public sealed class VisualTestItem : MonoBehaviour, IVisualItem<int>
    {
        #region [Variables/Fields]
        public int BoundValue = -1;
        public int BindCount;
        public int UnbindCount;
        public VisualItemInfo LastInfo;
        #endregion

        #region [Public Methods]
        public void Bind(int data, VisualItemInfo info)
        {
            BoundValue = data;
            LastInfo = info;
            BindCount++;
        }

        public void Unbind()
        {
            BoundValue = -1;
            UnbindCount++;
        }
        #endregion
    }
}
