using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class VisualSizeTable
    {
        #region [Variables/Fields]
        private readonly List<Vector2> _sizes = new List<Vector2>();
        private readonly List<bool> _known = new List<bool>();
        private Vector2 _estimate = new Vector2(VisualLayout.FallbackItemLength, VisualLayout.FallbackItemLength);
        private Vector2 _knownSum;
        #endregion

        #region [Properties]
        public int Count => _sizes.Count;
        public int KnownCount { get; private set; }
        public Vector2 KnownAverage => KnownCount > 0 ? _knownSum / KnownCount : _estimate;

        public Vector2 Estimate
        {
            get => _estimate;
            set => _estimate = value;
        }
        #endregion

        #region [Public Methods]
        public Vector2 Get(int index)
        {
            return _known[index] ? _sizes[index] : _estimate;
        }

        public bool IsKnown(int index)
        {
            return index >= 0 && index < _known.Count && _known[index];
        }

        public bool Set(int index, Vector2 size)
        {
            if (index < 0 || index >= _sizes.Count) return false;
            bool changed;
            if (_known[index])
            {
                changed = _sizes[index] != size;
                _knownSum -= _sizes[index];
            }
            else
            {
                changed = size != _estimate;
                _known[index] = true;
                KnownCount++;
            }
            _sizes[index] = size;
            _knownSum += size;
            return changed;
        }

        public void Resize(int count)
        {
            if (count < 0) count = 0;
            if (_sizes.Count > count) RemoveRange(count, _sizes.Count - count);
            while (_sizes.Count < count)
            {
                _sizes.Add(_estimate);
                _known.Add(false);
            }
        }

        public void Insert(int index, int count)
        {
            index = Mathf.Clamp(index, 0, _sizes.Count);
            for (int i = 0; i < count; i++)
            {
                _sizes.Insert(index, _estimate);
                _known.Insert(index, false);
            }
        }

        public void Remove(int index, int count)
        {
            if (index < 0 || index >= _sizes.Count || count <= 0) return;
            RemoveRange(index, Mathf.Min(count, _sizes.Count - index));
        }

        public void Invalidate(int index)
        {
            if (!IsKnown(index)) return;
            _known[index] = false;
            _knownSum -= _sizes[index];
            KnownCount--;
        }

        public void Clear()
        {
            _sizes.Clear();
            _known.Clear();
            _knownSum = Vector2.zero;
            KnownCount = 0;
        }
        #endregion

        #region [Private Methods]
        private void RemoveRange(int index, int count)
        {
            for (int i = index; i < index + count; i++)
            {
                if (!_known[i]) continue;
                _knownSum -= _sizes[i];
                KnownCount--;
            }
            _sizes.RemoveRange(index, count);
            _known.RemoveRange(index, count);
        }
        #endregion
    }
}
