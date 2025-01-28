using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static class AssetShelfGUI
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

        public static void LoadPreviewsIfNeeded(IReadOnlyList<AssetShelfContent> contents, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count)
                {
                    break;
                }

                LoadPreviewIfNeeded(contents[i]);
            }
        }

        public static void LoadPreviewIfNeeded(AssetShelfContent content)
        {
            if (content == null || content.Asset == null)
            {
                return;
            }

            if (content.MiniPreview == null)
            {
                content.MiniPreview = AssetPreview.GetMiniThumbnail(content.Asset);
            }
            if (content.Preview == null && !content.SkipPreview)
            {
                content.Preview = AssetPreview.GetAssetPreview(content.Asset);
                AssetShelfLog.LoadPreviewTotalCount++;

                if (content.Preview == null && !AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
                {
                    content.SkipPreview = true;
                }
            }
        }

        public static void DrawGridItems(Rect rect, float itemSize, Vector2 spacing, IReadOnlyList<AssetShelfContent> contents, int start, int end)
        {
            AssetShelfLog.LastDrawPreviewCount = 0;
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count)
                {
                    break;
                }

                var content = contents[i];
                if (content == null)
                {
                    continue;
                }

                var preview = content.Preview;
                if (preview == null)
                {
                    preview = content.MiniPreview;
                }
                if (preview == null)
                {
                    preview = EditorGUIUtility.whiteTexture;
                }

                var imageRect = new Rect(
                    rect.x + (i % columnCount) * (itemSize + spacing.x),
                    rect.y + (i / columnCount) * (itemSize + spacing.y),
                    itemSize,
                    itemSize
                );
                GUI.DrawTexture(imageRect, preview);
                AssetShelfLog.LastDrawPreviewCount++;
            }
        }

        public static int GetIndexInGridView(float itemSize, Vector2 spacing, Rect rect, Vector2 position)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            var column = Mathf.FloorToInt((position.x - rect.x) / (itemSize + spacing.x));
            var row = Mathf.FloorToInt((position.y - rect.y) / (itemSize + spacing.y));
            return column + row * columnCount;
        }
    }
}
