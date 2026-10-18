using System.Collections.Generic;
using GlobalVisualizer.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GlobalVisualizer.Tests
{
    public sealed class GlobalVisualizerEditorTests
    {
        #region [Variables/Fields]
        private const string TempFolder = "Assets/__GlobalVisualizerTestTemp";
        private readonly List<Object> _cleanup = new List<Object>();
        #endregion

        #region [Public Methods]
        [TearDown]
        public void TearDown()
        {
            VisualizerPreviewRegistry.ClearAll();
            for (int i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null) Object.DestroyImmediate(_cleanup[i]);
            }
            _cleanup.Clear();
            if (AssetDatabase.IsValidFolder(TempFolder)) AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void Factory_CreatesAVerticalVisualizer()
        {
            RectTransform parent = CreateParent();

            GlobalVisualizerView view = GlobalVisualizerFactory.Create(VisualLayoutKind.Vertical, parent);
            ScrollRect scroll = view.GetComponent<ScrollRect>();

            Assert.AreSame(parent, view.transform.parent);
            Assert.AreEqual(VisualLayoutKind.Vertical, view.LayoutSettings.Kind);
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>());
            Assert.IsNull(scroll.viewport.GetComponent<Mask>());
            Assert.IsNull(scroll.horizontalScrollbar);
            Assert.IsNotNull(scroll.verticalScrollbar);
            Assert.IsNotNull(view.ItemPrefab);
            Assert.IsFalse(view.ItemPrefab.gameObject.activeSelf);
            Assert.IsTrue(scroll.vertical);
            Assert.IsFalse(scroll.horizontal);
        }

        [Test]
        public void Factory_CreatesHorizontalAndGridVisualizers()
        {
            RectTransform parent = CreateParent();

            GlobalVisualizerView horizontal = GlobalVisualizerFactory.Create(VisualLayoutKind.Horizontal, parent);
            GlobalVisualizerView grid = GlobalVisualizerFactory.Create(VisualLayoutKind.Grid, parent);

            Assert.IsNull(horizontal.GetComponent<ScrollRect>().verticalScrollbar);
            Assert.IsNotNull(horizontal.GetComponent<ScrollRect>().horizontalScrollbar);
            Assert.AreEqual(VisualLayoutKind.Grid, grid.LayoutSettings.Kind);
            Assert.AreEqual(new Vector2(100f, 100f), grid.ItemPrefab.sizeDelta);
        }

        [Test]
        public void Factory_CreatesAFlowVisualizerThatMeasuresItems()
        {
            GlobalVisualizerView view = GlobalVisualizerFactory.Create(VisualLayoutKind.Flow, CreateParent());
            ScrollRect scroll = view.GetComponent<ScrollRect>();

            Assert.AreEqual(VisualLayoutKind.Flow, view.LayoutSettings.Kind);
            Assert.AreEqual(VisualSizeMode.Measure, view.SizeMode);
            Assert.IsNotNull(view.ItemPrefab.GetComponent<HorizontalLayoutGroup>());
            Assert.IsTrue(VisualMeasurer.CanMeasure(view.ItemPrefab));
            Assert.IsNull(scroll.horizontalScrollbar);
            Assert.IsTrue(scroll.vertical);
        }

        [Test]
        public void Factory_AddsAnEmptyTemplate()
        {
            GlobalVisualizerView view = GlobalVisualizerFactory.Create(VisualLayoutKind.Grid, CreateParent());

            Assert.IsNotNull(view.EmptyPrefab);
            Assert.AreEqual("Empty Template", view.EmptyPrefab.name);
            Assert.IsFalse(view.EmptyPrefab.gameObject.activeSelf);

            view.SetCount(0);
            Assert.IsTrue(view.IsShowingEmpty);
        }

        [Test]
        public void Preview_RemovesUnsavedViewportChildren()
        {
            GlobalVisualizerView view = GlobalVisualizerFactory.Create(VisualLayoutKind.Vertical, CreateParent());
            VisualizerPreviewRegistry.Begin(view, 20);
            var stray = new GameObject("Stray", typeof(RectTransform));
            stray.hideFlags = HideFlags.DontSave;
            stray.transform.SetParent(view.Viewport, false);

            VisualizerPreviewRegistry.End(view);

            Assert.IsNull(view.Viewport.Find("Stray"));
        }

        [Test]
        public void Preview_CreatesUnsavedViewsAndRemovesThem()
        {
            GlobalVisualizerView view = GlobalVisualizerFactory.Create(VisualLayoutKind.Vertical, CreateParent());

            VisualizerPreviewRegistry.Begin(view, 200);

            Assert.IsTrue(view.IsEditorPreview);
            Assert.Greater(view.ActiveViewCount, 0);
            Assert.AreEqual(200, view.Count);
            foreach (Transform child in view.Content)
            {
                Assert.AreNotEqual(HideFlags.None, child.gameObject.hideFlags & HideFlags.DontSaveInEditor);
            }

            VisualizerPreviewRegistry.End(view);

            Assert.IsFalse(view.IsEditorPreview);
            Assert.AreEqual(0, view.Content.childCount);
            Assert.AreEqual(0, view.CreatedViewCount);
            Assert.AreEqual(0, VisualizerPreviewRegistry.ActiveCount);
        }

        [Test]
        public void ScriptTemplate_FollowsTheProjectLayout()
        {
            string code = ItemTemplateFactory.BuildScript("InventorySlotView", "Game.UI", typeof(int), false);

            StringAssert.Contains("namespace Game.UI", code);
            StringAssert.Contains("public sealed class InventorySlotView : MonoBehaviour, IVisualItem<int>", code);
            StringAssert.Contains("private Text _label;", code);
            StringAssert.Contains("_label.text = data.ToString();", code);
            foreach (string region in new[] { "[Variables/Fields]", "[Properties]", "[Unity Methods]", "[Public Methods]", "[Private Methods]" })
            {
                StringAssert.Contains("#region " + region, code);
            }
            StringAssert.DoesNotContain("//", code);
        }

        [Test]
        public void ScriptTemplate_UsesTextMeshProAndFullTypeNames()
        {
            string tmp = ItemTemplateFactory.BuildScript("NameRowView", string.Empty, typeof(string), true);
            string custom = ItemTemplateFactory.BuildScript("RangeView", string.Empty, typeof(VisualRange), false);

            StringAssert.Contains("using TMPro;", tmp);
            StringAssert.Contains("private TMP_Text _label;", tmp);
            StringAssert.Contains("data != null", tmp);
            StringAssert.DoesNotContain("namespace", tmp);
            StringAssert.Contains("IVisualItem<global::GlobalVisualizer.VisualRange>", custom);
        }

        [Test]
        public void ResolveType_FindsAliasesFullAndSimpleNames()
        {
            Assert.AreEqual(typeof(int), ItemTemplateFactory.ResolveType("int", out _));
            Assert.AreEqual(typeof(string), ItemTemplateFactory.ResolveType("System.String", out _));
            Assert.AreEqual(typeof(VisualRange), ItemTemplateFactory.ResolveType("GlobalVisualizer.VisualRange", out _));
            Assert.IsNull(ItemTemplateFactory.ResolveType("NoSuchVisualizerType123", out string error));
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        [Test]
        public void IsValidName_RejectsBadIdentifiers()
        {
            Assert.IsTrue(ItemTemplateFactory.IsValidName("InventorySlot"));
            Assert.IsTrue(ItemTemplateFactory.IsValidName("_Row2"));
            Assert.IsFalse(ItemTemplateFactory.IsValidName("1Slot"));
            Assert.IsFalse(ItemTemplateFactory.IsValidName("Inventory Slot"));
            Assert.IsFalse(ItemTemplateFactory.IsValidName("class"));
            Assert.IsFalse(ItemTemplateFactory.IsValidName(string.Empty));
            Assert.IsTrue(ItemTemplateFactory.IsValidNamespace("Game.UI.Inventory"));
            Assert.IsFalse(ItemTemplateFactory.IsValidNamespace("Game..UI"));
        }

        [Test]
        public void CreatePrefab_BuildsBackgroundIconAndLabel()
        {
            AssetDatabase.CreateFolder("Assets", "__GlobalVisualizerTestTemp");

            GameObject prefab = ItemTemplateFactory.CreatePrefab(TempFolder + "/TestRow.prefab", ItemTemplateShape.Row, new Vector2(320f, 64f));

            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<Image>());
            Assert.AreEqual(new Vector2(320f, 64f), ((RectTransform)prefab.transform).sizeDelta);
            Assert.IsNotNull(prefab.transform.Find("Icon"));
            Assert.IsNotNull(prefab.transform.Find("Label").GetComponent<Graphic>());
        }
        #endregion

        #region [Private Methods]
        private RectTransform CreateParent()
        {
            var parent = new GameObject("Test Canvas Root", typeof(RectTransform));
            _cleanup.Add(parent);
            var rect = (RectTransform)parent.transform;
            rect.sizeDelta = new Vector2(1920f, 1080f);
            return rect;
        }
        #endregion
    }
}
