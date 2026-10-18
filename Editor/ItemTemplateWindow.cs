using System;
using UnityEditor;
using UnityEngine;

namespace GlobalVisualizer.EditorTools
{
    public sealed class ItemTemplateWindow : EditorWindow
    {
        #region [Variables/Fields]
        private static readonly Vector2 RowSize = new Vector2(320f, 64f);
        private static readonly Vector2 CellSize = new Vector2(96f, 96f);

        [SerializeField] private string _templateName = "NewItem";
        [SerializeField] private string _dataType = "int";
        [SerializeField] private string _namespace = string.Empty;
        [SerializeField] private string _folder = "Assets";
        [SerializeField] private ItemTemplateShape _shape = ItemTemplateShape.Row;
        [SerializeField] private Vector2 _size = new Vector2(320f, 64f);
        [SerializeField] private GlobalVisualizerView _target;

        private string _message;
        private MessageType _messageType = MessageType.None;
        #endregion

        #region [Public Methods]
        public static void Open(GlobalVisualizerView target, string folder)
        {
            var window = GetWindow<ItemTemplateWindow>(true, "Create Item Template", true);
            window.minSize = new Vector2(440f, 330f);
            if (target != null)
            {
                window._target = target;
                window._shape = target.LayoutSettings.Kind == VisualLayoutKind.Grid ? ItemTemplateShape.Cell : ItemTemplateShape.Row;
                window._size = SuggestSize(target, window._shape);
            }
            if (!string.IsNullOrEmpty(folder)) window._folder = folder;
            window._message = null;
            window.Show();
        }
        #endregion

        #region [Unity Methods]
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Creates an item prefab (background, icon, label) and a script that implements IVisualItem<T>. When Unity finishes compiling, the script is added to the prefab and its fields are linked.", MessageType.Info);
            EditorGUILayout.Space();

            _templateName = EditorGUILayout.TextField(new GUIContent("Template Name", "Prefab name. The script is <Template Name>View."), _templateName);
            _dataType = EditorGUILayout.TextField(new GUIContent("Data Type", "int, string, or a public type such as ItemInstance or CoreSystem.Data.ItemInstance."), _dataType);
            _namespace = EditorGUILayout.TextField(new GUIContent("Namespace", "Optional namespace for the script."), _namespace);

            EditorGUILayout.BeginHorizontal();
            _folder = EditorGUILayout.TextField("Folder", _folder);
            if (GUILayout.Button("...", GUILayout.Width(30f)))
            {
                string picked = ToProjectPath(EditorUtility.OpenFolderPanel("Item template folder", _folder, string.Empty));
                if (picked != null) _folder = picked;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            _shape = (ItemTemplateShape)EditorGUILayout.EnumPopup(new GUIContent("Shape", "Row: icon left, label right. Cell: icon top, label bottom."), _shape);
            if (EditorGUI.EndChangeCheck()) _size = _shape == ItemTemplateShape.Row ? RowSize : CellSize;
            _size = EditorGUILayout.Vector2Field("Size", _size);
            _target = (GlobalVisualizerView)EditorGUILayout.ObjectField(new GUIContent("Assign To", "Optional visualizer that gets the new prefab as its Item Prefab."), _target, typeof(GlobalVisualizerView), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Will create", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{_folder}/{_templateName}.prefab", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"{_folder}/{_templateName}View.cs", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);

            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(EditorApplication.isCompiling || Application.isPlaying))
            {
                if (GUILayout.Button("Create", GUILayout.Height(30f))) CreateTemplate();
            }
        }
        #endregion

        #region [Private Methods]
        private void CreateTemplate()
        {
            var request = new ItemTemplateRequest
            {
                TemplateName = _templateName != null ? _templateName.Trim() : string.Empty,
                DataTypeName = _dataType,
                Namespace = _namespace != null ? _namespace.Trim() : string.Empty,
                Folder = _folder,
                Shape = _shape,
                Size = new Vector2(Mathf.Max(16f, _size.x), Mathf.Max(16f, _size.y))
            };

            if (ItemTemplateFactory.Create(request, _target, out string error))
            {
                _message = $"Created {request.PrefabPath} and {request.ScriptPath}. The component is added to the prefab after Unity compiles the script.";
                _messageType = MessageType.Info;
            }
            else
            {
                _message = error;
                _messageType = MessageType.Error;
            }
        }

        private static Vector2 SuggestSize(GlobalVisualizerView target, ItemTemplateShape shape)
        {
            Vector2 configured = target.LayoutSettings.ItemSize;
            Vector2 fallback = shape == ItemTemplateShape.Row ? RowSize : CellSize;
            return new Vector2(configured.x > 0f ? configured.x : fallback.x, configured.y > 0f ? configured.y : fallback.y);
        }

        private static string ToProjectPath(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return null;
            string data = Application.dataPath.Replace('\\', '/');
            absolute = absolute.Replace('\\', '/');
            if (string.Equals(absolute, data, StringComparison.OrdinalIgnoreCase)) return "Assets";
            if (absolute.StartsWith(data + "/", StringComparison.OrdinalIgnoreCase)) return "Assets" + absolute.Substring(data.Length);
            return null;
        }
        #endregion
    }
}
