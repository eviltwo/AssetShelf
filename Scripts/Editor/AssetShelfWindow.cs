using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfWindow : EditorWindow, IHasCustomMenu
    {
        [MenuItem("Window/Asset Shelf")]
        private static void Open()
        {
            var window = GetWindow<AssetShelfWindow>();
            window.titleContent = new GUIContent("Asset Shelf");
            window.Show();
        }

        private AssetShelfContainer _container;

        private int _lastContainerVersion;

        private int _contentGroupCount;

        private string[] _contentGroupNames = new string[0];

        private AssetShelfContentGroup[] _contentGroups = new AssetShelfContentGroup[0];

        private AssetShelfContentDirectoryAnalyzer[] _directoryAnalyzers = new AssetShelfContentDirectoryAnalyzer[0];

        private bool _updateContentsRequired;

        private List<(int group, string path)> _foldoutPaths = new List<(int group, string path)>();

        private int _selectedGroupIndex = 0;

        private string _selectedPath = "";

        private bool _filteredContentsGenerated;

        private List<AssetShelfContent> _filteredContents = new List<AssetShelfContent>();

        private Vector2 _scrollPosition;

        private List<Object> _waitingPreviews = new List<Object>();

        private bool _showDebugView;

        private void OnEnable()
        {
            _updateContentsRequired = true;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Show Debug View"), _showDebugView, () => _showDebugView = !_showDebugView);
        }

        private void Update()
        {
            var repaintRequired = false;
            for (int i = _waitingPreviews.Count - 1; i >= 0; i--)
            {
                var asset = _waitingPreviews[i];
                if (asset == null)
                {
                    _waitingPreviews.RemoveAt(i);
                    continue;
                }
                if (!AssetPreview.IsLoadingAssetPreview(asset.GetInstanceID()))
                {
                    _waitingPreviews.RemoveAt(i);
                    repaintRequired = true;
                    continue;
                }
            }
            if (repaintRequired)
            {
                Repaint();
                AssetShelfLog.RepaintCallCount++;
            }
        }

        public void OnGUI()
        {
            if (_container != null && _lastContainerVersion != _container.PropertyVersion)
            {
                _updateContentsRequired = true;
            }

            if (_updateContentsRequired)
            {
                _updateContentsRequired = false;
                _filteredContentsGenerated = false;
                _filteredContents.Clear();
                if (_container == null)
                {
                    _contentGroupCount = 0;
                    _contentGroupNames = new string[0];
                    _contentGroups = new AssetShelfContentGroup[0];
                    _directoryAnalyzers = new AssetShelfContentDirectoryAnalyzer[0];
                }
                else
                {
                    _contentGroupCount = _container.GetContentGroupCount();
                    _contentGroupNames = new string[_contentGroupCount];
                    _directoryAnalyzers = new AssetShelfContentDirectoryAnalyzer[_contentGroupCount];
                    for (int i = 0; i < _contentGroupCount; i++)
                    {
                        _contentGroupNames[i] = _container.GetContentGroupName(i);
                    }
                    _contentGroups = new AssetShelfContentGroup[_contentGroupCount];
                    _lastContainerVersion = _container.PropertyVersion;
                }
            }

            var headerRect = new Rect(0, 0, position.width, 40);
            GUI.Box(headerRect, GUIContent.none);
            using (new GUILayout.AreaScope(headerRect))
            {
                DrawHeaderLayout();
            }

            var debugViewHeight = _showDebugView ? EditorGUIUtility.singleLineHeight * 4 : 0;

            var sidebarRect = new Rect(0, headerRect.height, 200, position.height - headerRect.height - debugViewHeight);
            GUI.Box(sidebarRect, GUIContent.none);
            using (new GUILayout.AreaScope(sidebarRect))
            {
                DrawSidebarLayout();
            }

            if (_showDebugView)
            {
                var debugViewRect = new Rect(0, position.height - debugViewHeight, sidebarRect.width, debugViewHeight);
                GUI.Box(debugViewRect, GUIContent.none);
                using (new GUILayout.AreaScope(debugViewRect))
                {
                    DrawDebugViewLayout();
                }
            }

            var assetViewRect = new Rect(sidebarRect.width, headerRect.height, position.width - sidebarRect.width, position.height - headerRect.height);
            DrawAssetView(assetViewRect);
            if (AssetShelfLog.LastDrawPreviewCount > 128)
            {
                AssetPreview.SetPreviewTextureCacheSize(AssetShelfLog.LastDrawPreviewCount);
            }
        }

        private void LoadContentGroupIfNull(int index)
        {
            if (_contentGroups[index] == null)
            {
                _contentGroups[index] = _container.GetContentGroupWithoutPreview(index);
            }
        }

        private void DrawHeaderLayout()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (_container != null)
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(40)))
                    {
                        _updateContentsRequired = true;
                        Repaint();
                    }
                }

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    _container = EditorGUILayout.ObjectField(_container, typeof(AssetShelfContainer), false) as AssetShelfContainer;
                    if (changeCheck.changed)
                    {
                        _updateContentsRequired = true;
                        Repaint();
                    }
                }
            }
        }

        private void DrawSidebarLayout()
        {
            if (_contentGroups == null || _contentGroups.Length == 0)
            {
                return;
            }

            var oldSelectedGroupIndex = _selectedGroupIndex;
            var oldSelectedPath = _selectedPath;
            for (int i = 0; i < _contentGroupCount; i++)
            {
                var contentGroupName = _contentGroupNames[i];
                var selected = i == _selectedGroupIndex && string.IsNullOrEmpty(_selectedPath);
                var groupFoldoutIndex = _foldoutPaths.FindIndex(v => v.group == i && string.IsNullOrEmpty(v.path));
                var prevFoldout = groupFoldoutIndex >= 0;
                var currentFoldout = prevFoldout;
                if (AssetShelfGUILayout.FoldoutSelectButton(selected, contentGroupName, ref currentFoldout))
                {
                    _selectedGroupIndex = i;
                    _selectedPath = "";
                }
                if (!prevFoldout && currentFoldout)
                {
                    _foldoutPaths.Add((i, ""));
                }
                else if (prevFoldout && !currentFoldout)
                {
                    _foldoutPaths.RemoveAt(groupFoldoutIndex);
                }
                if (currentFoldout)
                {
                    if (_directoryAnalyzers[i] == null)
                    {
                        LoadContentGroupIfNull(i);
                        _directoryAnalyzers[i] = new AssetShelfContentDirectoryAnalyzer(_contentGroups[i]);
                    }

                    DrawInnerDirectories(_directoryAnalyzers[i].Root, _foldoutPaths, i);
                }
            }

            if (oldSelectedGroupIndex != _selectedGroupIndex || oldSelectedPath != _selectedPath || !_filteredContentsGenerated)
            {
                _filteredContentsGenerated = true;
                _scrollPosition = Vector2.zero;
                LoadContentGroupIfNull(_selectedGroupIndex);
                _filteredContents.Clear();
                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    var selectedGroup = _contentGroups[_selectedGroupIndex];
                    _filteredContents.AddRange(selectedGroup.Contents.Where(c => AssetShelfUtility.HasDirectory(c.Path, _selectedPath)));
                }
                else
                {
                    _filteredContents.AddRange(_contentGroups[_selectedGroupIndex].Contents);
                }
            }
        }

        private void DrawInnerDirectories(AssetShelfContentDirectory directory, List<(int group, string path)> foldoutPaths, int group)
        {
            var childDirectories = directory.Childs;
            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < childDirectories.Count; i++)
                {
                    var child = childDirectories[i];
                    var selected = group == _selectedGroupIndex && child.Path == _selectedPath;
                    if (child.Childs.Count > 0)
                    {
                        var foldoutIndex = foldoutPaths.FindIndex(v => v.group == group && v.path == child.Path);
                        var prevFoldout = foldoutIndex >= 0;
                        var currentFoldout = prevFoldout;
                        if (AssetShelfGUILayout.FoldoutSelectButton(selected, child.ShortName, ref currentFoldout))
                        {
                            _selectedGroupIndex = group;
                            _selectedPath = child.Path;
                        }
                        if (!prevFoldout && currentFoldout)
                        {
                            foldoutPaths.Add((group, child.Path));
                        }
                        else if (prevFoldout && !currentFoldout)
                        {
                            foldoutPaths.RemoveAt(foldoutIndex);
                        }
                        if (currentFoldout)
                        {
                            DrawInnerDirectories(child, foldoutPaths, group);
                        }
                    }
                    else
                    {
                        if (AssetShelfGUILayout.SelectButton(selected, child.ShortName))
                        {
                            _selectedGroupIndex = group;
                            _selectedPath = child.Path;
                        }
                    }
                }
            }
        }

        private void DrawDebugViewLayout()
        {
            if (GUILayout.Button("Clear"))
            {
                AssetShelfLog.Clear();
            }
            GUILayout.Label($"Load preview total: {AssetShelfLog.LoadPreviewTotalCount}");
            GUILayout.Label($"Last draw preview: {AssetShelfLog.LastDrawPreviewCount}");
            GUILayout.Label($"Repaint call count: {AssetShelfLog.RepaintCallCount}");
        }

        private void DrawAssetView(Rect rect)
        {
            var contents = _filteredContents;
            var itemSize = 64;
            var spacing = new Vector2(5, 5);
            var scrollbarWidth = 15;
            var viewHeight = AssetShelfGUI.GetGridViewHeight(itemSize, spacing, rect.width - scrollbarWidth, contents.Count);
            var viewRect = new Rect(0, 0, rect.width - scrollbarWidth, viewHeight);
            using (var scrollView = new GUI.ScrollViewScope(rect, _scrollPosition, viewRect))
            {
                _scrollPosition = scrollView.scrollPosition;
                var columnCount = AssetShelfGUI.GetGridColumnCount(itemSize, spacing.x, viewRect.width);
                var startRow = Mathf.FloorToInt(_scrollPosition.y / (itemSize + spacing.y));
                var startIndex = startRow * columnCount;
                var endRow = Mathf.CeilToInt((_scrollPosition.y + rect.height) / (itemSize + spacing.y));
                var endIndex = endRow * columnCount;
                endIndex = Mathf.Min(endIndex, contents.Count);
                AssetShelfUtility.LoadPreviewsIfNeeded(contents, startIndex, endIndex);
                var isLoadingPreview = contents.Skip(startIndex).Take(endIndex - startIndex).Any(c => c.Preview == null && !c.SkipPreview);
                for (int i = startIndex; i < endIndex; i++)
                {
                    var content = contents[i];
                    if (content != null && content.Preview == null && !content.SkipPreview)
                    {
                        if (!_waitingPreviews.Contains(content.Asset))
                        {
                            _waitingPreviews.Add(content.Asset);
                        }
                    }
                }
                AssetShelfGUI.DrawGridItems(viewRect, itemSize, spacing, contents, startIndex, endIndex);
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                var gridViewMousePosition = Event.current.mousePosition - rect.position + _scrollPosition;
                var selectedIndex = AssetShelfGUI.GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
                if (selectedIndex >= 0 && selectedIndex < contents.Count)
                {
                    var selectedAsset = contents[selectedIndex].Asset;
                    if (selectedAsset != null)
                    {
                        // Clear drag data
                        DragAndDrop.PrepareStartDrag();

                        // Set up drag data
                        DragAndDrop.objectReferences = new Object[] { selectedAsset };

                        // Start drag
                        DragAndDrop.StartDrag($"Dragging Asset: {selectedAsset.name}");

                        // Make sure no one uses the event after us
                        Event.current.Use();
                    }
                }
            }
        }
    }
}
