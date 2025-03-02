using UnityEditor;

namespace AssetShelf
{
    [FilePath(nameof(AssetShelfPreference), FilePathAttribute.Location.PreferencesFolder)]
    public class AssetShelfPreference : ScriptableSingleton<AssetShelfPreference>
    {
        public void Save()
        {
            Save(true);
        }
    }
}
