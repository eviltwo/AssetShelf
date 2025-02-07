using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        private static GUIContent _loadingIcon;
        private static GUIStyle _loadingIconStyle;
        public static void DrawGridItem(Rect rect, AssetShelfContent content, bool isSelected)
        {
            if (_loadingIcon == null)
            {
                _loadingIcon = EditorGUIUtility.IconContent("Loading");
            }

            if (_loadingIconStyle == null)
            {
                _loadingIconStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter
                };
            }

            var drawContent = false;
            if (content != null)
            {
                if (content.Preview != null)
                {
                    GUI.DrawTexture(rect, content.Preview);
                    drawContent = true;
                }
                else if (content.Asset != null)
                {
                    var cacheTex = PreviewCache.GetTexture(content.Asset.GetInstanceID());
                    if (cacheTex != null)
                    {
                        GUI.DrawTexture(rect, cacheTex);
                        drawContent = true;
                    }
                }
            }

            if (!drawContent)
            {
                GUI.Box(rect, _loadingIcon, _loadingIconStyle);
            }

            if (isSelected)
            {
                HighlightBox(rect);
            }
        }
    }
}
