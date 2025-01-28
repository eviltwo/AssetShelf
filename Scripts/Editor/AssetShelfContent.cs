using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfContent
    {
        private Object _asset;

        public Object Asset
        {
            get
            {
                if (_asset == null)
                {
                    _asset = AssetDatabase.LoadAssetAtPath<Object>(Path);
                }
                return _asset;
            }
        }

        public string Path;
        public Texture2D MiniPreview;
        public Texture2D Preview;
        public bool SkipPreview;
    }

    public class AssetShelfContentGroup
    {
        public string Name;
        public List<AssetShelfContent> Contents = new List<AssetShelfContent>();
    }
}
