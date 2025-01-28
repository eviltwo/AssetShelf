using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    [CreateAssetMenu(fileName = "AssetShelfSettings", menuName = "AssetShelf/Settings")]
    public class AssetShelfSettings : AssetShelfContainer
    {
        public AssetShelfRule[] Rules = new AssetShelfRule[]
        {
            new AssetShelfRule
            {
                Name = "Source 1",
                Folders = new Object[0],
                SearchFilter = "t:GameObject"
            }
        };

        public override int GetContentGroupCount()
        {
            return Rules.Length;
        }

        public override string GetContentGroupName(int index)
        {
            return Rules[index].Name;
        }

        public override AssetShelfContentGroup GetContentGroupWithoutPreview(int index)
        {
            var rule = Rules[index];
            var contentGroup = new AssetShelfContentGroup
            {
                Name = rule.Name
            };
            foreach (var source in rule.Folders)
            {
                if (source == null)
                {
                    continue;
                }
                var sourcePath = AssetDatabase.GetAssetPath(source);
                if (string.IsNullOrEmpty(sourcePath))
                {
                    continue;
                }
                if (source is DefaultAsset defaultAsset && AssetDatabase.IsValidFolder(sourcePath))
                {
                    CollectContentsWithoutPreviewFromFolder(sourcePath, rule.SearchFilter, contentGroup.Contents);
                }
            }
            return contentGroup;
        }

        private static void CollectContentsWithoutPreviewFromFolder(string folderPath, string searchFilter, List<AssetShelfContent> accumulatedResults)
        {
            var assetPaths = AssetDatabase.FindAssets(searchFilter, new string[] { folderPath });
            foreach (var assetPath in assetPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(assetPath));
                if (asset == null)
                {
                    continue;
                }
                var content = new AssetShelfContent
                {
                    Asset = asset,
                    Path = AssetDatabase.GetAssetPath(asset)
                };
                accumulatedResults.Add(content);
            }
        }
    }
}
