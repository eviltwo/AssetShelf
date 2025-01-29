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

        private static GUIContent _loadingIcon;
        public static void DrawGridItems(Rect rect, float itemSize, Vector2 spacing, IReadOnlyList<AssetShelfContent> contents, int start, int end, Object selectedItem)
        {
            if (_loadingIcon == null)
            {
                _loadingIcon = EditorGUIUtility.IconContent("Loading");
            }

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

        public static void DrawGridItem(Rect rect, AssetShelfContent content, bool isSelected)
        {
            if (content == null || content.Asset == null || content.Preview == null)
            {
                GUI.Box(rect, _loadingIcon);
            }
            else
            {
                GUI.DrawTexture(rect, content.Preview);
                if (isSelected)
                {
                    HighlightBox(rect);
                }
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

        public class BackgroundColorScope : System.IDisposable
        {
            private Color _previousColor;

            public BackgroundColorScope(Color color)
            {
                _previousColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = _previousColor;
            }
        }
    }
}
