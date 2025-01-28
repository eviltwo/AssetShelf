using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public abstract class AssetShelfContainer : ScriptableObject
    {
        public abstract void CollectContentGroupsWithoutPreview(List<AssetShelfContentGroup> accumulatedResults);
    }
}
