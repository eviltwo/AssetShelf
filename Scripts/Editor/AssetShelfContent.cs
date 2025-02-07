using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfContent
    {
        private static int LoadAssetCount;
        public static int LoadAssetLimit = 100;
        public static bool IsLimitted => LoadAssetCount >= LoadAssetLimit;

        public static void ResetLoadAssetCount()
        {
            LoadAssetCount = 0;
        }

        private Object _asset;

        public Object Asset
        {
            get
            {
                if (_asset == null && LoadAssetCount < LoadAssetLimit)
                {
                    LoadAssetCount++;
                    _asset = AssetDatabase.LoadAssetAtPath<Object>(Path);
                }
                return _asset;
            }
        }

        public string Path;
        public Texture2D Preview;
    }

    public class AssetShelfContentGroup
    {
        public string Name;
        public List<AssetShelfContent> Contents = new List<AssetShelfContent>();
    }
}
