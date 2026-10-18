using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static class AssetPreviewCompat
    {
        public static bool IsLoadingAssetPreview(Object asset)
        {
#if UNITY_6000_3_OR_NEWER
            return AssetPreview.IsLoadingAssetPreview(asset.GetEntityId());
#else
            return AssetPreview.IsLoadingAssetPreview(asset.GetInstanceID());
#endif
        }
    }
}
