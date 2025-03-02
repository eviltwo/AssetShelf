using UnityEditor;

namespace AssetShelf
{
    [System.Serializable]
    public class AssetShelfRule
    {
        public string Name;

        public string SearchFilter;

        public DefaultAsset[] Folders;
    }
}
