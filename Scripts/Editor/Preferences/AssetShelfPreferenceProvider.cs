using UnityEditor;

namespace AssetShelf
{
    public class AssetShelfPreferenceProvider : SettingsProvider
    {
        private static readonly string SettingPath = "Preferences/Asset Shelf";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new AssetShelfPreferenceProvider(SettingPath, SettingsScope.User);
        }

        public AssetShelfPreferenceProvider(string path, SettingsScope scopes, string[] keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnGUI(string searchContext)
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var preference = AssetShelfPreference.instance;

                // TODO: Draw fields

                if (check.changed)
                {
                    preference.Save();
                }
            }
        }
    }
}
