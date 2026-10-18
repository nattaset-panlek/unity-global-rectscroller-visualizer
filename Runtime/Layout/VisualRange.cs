using System;
using UnityEngine;

namespace GlobalVisualizer
{
    public readonly struct VisualRange : IEquatable<VisualRange>
    {
        #region [Variables/Fields]
        public static readonly VisualRange Empty = new VisualRange(0, -1);
        #endregion

        #region [Properties]
        public int First { get; }
        public int Last { get; }
        public bool IsEmpty => Last < First;
        public int Length => IsEmpty ? 0 : Last - First + 1;
        #endregion

        #region [Public Methods]
        public VisualRange(int first, int last)
        {
            First = first;
            Last = last;
        }

        public static VisualRange Clamp(int first, int last, int count)
        {
            if (count <= 0) return Empty;
            first = Mathf.Max(0, first);
            last = Mathf.Min(count - 1, last);
            return last < first ? Empty : new VisualRange(first, last);
        }

        public bool Contains(int index)
        {
            return index >= First && index <= Last;
        }

        public bool Equals(VisualRange other)
        {
            return (IsEmpty && other.IsEmpty) || (First == other.First && Last == other.Last);
        }

        public override bool Equals(object obj)
        {
            return obj is VisualRange other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsEmpty ? -1 : (First * 397) ^ Last;
        }

        public override string ToString()
        {
            return IsEmpty ? "[empty]" : $"[{First}..{Last}]";
        }
        #endregion
    }
}
