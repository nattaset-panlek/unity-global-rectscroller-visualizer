using System.Collections;
using System.Collections.Generic;

namespace GlobalVisualizer
{
    internal sealed class VisualIndexList : IReadOnlyList<int>
    {
        #region [Properties]
        public int Count { get; }
        public int this[int index] => index;
        #endregion

        #region [Public Methods]
        public VisualIndexList(int count)
        {
            Count = count < 0 ? 0 : count;
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < Count; i++) yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
