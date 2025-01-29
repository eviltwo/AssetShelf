using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;

namespace AssetShelf
{
    public class AssetShelfTreeView : TreeView
    {
        private AssetShelfContainer _container;
        private int _groupCount;
        private string[] _groupNames;
        private AssetShelfContentGroup[] _groups;
        private AssetShelfContentDirectoryAnalyzer[] _analyzers;

        public AssetShelfTreeView(TreeViewState state) : base(state)
        {
        }

        public void Setup(AssetShelfContainer container)
        {
            _container = container;
            _groupCount = _container.GetContentGroupCount();
            _groupNames = new string[_groupCount];
            _groups = new AssetShelfContentGroup[_groupCount];
            _analyzers = new AssetShelfContentDirectoryAnalyzer[_groupCount];
            for (int i = 0; i < _groupCount; i++)
            {
                _groupNames[i] = _container.GetContentGroupName(i);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = GetRows() ?? new List<TreeViewItem>();
            rows.Clear();

            for (int i = 0; i < _groupCount; i++)
            {
                var topName = _groupNames[i];
                var topItem = new TreeViewItem { id = i, displayName = topName };
                rows.Add(topItem);
                root.AddChild(topItem);
                if (IsExpanded(topItem.id))
                {
                    LoadContentGroupIfNull(i);
                    if (_analyzers[i] == null)
                    {
                        _analyzers[i] = new AssetShelfContentDirectoryAnalyzer(_groups[i]);
                    }
                    AddChilds(topItem, _analyzers[i].Root, rows);
                }
                else
                {
                    topItem.children = CreateChildListForCollapsedParent();
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }

        private void AddChilds(TreeViewItem parentItem, AssetShelfContentDirectory parentDirectory, IList<TreeViewItem> rows)
        {
            foreach (var child in parentDirectory.Childs)
            {
                var childItemId = child.Path.GetHashCode();
                var childItem = new TreeViewItem { id = childItemId, displayName = child.ShortName };
                rows.Add(childItem);
                parentItem.AddChild(childItem);
                if (IsExpanded(childItemId))
                {
                    AddChilds(childItem, child, rows);
                }
                else
                {
                    childItem.children = child.Childs.Count > 0 ? CreateChildListForCollapsedParent() : null;
                }
            }
        }

        private void LoadContentGroupIfNull(int index)
        {
            if (_groups[index] == null)
            {
                _groups[index] = _container.GetContentGroupWithoutPreview(index);
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void GetSelectedContent(out int index, out string path)
        {
            if (!HasSelection())
            {
                index = -1;
                path = string.Empty;
                return;
            }

            var selectedId = GetSelection().FirstOrDefault();
            if (selectedId >= 0 && selectedId < _groupCount)
            {
                index = selectedId;
                path = string.Empty;
                return;
            }

            for (int i = 0; i < _groupCount; i++)
            {
                if (_analyzers[i] == null)
                {
                    continue;
                }

                var foundPath = FindSelectedPath(selectedId, _analyzers[i].Root);
                if (!string.IsNullOrEmpty(foundPath))
                {
                    index = i;
                    path = foundPath;
                    return;
                }
            }

            index = -1;
            path = string.Empty;
        }

        private string FindSelectedPath(int itemId, AssetShelfContentDirectory directory)
        {
            foreach (var child in directory.Childs)
            {
                var childItemId = child.Path.GetHashCode();
                if (childItemId == itemId)
                {
                    return child.Path;
                }

                var path = FindSelectedPath(itemId, child);
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
