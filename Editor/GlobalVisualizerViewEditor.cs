using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GlobalVisualizer.EditorTools
{
    [CustomEditor(typeof(GlobalVisualizerView))]
    public sealed class GlobalVisualizerViewEditor : UnityEditor.Editor
    {
        #region [Variables/Fields]
        private const string PreviewCountKey = "GlobalVisualizer.PreviewCount";
        private const int MaxOutlinedItems = 300;
        private static readonly Vector3[] Corners = new Vector3[4];
        private static readonly Color ViewportColor = new Color(0.2f, 0.85f, 0.45f, 0.9f);
        private static readonly Color BufferColor = new Color(1f, 0.75f, 0.2f, 0.8f);
        private static readonly Color ItemColor = new Color(0.3f, 0.6f, 1f, 0.6f);
        #endregion

        #region [Unity Methods]
        public override void OnInspectorGUI()
        {
            var view = (GlobalVisualizerView)target;
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            if (changed && (Application.isPlaying || view.IsEditorPreview)) view.InvalidateSizes();

            DrawWarnings(view);
            DrawStatus(view);
            DrawPreview(view);

            EditorGUILayout.Space();
            if (GUILayout.Button("Create Item Template...")) ItemTemplateWindow.Open(view, null);
        }

        private void OnSceneGUI()
        {
            DrawSceneOverlay((GlobalVisualizerView)target);
        }
        #endregion

        #region [Private Methods]
        private static void DrawWarnings(GlobalVisualizerView view)
        {
            if (view.ItemPrefab == null)
            {
                EditorGUILayout.HelpBox("No Item Prefab: nothing will be shown. Assign one, or press Create Item Template.", MessageType.Warning);
            }

            VisualLayoutKind kind = view.LayoutSettings.Kind;
            if (view.SizeMode != VisualSizeMode.Fixed && kind == VisualLayoutKind.Grid)
            {
                EditorGUILayout.HelpBox("Grid uses one cell size for every item, so Size Mode is ignored. Use Flow for items of different sizes.", MessageType.Info);
            }
            else if (view.SizeMode == VisualSizeMode.Measure && view.ItemPrefab != null && !VisualMeasurer.CanMeasure(view.ItemPrefab))
            {
                EditorGUILayout.HelpBox("Size Mode is Measure, but the Item Prefab root has no layout component to measure (Layout Group, Layout Element or a text). Items keep the estimated size.", MessageType.Warning);
            }
            else if (view.SizeMode == VisualSizeMode.Provider)
            {
                EditorGUILayout.HelpBox("Provider mode reads sizes from view.SizeProvider, which is set from code. Until it is set, items use Item Size.", MessageType.Info);
            }

            ScrollRect scroll = view.GetComponent<ScrollRect>();
            if (scroll == null) return;
            if (scroll.viewport == null)
            {
                EditorGUILayout.HelpBox("The ScrollRect has no Viewport, so the ScrollRect itself is used as the viewport.", MessageType.Info);
            }
            if (scroll.content == null)
            {
                EditorGUILayout.HelpBox("The ScrollRect has no Content. One is created under the Viewport at runtime.", MessageType.Info);
                return;
            }

            LayoutGroup group = scroll.content.GetComponent<LayoutGroup>();
            if (group != null && group.enabled)
            {
                EditorGUILayout.HelpBox($"Content has an enabled {group.GetType().Name}. The visualizer positions items itself and disables it at runtime: remove it.", MessageType.Warning);
            }
            ContentSizeFitter fitter = scroll.content.GetComponent<ContentSizeFitter>();
            if (fitter != null && fitter.enabled)
            {
                EditorGUILayout.HelpBox("Content has an enabled ContentSizeFitter. The visualizer sizes the content itself and disables it at runtime: remove it.", MessageType.Warning);
            }
        }

        private static void DrawStatus(GlobalVisualizerView view)
        {
            if (!Application.isPlaying && !view.IsEditorPreview) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(view.IsEditorPreview ? "Status (preview)" : "Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Items", view.Count.ToString("N0"));
            EditorGUILayout.LabelField("Visible", view.VisibleRange.ToString());
            EditorGUILayout.LabelField("Views alive / created", $"{view.ActiveViewCount} / {view.CreatedViewCount}");
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Focused / selected", $"{view.FocusedIndex} / {view.SelectedCount}");
                if (view.IsShowingEmpty) EditorGUILayout.LabelField("Empty placeholder", "shown");
            }
            if (Application.isPlaying && GUILayout.Button("Refresh")) view.Refresh(true);
        }

        private static void DrawPreview(GlobalVisualizerView view)
        {
            if (Application.isPlaying) return;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);

            int count = EditorPrefs.GetInt(PreviewCountKey, 50);
            int next = EditorGUILayout.IntSlider("Preview Items", count, 1, 10000);
            if (next != count)
            {
                EditorPrefs.SetInt(PreviewCountKey, next);
                if (view.IsEditorPreview) VisualizerPreviewRegistry.Begin(view, next);
            }

            using (new EditorGUI.DisabledScope(view.ItemPrefab == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(view.IsEditorPreview ? "Update Preview" : "Show Preview")) VisualizerPreviewRegistry.Begin(view, next);
                using (new EditorGUI.DisabledScope(!view.IsEditorPreview))
                {
                    if (GUILayout.Button("Clear Preview")) VisualizerPreviewRegistry.End(view);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox("Preview items are never saved. They are removed before Play mode, script reloads and closing the scene. Components that implement IVisualItem<int> receive the index.", MessageType.None);
        }

        private static void DrawSceneOverlay(GlobalVisualizerView view)
        {
            ScrollRect scroll = view.GetComponent<ScrollRect>();
            if (scroll == null) return;
            RectTransform viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)view.transform;

            viewport.GetWorldCorners(Corners);
            Handles.DrawSolidRectangleWithOutline(Corners, new Color(ViewportColor.r, ViewportColor.g, ViewportColor.b, 0.04f), ViewportColor);
            Vector3 labelPosition = Corners[1];

            Rect rect = viewport.rect;
            float buffer = view.BufferPixels;
            Corners[0] = viewport.TransformPoint(new Vector3(rect.xMin - buffer, rect.yMin - buffer, 0f));
            Corners[1] = viewport.TransformPoint(new Vector3(rect.xMin - buffer, rect.yMax + buffer, 0f));
            Corners[2] = viewport.TransformPoint(new Vector3(rect.xMax + buffer, rect.yMax + buffer, 0f));
            Corners[3] = viewport.TransformPoint(new Vector3(rect.xMax + buffer, rect.yMin - buffer, 0f));
            Handles.DrawSolidRectangleWithOutline(Corners, Color.clear, BufferColor);

            VisualRange range = view.VisibleRange;
            if (!range.IsEmpty)
            {
                int last = Mathf.Min(range.Last, range.First + MaxOutlinedItems);
                for (int i = range.First; i <= last; i++)
                {
                    RectTransform item = view.GetView(i);
                    if (item == null) continue;
                    item.GetWorldCorners(Corners);
                    Handles.DrawSolidRectangleWithOutline(Corners, Color.clear, ItemColor);
                }
            }

            Handles.Label(labelPosition, $"Global Visualizer  visible {range}  alive {view.ActiveViewCount}  created {view.CreatedViewCount}", EditorStyles.whiteMiniLabel);
        }
        #endregion
    }
}
