using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static class AssetShelfGUILayout
    {

        public static bool FoldoutSelectButton(bool selected, string label, ref bool foldout)
        {
            var rect = EditorGUILayout.GetControlRect();
            if (selected)
            {
                SelectedBox(rect);
            }

            var foldoutRect = new Rect(rect.x, rect.y, 15, rect.height);
            foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none);
            var buttonRect = EditorGUI.IndentedRect(new Rect(rect.x + 15, rect.y, rect.width - 15, rect.height));
            return GUI.Button(buttonRect, label, GUI.skin.label);
        }

        public static bool SelectButton(bool selected, string label)
        {
            var rect = EditorGUILayout.GetControlRect();
            if (selected)
            {
                SelectedBox(rect);
            }

            var buttonRect = EditorGUI.IndentedRect(new Rect(rect.x + 15, rect.y, rect.width - 15, rect.height));
            return GUI.Button(buttonRect, label, GUI.skin.label);
        }

        private static Color SelectedBgColor = new Color(1f, 1f, 1f, 0.1f);
        private static void SelectedBox(Rect rect)
        {
            var selectedBoxStyle = new GUIStyle(GUI.skin.box);
            selectedBoxStyle.normal.background = EditorGUIUtility.whiteTexture;
            using (new AssetShelfGUI.BackgroundColorScope(SelectedBgColor))
            {
                GUI.Box(rect, GUIContent.none, selectedBoxStyle);
            }
        }
    }
}
