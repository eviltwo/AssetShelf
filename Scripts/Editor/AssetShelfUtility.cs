using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AssetShelf
{
    public static class AssetShelfUtility
    {
        public static bool HasDirectory(string path, string directoryName)
        {
            var pathParts = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar);
            var dirParts = directoryName.Split(Path.DirectorySeparatorChar);
            for (int i = 0; i < dirParts.Length; i++)
            {
                if (i >= pathParts.Length)
                {
                    return false;
                }
                if (pathParts[i] != dirParts[i])
                {
                    return false;
                }
            }
            return true;
        }

        private static int PreviewLoadingCount;
        private static int PreviewLoadingLimit = 1000;
        public static void LoadPreviewsIfNeeded(IReadOnlyList<AssetShelfContent> contents, int start, int end)
        {
            PreviewLoadingCount = 0;
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count || PreviewLoadingCount >= PreviewLoadingLimit)
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

            if (AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
            {
                PreviewLoadingCount++;
                return;
            }

            // Requesting every time to prevent the AssetPreview cache from being cleared.
            content.Preview = AssetPreview.GetAssetPreview(content.Asset);
            if (content.Preview != null)
            {
                return;
            }

            if (AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
            {
                PreviewLoadingCount++;
                return;
            }

            content.Preview = AssetPreview.GetMiniThumbnail(content.Asset);
            if (content.Preview != null || AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
            {
                return;
            }

            content.Preview = AssetPreview.GetMiniTypeThumbnail(content.Asset.GetType());
            if (content.Preview != null || AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
            {
                return;
            }

            content.Preview = EditorGUIUtility.whiteTexture;
        }
    }
}
