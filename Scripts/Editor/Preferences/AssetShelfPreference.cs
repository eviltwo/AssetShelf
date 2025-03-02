using UnityEditor;

namespace AssetShelf
{
    [FilePath(nameof(AssetShelfPreference), FilePathAttribute.Location.PreferencesFolder)]
    public class AssetShelfPreference : ScriptableSingleton<AssetShelfPreference>
    {
        public bool ShowAssetName = true;
        public float AssetNameHeight = 18;

        public void Save()
        {
            Save(true);
        }
    }
}
