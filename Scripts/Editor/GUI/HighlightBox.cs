using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        private static GUIStyle HighlightBoxStyle;
        private static Color HighlightBgColor = new Color(1f, 1f, 1f, 0.1f);
        public static void HighlightBox(Rect rect)
        {
            if (HighlightBoxStyle == null)
            {
                HighlightBoxStyle = new GUIStyle(GUI.skin.box);
                HighlightBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
            }
            using (new BackgroundColorScope(HighlightBgColor))
            {
                GUI.Box(rect, GUIContent.none, HighlightBoxStyle);
            }
        }
    }
}
