using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlobalVisualizer.EditorTools
{
    public static class GlobalVisualizerMenuItems
    {
        #region [Variables/Fields]
        private const string Root = "GameObject/Global Visualizer/";
        private const int Priority = 10;
        #endregion

        #region [Public Methods]
        [MenuItem(Root + "Vertical List", false, Priority)]
        public static void CreateVertical(MenuCommand command)
        {
            Create(VisualLayoutKind.Vertical, command);
        }

        [MenuItem(Root + "Horizontal List", false, Priority + 1)]
        public static void CreateHorizontal(MenuCommand command)
        {
            Create(VisualLayoutKind.Horizontal, command);
        }

        [MenuItem(Root + "Grid", false, Priority + 2)]
        public static void CreateGrid(MenuCommand command)
        {
            Create(VisualLayoutKind.Grid, command);
        }

        [MenuItem(Root + "Flow (Tags)", false, Priority + 3)]
        public static void CreateFlow(MenuCommand command)
        {
            Create(VisualLayoutKind.Flow, command);
        }

        [MenuItem(Root + "Create Item Template...", false, Priority + 20)]
        public static void CreateItemTemplate(MenuCommand command)
        {
            GameObject context = command.context as GameObject;
            if (context == null) context = Selection.activeGameObject;
            GlobalVisualizerView view = context != null ? context.GetComponentInParent<GlobalVisualizerView>(true) : null;
            ItemTemplateWindow.Open(view, null);
        }

        [MenuItem("Assets/Create/Global Visualizer/Item Template...", false, 80)]
        public static void CreateItemTemplateInFolder()
        {
            ItemTemplateWindow.Open(null, SelectedFolder());
        }
        #endregion

        #region [Private Methods]
        private static void Create(VisualLayoutKind kind, MenuCommand command)
        {
            if (Selection.gameObjects.Length > 1 && command.context != null && command.context != Selection.gameObjects[0]) return;
            GameObject context = command.context as GameObject;
            Transform parent = GlobalVisualizerFactory.ResolveParent(context);
            GlobalVisualizerFactory.EnsureEventSystem();
            GlobalVisualizerView view = GlobalVisualizerFactory.Create(kind, parent);
            GameObjectUtility.EnsureUniqueNameForSibling(view.gameObject);
            Undo.RegisterCreatedObjectUndo(view.gameObject, "Create Global Visualizer");
            Selection.activeGameObject = view.gameObject;
        }

        private static string SelectedFolder()
        {
            foreach (Object selected in Selection.GetFiltered<Object>(SelectionMode.Assets))
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path)) return path;
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) return directory.Replace('\\', '/');
            }
            return "Assets";
        }
        #endregion
    }
}
