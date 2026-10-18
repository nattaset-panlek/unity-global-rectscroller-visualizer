using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GlobalVisualizer.EditorTools
{
    public static class GlobalVisualizerFactory
    {
        #region [Variables/Fields]
        private const string TextMeshProTypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string TextMeshProBaseTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";
        private const string InputSystemModuleTypeName = "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem";
        private const string SpritePath = "UI/Skin/UISprite.psd";
        private const string BackgroundPath = "UI/Skin/Background.psd";
        private const string MaskPath = "UI/Skin/UIMask.psd";
        private const string KnobPath = "UI/Skin/Knob.psd";
        private const string CheckmarkPath = "UI/Skin/Checkmark.psd";
        private const string DropdownPath = "UI/Skin/DropdownArrow.psd";
        private const string InputFieldPath = "UI/Skin/InputFieldBackground.psd";
        #endregion

        #region [Properties]
        public static Type TextMeshProType => Type.GetType(TextMeshProTypeName);
        public static bool HasTextMeshPro => Type.GetType(TextMeshProTypeName) != null && Type.GetType(TextMeshProBaseTypeName) != null;
        #endregion

        #region [Public Methods]
        public static GlobalVisualizerView Create(VisualLayoutKind kind, Transform parent)
        {
            GameObject root = DefaultControls.CreateScrollView(DefaultResources());
            root.name = "Global Visualizer (" + kind + ")";
            var rootRect = (RectTransform)root.transform;
            if (parent != null) rootRect.SetParent(parent, false);
            rootRect.sizeDelta = DefaultSize(kind);

            ScrollRect scroll = root.GetComponent<ScrollRect>();
            scroll.scrollSensitivity = 30f;
            UseRectMask(scroll.viewport);
            if (kind == VisualLayoutKind.Horizontal) RemoveScrollbar(scroll, false);
            else RemoveScrollbar(scroll, true);

            var view = root.AddComponent<GlobalVisualizerView>();
            view.SetLayout(DefaultLayout(kind));
            if (kind == VisualLayoutKind.Flow) view.SizeMode = VisualSizeMode.Measure;
            view.ItemPrefab = kind == VisualLayoutKind.Flow ? CreateFlowTemplate(rootRect) : CreateSceneTemplate(rootRect, kind);
            view.EmptyPrefab = CreateEmptyTemplate(rootRect);
            SetLayerRecursively(root, parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI"));
            return view;
        }

        public static Transform ResolveParent(GameObject context)
        {
            if (context != null && context.GetComponentInParent<Canvas>(true) != null) return context.transform;
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) return canvas.rootCanvas.transform;
            return CreateCanvas(context != null ? context.transform : null).transform;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            Type inputSystemModule = null;
#if ENABLE_INPUT_SYSTEM
            inputSystemModule = Type.GetType(InputSystemModuleTypeName);
#endif
            if (inputSystemModule != null) go.AddComponent(inputSystemModule);
            else go.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        public static Graphic CreateLabel(Transform parent, string name, string text, float fontSize, bool center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            Type textMeshPro = TextMeshProType;
            if (textMeshPro != null)
            {
                var graphic = (Graphic)go.AddComponent(textMeshPro);
                SetProperty(graphic, "text", text);
                SetProperty(graphic, "fontSize", fontSize);
                SetEnumProperty(graphic, "alignment", center ? "Center" : "MidlineLeft");
                graphic.color = new Color(0.1f, 0.1f, 0.1f);
                graphic.raycastTarget = false;
                return graphic;
            }
            var label = go.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = text;
            label.fontSize = Mathf.RoundToInt(fontSize);
            label.alignment = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            label.color = new Color(0.1f, 0.1f, 0.1f);
            label.raycastTarget = false;
            return label;
        }

        public static Vector2 DefaultSize(VisualLayoutKind kind)
        {
            switch (kind)
            {
                case VisualLayoutKind.Horizontal: return new Vector2(640f, 180f);
                case VisualLayoutKind.Grid: return new Vector2(540f, 520f);
                case VisualLayoutKind.Flow: return new Vector2(420f, 320f);
                default: return new Vector2(400f, 520f);
            }
        }

        public static VisualLayoutSettings DefaultLayout(VisualLayoutKind kind)
        {
            VisualLayoutSettings settings;
            switch (kind)
            {
                case VisualLayoutKind.Horizontal: settings = VisualLayoutSettings.Horizontal(140f, 8f); break;
                case VisualLayoutKind.Grid: settings = VisualLayoutSettings.Grid(new Vector2(100f, 100f), 8f); break;
                case VisualLayoutKind.Flow: settings = VisualLayoutSettings.Flow(8f, new Vector2(100f, 36f)); break;
                default: settings = VisualLayoutSettings.Vertical(56f, 6f); break;
            }
            settings.Padding = new VisualPadding(6f);
            return settings;
        }
        #endregion

        #region [Private Methods]
        private static DefaultControls.Resources DefaultResources()
        {
            return new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpritePath),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>(BackgroundPath),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>(InputFieldPath),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>(KnobPath),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>(CheckmarkPath),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>(DropdownPath),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>(MaskPath)
            };
        }

        private static void UseRectMask(RectTransform viewport)
        {
            if (viewport == null) return;
            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null) Object.DestroyImmediate(mask);
            Image image = viewport.GetComponent<Image>();
            if (image != null) Object.DestroyImmediate(image);
            if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
        }

        private static void RemoveScrollbar(ScrollRect scroll, bool horizontal)
        {
            Scrollbar bar = horizontal ? scroll.horizontalScrollbar : scroll.verticalScrollbar;
            if (bar != null) Object.DestroyImmediate(bar.gameObject);
            if (horizontal) scroll.horizontalScrollbar = null;
            else scroll.verticalScrollbar = null;
        }

        private static RectTransform CreateSceneTemplate(RectTransform parent, VisualLayoutKind kind)
        {
            var go = new GameObject("Item Template", typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = kind == VisualLayoutKind.Grid ? new Vector2(100f, 100f) : kind == VisualLayoutKind.Horizontal ? new Vector2(140f, 156f) : new Vector2(388f, 56f);

            Image background = go.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpritePath);
            background.type = Image.Type.Sliced;
            background.color = new Color(0.9f, 0.9f, 0.9f);

            Graphic label = CreateLabel(rect, "Label", "Item", 18f, kind != VisualLayoutKind.Vertical);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);

            go.SetActive(false);
            return rect;
        }

        private static RectTransform CreateFlowTemplate(RectTransform parent)
        {
            var go = new GameObject("Item Template", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(100f, 36f);

            Image background = go.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(SpritePath);
            background.type = Image.Type.Sliced;
            background.color = new Color(0.9f, 0.9f, 0.9f);

            HorizontalLayoutGroup group = go.GetComponent<HorizontalLayoutGroup>();
            group.padding = new RectOffset(14, 14, 6, 6);
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            CreateLabel(rect, "Label", "Tag", 18f, true);
            go.SetActive(false);
            return rect;
        }

        private static RectTransform CreateEmptyTemplate(RectTransform parent)
        {
            var go = new GameObject("Empty Template", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Graphic label = CreateLabel(rect, "Label", "Nothing to show", 18f, true);
            label.color = new Color(0.45f, 0.45f, 0.45f);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 12f);
            labelRect.offsetMax = new Vector2(-12f, -12f);

            go.SetActive(false);
            return rect;
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Undo.RegisterCreatedObjectUndo(go, "Create Canvas");
            return canvas;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (layer < 0) return;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }

        private static void SetProperty(object target, string name, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite) property.SetValue(target, value);
        }

        private static void SetEnumProperty(object target, string name, string valueName)
        {
            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum) return;
            if (!Enum.IsDefined(property.PropertyType, valueName)) return;
            property.SetValue(target, Enum.Parse(property.PropertyType, valueName));
        }
        #endregion
    }
}
