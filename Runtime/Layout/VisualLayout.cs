using UnityEngine;

namespace GlobalVisualizer
{
    public abstract class VisualLayout
    {
        #region [Variables/Fields]
        public const float FallbackItemLength = 100f;
        #endregion

        #region [Properties]
        public int Count { get; protected set; }
        public Vector2 ContentSize { get; protected set; }
        public abstract VisualLayoutKind Kind { get; }
        public abstract bool ScrollsVertically { get; }
        public virtual bool SupportsVariableSize => false;
        #endregion

        #region [Public Methods]
        public static VisualLayout Create(VisualLayoutKind kind)
        {
            switch (kind)
            {
                case VisualLayoutKind.Horizontal: return new HorizontalVisualLayout();
                case VisualLayoutKind.Grid: return new GridVisualLayout();
                case VisualLayoutKind.Flow: return new FlowVisualLayout();
                default: return new VerticalVisualLayout();
            }
        }

        public void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count)
        {
            Rebuild(settings, viewportSize, prefabSize, count, null);
        }

        public abstract void Rebuild(VisualLayoutSettings settings, Vector2 viewportSize, Vector2 prefabSize, int count, VisualSizeTable sizes);

        public abstract Rect GetItemRect(int index);

        public abstract VisualRange GetRange(Rect visibleArea);

        public Vector2 GetScrollPosition(int index, Vector2 viewportSize, float alignment)
        {
            if (Count == 0) return Vector2.zero;
            Rect rect = GetItemRect(Mathf.Clamp(index, 0, Count - 1));
            alignment = Mathf.Clamp01(alignment);
            if (ScrollsVertically)
            {
                float y = rect.y - alignment * (viewportSize.y - rect.height);
                return new Vector2(0f, Mathf.Clamp(y, 0f, Mathf.Max(0f, ContentSize.y - viewportSize.y)));
            }
            float x = rect.x - alignment * (viewportSize.x - rect.width);
            return new Vector2(Mathf.Clamp(x, 0f, Mathf.Max(0f, ContentSize.x - viewportSize.x)), 0f);
        }

        public virtual int GetNeighbor(int index, VisualDirection direction)
        {
            if (Count == 0) return -1;
            int step;
            if (ScrollsVertically) step = direction == VisualDirection.Up ? -1 : direction == VisualDirection.Down ? 1 : 0;
            else step = direction == VisualDirection.Left ? -1 : direction == VisualDirection.Right ? 1 : 0;
            if (step == 0) return -1;
            int next = index + step;
            return next >= 0 && next < Count ? next : -1;
        }

        public Vector2 ClampScrollPosition(Vector2 position, Vector2 viewportSize)
        {
            float x = Mathf.Clamp(position.x, 0f, Mathf.Max(0f, ContentSize.x - viewportSize.x));
            float y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, ContentSize.y - viewportSize.y));
            return new Vector2(x, y);
        }
        #endregion

        #region [Private Methods]
        protected static float ResolveLength(float configured, float prefab)
        {
            if (configured > 0f) return configured;
            return prefab > 0f ? prefab : FallbackItemLength;
        }

        protected static float Span(int count, float length, float gap)
        {
            return count > 0 ? count * length + (count - 1) * gap : 0f;
        }

        protected static float AlignOffset(VisualAlignment alignment, float available, float length)
        {
            float free = Mathf.Max(0f, available - length);
            switch (alignment)
            {
                case VisualAlignment.Center: return free * 0.5f;
                case VisualAlignment.End: return free;
                default: return 0f;
            }
        }

        protected static float[] EnsureCapacity(float[] buffer, int count)
        {
            if (buffer != null && buffer.Length >= count) return buffer;
            return new float[Mathf.Max(count, buffer != null ? buffer.Length * 2 : 16)];
        }
        #endregion
    }
}
