using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        public static float GetGridViewHeight(float itemSize, Vector2 spacing, float width, int itemCount)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, width);
            var rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            return rowCount * (itemSize + spacing.y) - spacing.y;
        }

        public static int GetGridColumnCount(float itemSize, float spacing, float width)
        {
            var columnCount = Mathf.FloorToInt((width - itemSize) / (itemSize + spacing)) + 1;
            columnCount = Mathf.Max(1, columnCount);
            return columnCount;
        }

        public static void DrawGridItems(Rect rect, float itemSize, Vector2 spacing, IReadOnlyList<AssetShelfContent> contents, int start, int end, Object selectedItem)
        {
            AssetShelfLog.LastDrawPreviewCount = 0;
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count)
                {
                    break;
                }

                var imageRect = new Rect(
                    rect.x + (i % columnCount) * (itemSize + spacing.x),
                    rect.y + (i / columnCount) * (itemSize + spacing.y),
                    itemSize,
                    itemSize
                );

                var content = contents[i];
                var isSelected = selectedItem != null && content?.Asset == selectedItem;
                DrawGridItem(imageRect, content, isSelected);
                AssetShelfLog.LastDrawPreviewCount++;
            }
        }

        private static GUIContent _loadingIcon;
        private static GUIStyle _loadingIconStyle;
        private static Texture2D _tex;
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

            if (_tex == null)
            {
                _tex = new Texture2D(2, 2);
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
                    if (PreviewCache.TryGetTexture(content.Asset.GetInstanceID(), _tex))
                    {
                        GUI.DrawTexture(rect, _tex);
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

        public static int GetIndexInGridView(float itemSize, Vector2 spacing, Rect rect, Vector2 position)
        {
            if (!rect.Contains(position))
            {
                return -1;
            }

            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            var column = Mathf.FloorToInt((position.x - rect.x) / (itemSize + spacing.x));
            var row = Mathf.FloorToInt((position.y - rect.y) / (itemSize + spacing.y));
            return column + row * columnCount;
        }
    }
}
