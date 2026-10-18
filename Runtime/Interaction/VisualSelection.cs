using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer
{
    public sealed class VisualSelection
    {
        #region [Variables/Fields]
        private readonly HashSet<int> _selected = new HashSet<int>();
        private readonly List<int> _buffer = new List<int>();
        #endregion

        #region [Properties]
        public VisualSelectionMode Mode { get; set; } = VisualSelectionMode.Single;
        public int Count => _selected.Count;
        public int Anchor { get; private set; } = -1;

        public int First
        {
            get
            {
                int first = -1;
                foreach (int index in _selected)
                {
                    if (first < 0 || index < first) first = index;
                }
                return first;
            }
        }
        #endregion

        #region [Public Methods]
        public bool Contains(int index)
        {
            return _selected.Contains(index);
        }

        public bool Click(int index, bool additive, bool range)
        {
            if (Mode == VisualSelectionMode.None || index < 0) return false;

            if (Mode == VisualSelectionMode.Single || (!additive && !range))
            {
                Anchor = index;
                if (_selected.Count == 1 && _selected.Contains(index)) return false;
                _selected.Clear();
                _selected.Add(index);
                return true;
            }

            if (range && Anchor >= 0)
            {
                if (!additive) _selected.Clear();
                int from = Mathf.Min(Anchor, index);
                int to = Mathf.Max(Anchor, index);
                for (int i = from; i <= to; i++) _selected.Add(i);
                return true;
            }

            Anchor = index;
            if (!_selected.Remove(index)) _selected.Add(index);
            return true;
        }

        public bool Set(int index, bool selected)
        {
            if (Mode == VisualSelectionMode.None || index < 0) return false;
            if (!selected) return _selected.Remove(index);
            if (_selected.Contains(index)) return false;
            if (Mode == VisualSelectionMode.Single) _selected.Clear();
            _selected.Add(index);
            Anchor = index;
            return true;
        }

        public bool Clear()
        {
            Anchor = -1;
            if (_selected.Count == 0) return false;
            _selected.Clear();
            return true;
        }

        public void CopyTo(List<int> results)
        {
            foreach (int index in _selected) results.Add(index);
        }

        public bool OnInserted(int index, int count)
        {
            if (count <= 0 || _selected.Count == 0) return false;
            bool changed = false;
            _buffer.Clear();
            foreach (int selected in _selected)
            {
                if (selected >= index)
                {
                    changed = true;
                    _buffer.Add(selected + count);
                }
                else _buffer.Add(selected);
            }
            Replace();
            if (Anchor >= index) Anchor += count;
            return changed;
        }

        public bool OnRemoved(int index, int count)
        {
            if (count <= 0 || _selected.Count == 0) return false;
            bool changed = false;
            _buffer.Clear();
            foreach (int selected in _selected)
            {
                if (selected < index) _buffer.Add(selected);
                else if (selected >= index + count)
                {
                    changed = true;
                    _buffer.Add(selected - count);
                }
                else changed = true;
            }
            Replace();
            if (Anchor >= index + count) Anchor -= count;
            else if (Anchor >= index) Anchor = -1;
            return changed;
        }

        public bool Clamp(int count)
        {
            if (_selected.Count == 0) return false;
            int before = _selected.Count;
            _selected.RemoveWhere(index => index >= count);
            if (Anchor >= count) Anchor = -1;
            return before != _selected.Count;
        }
        #endregion

        #region [Private Methods]
        private void Replace()
        {
            _selected.Clear();
            for (int i = 0; i < _buffer.Count; i++) _selected.Add(_buffer[i]);
            _buffer.Clear();
        }
        #endregion
    }
}
