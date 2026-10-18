using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.Samples.Showcase
{
    public sealed class ShowcaseFpsMeter : MonoBehaviour
    {
        #region [Variables/Fields]
        private const float Interval = 0.5f;
        private Text _label;
        private float _elapsed;
        private int _frames;
        private float _worst;
        #endregion

        #region [Unity Methods]
        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            _elapsed += delta;
            _frames++;
            if (delta > _worst) _worst = delta;
            if (_elapsed < Interval || _label == null) return;
            float fps = _frames / _elapsed;
            _label.text = $"{fps:0} FPS  ·  {1000f * _elapsed / _frames:0.0} ms avg  ·  {_worst * 1000f:0.0} ms worst";
            _label.color = fps >= 55f ? Color.white : fps >= 30f ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.55f, 0.5f);
            _elapsed = 0f;
            _frames = 0;
            _worst = 0f;
        }
        #endregion

        #region [Public Methods]
        public void Setup(Text label)
        {
            _label = label;
        }
        #endregion
    }
}
