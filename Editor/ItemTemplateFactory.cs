using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GlobalVisualizer.EditorTools
{
    [InitializeOnLoad]
    public static class ItemTemplateFactory
    {
        #region [Variables/Fields]
        private const string PendingKey = "GlobalVisualizer.PendingItemTemplate";
        private const string SpritePath = "UI/Skin/UISprite.psd";

        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        };

        private static readonly Dictionary<string, Type> Aliases = new Dictionary<string, Type>
        {
            { "int", typeof(int) },
            { "float", typeof(float) },
            { "double", typeof(double) },
            { "long", typeof(long) },
            { "bool", typeof(bool) },
            { "string", typeof(string) },
            { "object", typeof(object) }
        };
        #endregion

        #region [Unity Methods]
        static ItemTemplateFactory()
        {
            EditorApplication.delayCall += CompletePending;
        }
        #endregion

        #region [Public Methods]
        public static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$") && !Keywords.Contains(name);
        }

        public static bool IsValidNamespace(string value)
        {
            return string.IsNullOrEmpty(value) || value.Split('.').All(IsValidName);
        }

        public static Type ResolveType(string typeName, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                error = "Enter a data type, for example int, string or ItemInstance.";
                return null;
            }

            typeName = typeName.Trim();
            if (Aliases.TryGetValue(typeName, out Type alias)) return alias;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type exact = assembly.GetType(typeName, false);
                if (exact != null) return Validate(exact, out error);
            }

            Type match = null;
            int matches = 0;
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in SafeTypes(assembly))
                {
                    if (type.Name != typeName || !type.IsVisible) continue;
                    match = type;
                    matches++;
                }
            }

            if (matches == 1) return Validate(match, out error);
            error = matches == 0
                ? $"Type '{typeName}' was not found in the loaded assemblies."
                : $"'{typeName}' matches {matches} types. Use the full name with its namespace.";
            return null;
        }

        public static string CodeName(Type type)
        {
            foreach (KeyValuePair<string, Type> pair in Aliases)
            {
                if (pair.Value == type) return pair.Key;
            }
            string name = (type.FullName ?? type.Name).Replace('+', '.');
            if (!type.IsGenericType) return "global::" + name;
            string baseName = name.Substring(0, name.IndexOf('`'));
            return "global::" + baseName + "<" + string.Join(", ", type.GetGenericArguments().Select(CodeName)) + ">";
        }

        public static string BuildScript(string className, string scriptNamespace, Type dataType, bool useTextMeshPro)
        {
            string dataName = CodeName(dataType);
            string labelType = useTextMeshPro ? "TMP_Text" : "Text";
            string bindLine = dataType.IsValueType ? "_label.text = data.ToString();" : "_label.text = data != null ? data.ToString() : string.Empty;";
            bool hasNamespace = !string.IsNullOrEmpty(scriptNamespace);
            string indent = hasNamespace ? "    " : string.Empty;
            var builder = new StringBuilder();

            builder.AppendLine("using GlobalVisualizer;");
            if (useTextMeshPro) builder.AppendLine("using TMPro;");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine("using UnityEngine.UI;");
            builder.AppendLine();
            if (hasNamespace)
            {
                builder.AppendLine("namespace " + scriptNamespace);
                builder.AppendLine("{");
            }

            string[] lines =
            {
                $"public sealed class {className} : MonoBehaviour, IVisualItem<{dataName}>",
                "{",
                "    #region [Variables/Fields]",
                "    [SerializeField] private Image _background;",
                "    [SerializeField] private Image _icon;",
                $"    [SerializeField] private {labelType} _label;",
                "    #endregion",
                "",
                "    #region [Properties]",
                "    #endregion",
                "",
                "    #region [Unity Methods]",
                "    #endregion",
                "",
                "    #region [Public Methods]",
                $"    public void Bind({dataName} data, VisualItemInfo info)",
                "    {",
                "        " + bindLine,
                "    }",
                "",
                "    public void Unbind()",
                "    {",
                "    }",
                "    #endregion",
                "",
                "    #region [Private Methods]",
                "    #endregion",
                "}"
            };
            foreach (string line in lines) builder.AppendLine(line.Length == 0 ? string.Empty : indent + line);

            if (hasNamespace) builder.AppendLine("}");
            return builder.ToString();
        }

        public static GameObject CreatePrefab(string path, ItemTemplateShape shape, Vector2 size)
        {
            var root = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(RectTransform), typeof(Image));
            try
            {
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0) root.layer = uiLayer;
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = size;

                Image background = root.GetComponent<Image>();
                background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpritePath);
                background.type = Image.Type.Sliced;
                background.color = new Color(0.92f, 0.92f, 0.92f);

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.layer = root.layer;
                var iconRect = (RectTransform)icon.transform;
                iconRect.SetParent(rect, false);
                Image iconImage = icon.GetComponent<Image>();
                iconImage.color = new Color(0.25f, 0.25f, 0.25f);
                iconImage.raycastTarget = false;

                Graphic label = GlobalVisualizerFactory.CreateLabel(rect, "Label", "Item", shape == ItemTemplateShape.Row ? 18f : 14f, shape == ItemTemplateShape.Cell);
                RectTransform labelRect = label.rectTransform;

                if (shape == ItemTemplateShape.Row)
                {
                    float iconSize = Mathf.Max(16f, size.y - 16f);
                    iconRect.anchorMin = new Vector2(0f, 0.5f);
                    iconRect.anchorMax = new Vector2(0f, 0.5f);
                    iconRect.pivot = new Vector2(0f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(8f, 0f);
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(iconSize + 20f, 4f);
                    labelRect.offsetMax = new Vector2(-8f, -4f);
                }
                else
                {
                    float iconSize = Mathf.Max(16f, Mathf.Min(size.x, size.y) - 36f);
                    iconRect.anchorMin = new Vector2(0.5f, 1f);
                    iconRect.anchorMax = new Vector2(0.5f, 1f);
                    iconRect.pivot = new Vector2(0.5f, 1f);
                    iconRect.anchoredPosition = new Vector2(0f, -6f);
                    iconRect.sizeDelta = new Vector2(iconSize, iconSize);
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = new Vector2(1f, 0f);
                    labelRect.pivot = new Vector2(0.5f, 0f);
                    labelRect.anchoredPosition = new Vector2(0f, 4f);
                    labelRect.sizeDelta = new Vector2(-8f, 24f);
                }

                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        public static bool Create(ItemTemplateRequest request, GlobalVisualizerView target, out string error)
        {
            error = null;
            if (!IsValidName(request.TemplateName))
            {
                error = "Template Name must be a C# identifier (letters, digits, underscore, not starting with a digit).";
                return false;
            }
            if (!IsValidNamespace(request.Namespace))
            {
                error = "Namespace must be empty or dot-separated C# identifiers.";
                return false;
            }
            string folder = (request.Folder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder))
            {
                error = $"Folder '{folder}' does not exist in the project.";
                return false;
            }

            Type dataType = ResolveType(request.DataTypeName, out error);
            if (dataType == null) return false;

            request.Folder = folder;
            request.ScriptPath = $"{folder}/{request.ClassName}.cs";
            request.PrefabPath = $"{folder}/{request.TemplateName}.prefab";
            request.ClassFullName = string.IsNullOrEmpty(request.Namespace) ? request.ClassName : request.Namespace + "." + request.ClassName;

            if (File.Exists(request.ScriptPath) || File.Exists(request.PrefabPath))
            {
                error = $"'{request.ScriptPath}' or '{request.PrefabPath}' already exists.";
                return false;
            }
            if (ResolveType(request.ClassFullName, out _) != null)
            {
                error = $"A type named '{request.ClassFullName}' already exists. Pick another Template Name or Namespace.";
                return false;
            }

            bool useTextMeshPro = GlobalVisualizerFactory.HasTextMeshPro;
            File.WriteAllText(request.ScriptPath, BuildScript(request.ClassName, request.Namespace, dataType, useTextMeshPro));
            GameObject prefab = CreatePrefab(request.PrefabPath, request.Shape, request.Size);

            if (target != null && prefab != null)
            {
                Undo.RecordObject(target, "Assign Item Template");
                target.ItemPrefab = (RectTransform)prefab.transform;
                EditorUtility.SetDirty(target);
            }

            SessionState.SetString(PendingKey, JsonUtility.ToJson(request));
            AssetDatabase.ImportAsset(request.ScriptPath);
            EditorApplication.delayCall += CompletePending;
            return true;
        }
        #endregion

        #region [Private Methods]
        private static void CompletePending()
        {
            string json = SessionState.GetString(PendingKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += CompletePending;
                return;
            }

            var request = JsonUtility.FromJson<ItemTemplateRequest>(json);
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(request.ScriptPath);
            Type componentType = script != null ? script.GetClass() : null;
            if (componentType == null) return;
            SessionState.EraseString(PendingKey);

            GameObject contents = PrefabUtility.LoadPrefabContents(request.PrefabPath);
            try
            {
                Component component = contents.GetComponent(componentType);
                if (component == null) component = contents.AddComponent(componentType);
                var serialized = new SerializedObject(component);
                Assign(serialized, "_background", contents.GetComponent<Image>());
                Transform icon = contents.transform.Find("Icon");
                Assign(serialized, "_icon", icon != null ? icon.GetComponent<Image>() : null);
                Transform label = contents.transform.Find("Label");
                Assign(serialized, "_label", label != null ? label.GetComponent<Graphic>() : null);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, request.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log($"Global Visualizer: item template '{request.TemplateName}' is ready ({request.PrefabPath}, {request.ScriptPath}).", AssetDatabase.LoadAssetAtPath<GameObject>(request.PrefabPath));
        }

        private static void Assign(SerializedObject serialized, string field, Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null && value != null) property.objectReferenceValue = value;
        }

        private static Type Validate(Type type, out string error)
        {
            error = null;
            if (type.ContainsGenericParameters)
            {
                error = "Open generic types are not supported.";
                return null;
            }
            if (!type.IsVisible)
            {
                error = "The data type must be public.";
                return null;
            }
            return type;
        }

        private static IEnumerable<Type> SafeTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }
        #endregion
    }
}
