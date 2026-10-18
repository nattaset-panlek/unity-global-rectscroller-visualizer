using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GlobalVisualizer.EditorTools
{
    [InitializeOnLoad]
    public static class VisualizerPreviewRegistry
    {
        #region [Variables/Fields]
        private static readonly List<GlobalVisualizerView> Previews = new List<GlobalVisualizerView>();
        #endregion

        #region [Properties]
        public static int ActiveCount => Previews.Count;
        #endregion

        #region [Unity Methods]
        static VisualizerPreviewRegistry()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += ClearAll;
            EditorSceneManager.sceneClosing += OnSceneClosing;
        }
        #endregion

        #region [Public Methods]
        public static void Begin(GlobalVisualizerView view, int count)
        {
            if (view == null || Application.isPlaying) return;
            if (view.IsEditorPreview) view.EndEditorPreview();
            DestroyLeftovers(view);
            view.BeginEditorPreview(Mathf.Max(1, count));
            if (!Previews.Contains(view)) Previews.Add(view);
            SceneView.RepaintAll();
        }

        public static void End(GlobalVisualizerView view)
        {
            if (view == null) return;
            view.EndEditorPreview();
            DestroyLeftovers(view);
            Previews.Remove(view);
            SceneView.RepaintAll();
        }

        public static void ClearAll()
        {
            for (int i = Previews.Count - 1; i >= 0; i--)
            {
                if (Previews[i] != null) End(Previews[i]);
            }
            Previews.Clear();
        }
        #endregion

        #region [Private Methods]
        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) ClearAll();
        }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            ClearAll();
        }

        private static void DestroyLeftovers(GlobalVisualizerView view)
        {
            ScrollRect scroll = view.GetComponent<ScrollRect>();
            if (scroll == null) return;
            DestroyUnsavedChildren(scroll.content);
            DestroyUnsavedChildren(scroll.viewport);
        }

        private static void DestroyUnsavedChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                if ((child.hideFlags & HideFlags.DontSaveInEditor) != 0) Object.DestroyImmediate(child);
            }
        }
        #endregion
    }
}
