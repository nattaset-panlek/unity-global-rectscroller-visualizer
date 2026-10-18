using System;
using UnityEngine;

namespace GlobalVisualizer.EditorTools
{
    [Serializable]
    public sealed class ItemTemplateRequest
    {
        #region [Variables/Fields]
        public string TemplateName = "NewItem";
        public string DataTypeName = "int";
        public string Namespace = "";
        public string Folder = "Assets";
        public ItemTemplateShape Shape = ItemTemplateShape.Row;
        public Vector2 Size = new Vector2(320f, 64f);
        public string ScriptPath;
        public string PrefabPath;
        public string ClassFullName;
        #endregion

        #region [Properties]
        public string ClassName => TemplateName + "View";
        #endregion
    }
}
