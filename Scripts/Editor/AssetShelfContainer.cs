using UnityEngine;

namespace AssetShelf
{
    public abstract class AssetShelfContainer : ScriptableObject
    {
        public abstract int GetContentGroupCount();

        public abstract string GetContentGroupName(int index);

        public abstract AssetShelfContentGroup GetContentGroupWithoutPreview(int index);
    }
}
