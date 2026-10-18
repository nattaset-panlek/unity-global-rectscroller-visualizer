using UnityEngine;

namespace GlobalVisualizer.Samples.Showcase
{
    public abstract class ShowcasePage : MonoBehaviour
    {
        #region [Properties]
        public abstract string Title { get; }
        protected ShowcaseApp App { get; private set; }
        protected ShowcaseFactory Factory => App.Factory;
        protected ShowcaseTemplates Templates => App.Templates;
        protected RectTransform Root { get; private set; }
        #endregion

        #region [Public Methods]
        public void Initialize(ShowcaseApp app)
        {
            App = app;
            Root = (RectTransform)transform;
            Build(Root);
        }

        public virtual void OnShown()
        {
        }

        public virtual void OnHidden()
        {
        }
        #endregion

        #region [Private Methods]
        protected abstract void Build(RectTransform root);
        #endregion
    }
}
