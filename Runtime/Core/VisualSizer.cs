using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer
{
    internal sealed class VisualSizer
    {
        #region [Variables/Fields]
        private readonly VisualSizeTable _table = new VisualSizeTable();
        private RectTransform _measurePrefab;
        private bool _canMeasure;
        private float _measureWidth = -1f;
        #endregion

        #region [Properties]
        public VisualSizeTable Table => _table;
        public Func<int, Vector2> Provider { get; set; }
        #endregion

        #region [Public Methods]
        public bool CanMeasure(RectTransform prefab)
        {
            if (prefab != _measurePrefab)
            {
                _measurePrefab = prefab;
                _canMeasure = VisualMeasurer.CanMeasure(prefab);
            }
            return _canMeasure;
        }

        public void Prepare(VisualSizeMode mode, VisualLayoutSettings settings, Vector2 prefabSize, int count, float viewportWidth)
        {
            if (mode == VisualSizeMode.Measure && !Mathf.Approximately(viewportWidth, _measureWidth))
            {
                if (_measureWidth >= 0f) _table.Clear();
                _measureWidth = viewportWidth;
            }

            Vector2 estimate = new Vector2(Resolve(settings.ItemSize.x, prefabSize.x), Resolve(settings.ItemSize.y, prefabSize.y));
            _table.Estimate = mode == VisualSizeMode.Measure && _table.KnownCount > 0 ? _table.KnownAverage : estimate;
            _table.Resize(count);

            if (mode != VisualSizeMode.Provider || Provider == null) return;
            for (int i = 0; i < count; i++)
            {
                if (!_table.IsKnown(i)) _table.Set(i, Provider(i));
            }
        }

        public bool MeasureActive(Dictionary<int, RectTransform> active, VisualLayoutKind kind, float availableWidth)
        {
            bool changed = false;
            foreach (KeyValuePair<int, RectTransform> pair in active)
            {
                if (_table.IsKnown(pair.Key)) continue;
                Vector2 size = VisualMeasurer.Measure(pair.Value, kind, availableWidth);
                if (_table.Set(pair.Key, size)) changed = true;
            }
            return changed;
        }

        public void Reset()
        {
            _table.Clear();
            _measureWidth = -1f;
        }
        #endregion

        #region [Private Methods]
        private static float Resolve(float configured, float prefab)
        {
            if (configured > 0f) return configured;
            return prefab > 0f ? prefab : VisualLayout.FallbackItemLength;
        }
        #endregion
    }
}
