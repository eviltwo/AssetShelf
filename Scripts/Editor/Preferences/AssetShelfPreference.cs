using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    [FilePath(nameof(AssetShelfPreference), FilePathAttribute.Location.PreferencesFolder)]
    public class AssetShelfPreference : ScriptableSingleton<AssetShelfPreference>
    {
        public float SidebarWidth = 200;
        public bool ShowAssetName = true;
        public float AssetNameHeight = 18;
        public Vector2 GridSpacing = new Vector2(5, 5);

        public void Save()
        {
            Save(true);
        }
    }
}
