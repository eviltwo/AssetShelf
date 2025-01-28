using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfContent
    {
        public Object Asset;
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
