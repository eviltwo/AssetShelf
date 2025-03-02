using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        private static GUIContent _loadingIcon;
        private static GUIStyle _loadingIconStyle;
        private static GUIStyle _labelStyle;
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

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                {
                    alignment = TextAnchor.UpperCenter
                };
            }

            var imgSize = rect.width;
            var imgRect = new Rect(rect.x, rect.y, imgSize, imgSize);
            var labelRect = new Rect(rect.x, rect.y + imgSize, rect.width, rect.height - imgSize);

            // Draw thumbnail
            var drawContent = false;
            if (content != null)
            {
                if (content.Preview != null)
                {
                    GUI.DrawTexture(imgRect, content.Preview);
                    drawContent = true;
                }
                else if (content.Asset != null)
                {
                    var cacheTex = PreviewCache.GetTexture(content.Asset.GetInstanceID());
                    if (cacheTex != null)
                    {
                        GUI.DrawTexture(imgRect, cacheTex);
                        drawContent = true;
                    }
                }
            }

            if (!drawContent)
            {
                GUI.Box(imgRect, _loadingIcon, _loadingIconStyle);
            }

            // Draw label
            if (content != null && labelRect.height > 0)
            {
                var name = Path.GetFileNameWithoutExtension(content.Path);
                var labelContent = new GUIContent(name);
                var style = _labelStyle;
                GUI.Label(labelRect, labelContent, style);
            }

            // Draw selection highlight
            if (isSelected)
            {
                HighlightBox(rect);
            }
        }
    }
}
