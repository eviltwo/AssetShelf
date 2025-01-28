using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static class AssetShelfGUILayout
    {
        private static Color SelectedBgColor = new Color(1f, 1f, 1f, 0.1f);
        public static bool FoldoutSelectButton(bool selected, string content, ref bool foldout)
        {
            var rect = EditorGUILayout.GetControlRect();
            if (selected)
            {
                var selectedBoxStyle = new GUIStyle(GUI.skin.box);
                selectedBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
                using (new AssetShelfGUI.BackgroundColorScope(SelectedBgColor))
                {
                    GUI.Box(rect, GUIContent.none, selectedBoxStyle);
                }
            }

            var foldoutRect = new Rect(rect.x, rect.y, 15, rect.height);
            foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none);
            var buttonRect = EditorGUI.IndentedRect(new Rect(rect.x + 15, rect.y, rect.width - 15, rect.height));
            return GUI.Button(buttonRect, content, GUI.skin.label);
        }
    }
}
