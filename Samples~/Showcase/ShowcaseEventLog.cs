using System.Collections.Generic;
using UnityEngine;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseEventLog : MonoBehaviour
    {
        #region [Variables/Fields]
        private const int MaxLines = 200;
        private readonly List<string> _lines = new List<string>();
        private GlobalVisualizerView _view;
        private int _counter;
        #endregion

        #region [Properties]
        public int Count => _lines.Count;
        #endregion

        #region [Public Methods]
        public static ShowcaseEventLog Create(ShowcaseFactory factory, ShowcaseTemplates templates, Transform parent, float height)
        {
            GlobalVisualizerView view = factory.Visualizer(parent, "Event Log", VisualLayoutSettings.Vertical(20f, 0f));
            ShowcaseFactory.Layout(view.gameObject, minHeight: height, preferredHeight: height, flexibleHeight: 0f);
            view.HandleInput = false;
            view.ItemPrefab = templates.LogLine;
            ShowcaseEventLog log = view.gameObject.AddComponent<ShowcaseEventLog>();
            log._view = view;
            view.SetItems(log._lines);
            return log;
        }

        public void Add(string message)
        {
            _counter++;
            _lines.Insert(0, _counter.ToString("000") + "  " + message);
            _view.NotifyInserted(0, 1);
            if (_lines.Count <= MaxLines) return;
            int extra = _lines.Count - MaxLines;
            _lines.RemoveRange(MaxLines, extra);
            _view.NotifyRemoved(MaxLines, extra);
        }

        public void Clear()
        {
            _lines.Clear();
            _view.NotifyReset();
        }
        #endregion
    }
}
