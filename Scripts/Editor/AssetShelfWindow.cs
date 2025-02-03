using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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

        private static string ContainerGuidUserSettingsKey = "AssetShelfWindow.Container";
        private static string SelectedGroupIndexUserSettingsKey = "AssetShelfWindow.SelectedGroupIndex";
        private static string PreviewItemSizeUserSettingsKey = "AssetShelfWindow.PreviewItemSize";
        private static string TreeViewStateUserSettingsKey = "AssetShelfWindow.TreeViewState";

        private AssetShelfContainer _container;

        private int _contentGroupCount;

        private string[] _contentGroupNames = new string[0];

        private AssetShelfContentGroup[] _contentGroups = new AssetShelfContentGroup[0];

        private AssetShelfContentDirectoryAnalyzer[] _directoryAnalyzers = new AssetShelfContentDirectoryAnalyzer[0];

        private bool _updateContentsRequired;

        private int _selectedGroupIndex = 0;

        private string _selectedPath = "";

        private bool _filteredContentsGenerated;

        private List<AssetShelfContent> _filteredContents = new List<AssetShelfContent>();

        private Vector2 _assetViewScrollPosition;

        private float _previewItemSize = 100;

        private bool _isLoadingPreviews;
        private int _loadingStart;
        private int _loadingEnd;

        private bool _showDebugView;

        private TreeViewState _treeViewState;

        private AssetShelfTreeView _treeView;
        private bool _treeViewAvailable;

        private SelectionWithoutPing _selectionWithoutPing;

        private void OnEnable()
        {
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;
            if (_container == null)
            {
                var containerGuid = EditorUserSettings.GetConfigValue(ContainerGuidUserSettingsKey);
                if (!string.IsNullOrEmpty(containerGuid))
                {
                    _container = AssetDatabase.LoadAssetAtPath<AssetShelfContainer>(AssetDatabase.GUIDToAssetPath(containerGuid));
                    _updateContentsRequired = true;
                }

                try
                {
                    _previewItemSize = float.Parse(EditorUserSettings.GetConfigValue(PreviewItemSizeUserSettingsKey));
                }
                catch (System.Exception)
                {
                    _previewItemSize = 100;
                }

                try
                {
                    _selectedGroupIndex = int.Parse(EditorUserSettings.GetConfigValue(SelectedGroupIndexUserSettingsKey));
                }
                catch (System.Exception)
                {
                    _selectedGroupIndex = 0;
                }
            }

            if (_treeView == null)
            {
                try
                {
                    var json = EditorUserSettings.GetConfigValue(TreeViewStateUserSettingsKey);
                    if (!string.IsNullOrEmpty(json))
                    {
                        _treeViewState = JsonUtility.FromJson<TreeViewState>(json);
                    }
                }
                catch (System.Exception)
                {
                    _treeViewState = new TreeViewState();
                }

                _treeView = new AssetShelfTreeView(_treeViewState);
                _treeViewAvailable = false;
                _updateContentsRequired = true;
            }

            if (_selectionWithoutPing == null)
            {
                _selectionWithoutPing = new SelectionWithoutPing();
            }
        }

        private void OnDisable()
        {
            ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
            if (_treeViewState != null)
            {
                try
                {
                    var json = JsonUtility.ToJson(_treeViewState);
                    EditorUserSettings.SetConfigValue(TreeViewStateUserSettingsKey, json);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }

            _selectionWithoutPing.Dispose();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Show Debug View"), _showDebugView, () => _showDebugView = !_showDebugView);
        }

        private void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            ChangeAssetObjectPropertiesEventArgs data;
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    stream.GetChangeAssetObjectPropertiesEvent(i, out data);
                    if (data.instanceId == _container.GetInstanceID())
                    {
                        _updateContentsRequired = true;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (_isLoadingPreviews)
            {
                var hasLoading = false;
                for (int i = _loadingStart; i < _loadingEnd && i < _filteredContents.Count; i++)
                {
                    var content = _filteredContents[i];
                    if (content != null && content.Asset != null && AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
                    {
                        hasLoading = true;
                        break;
                    }
                }
                if (!hasLoading)
                {
                    _isLoadingPreviews = false;
                    Repaint();
                    AssetShelfLog.RepaintCallCount++;
                }
            }

            AssetShelfContent.ResetLoadAssetCount();
            if (AssetShelfContent.IsLimitted)
            {
                Repaint();
                AssetShelfLog.RepaintCallCount++;
            }

            _selectionWithoutPing.Update();
        }

        public void OnGUI()
        {
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
                    _treeViewAvailable = false;
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
                    _treeViewAvailable = false;
                }
            }

            if (!_treeViewAvailable)
            {
                _treeView.Setup(_container);
                _treeView.Reload();
                _treeViewAvailable = true;
            }

            const float singleLineHeight = 18;
            const float headerHeight = singleLineHeight * 1 + 4;
            const float sidebarWidth = 200;
            const float debugViewHeight = singleLineHeight * 4;
            const float footerHeight = singleLineHeight * 1;

            // Header
            var headerRect = new Rect(0, 0, position.width, headerHeight);
            GUI.Box(headerRect, GUIContent.none);
            using (new GUILayout.AreaScope(headerRect))
            {
                DrawHeaderLayout();
            }

            // Sidebar and debug view
            var actualDebugViewHeight = _showDebugView ? debugViewHeight : 0;

            var sidebarRect = new Rect(0, headerHeight, sidebarWidth, position.height - headerHeight - actualDebugViewHeight);
            DrawTreeView(sidebarRect);

            if (_showDebugView)
            {
                var debugViewRect = new Rect(0, position.height - actualDebugViewHeight, sidebarWidth, actualDebugViewHeight);
                GUI.Box(debugViewRect, GUIContent.none);
                using (new GUILayout.AreaScope(debugViewRect))
                {
                    DrawDebugViewLayout();
                }
            }

            // Asset view
            var assetViewRect = new Rect(sidebarWidth, headerHeight, position.width - sidebarWidth, position.height - headerHeight - footerHeight);
            DrawAssetView(assetViewRect);

            // Footer
            var footerRect = new Rect(sidebarWidth, position.height - footerHeight, position.width - sidebarWidth, footerHeight);
            GUI.Box(footerRect, GUIContent.none);
            using (new GUILayout.AreaScope(footerRect))
            {
                DrawFooterLayout();
            }

            // Lines
            var lineColor = new Color(0.1f, 0.1f, 0.1f, 1);
            AssetShelfGUI.HorizontalLine(new Vector2(0, headerHeight), position.width, 1, lineColor);
            AssetShelfGUI.VerticalLine(new Vector2(sidebarWidth, headerHeight), position.height - headerHeight, 2, lineColor);
            AssetShelfGUI.HorizontalLine(new Vector2(sidebarWidth, position.height - footerHeight), position.width - sidebarWidth, 1, lineColor);

            // Resize preview chashe size
            if (AssetShelfLog.LastDrawPreviewCount * 2 > 128)
            {
                AssetPreview.SetPreviewTextureCacheSize(AssetShelfLog.LastDrawPreviewCount * 2);
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
                        AssetShelfLog.RepaintCallCount++;
                    }
                }

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    _container = EditorGUILayout.ObjectField(_container, typeof(AssetShelfContainer), false) as AssetShelfContainer;
                    if (changeCheck.changed)
                    {
                        _updateContentsRequired = true;
                        _selectedAsset = null;
                        if (_container != null)
                        {
                            var containerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_container));
                            EditorUserSettings.SetConfigValue(ContainerGuidUserSettingsKey, containerGuid);
                        }
                        Repaint();
                        AssetShelfLog.RepaintCallCount++;
                    }
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUILayout.Width(40)))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create Asset Shelf Settings", "AssetShelfSettings", "asset", "Create a new Asset Shelf Settings.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        _container = CreateInstance<AssetShelfSettings>();
                        AssetDatabase.CreateAsset(_container, path);
                        var containerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_container));
                        EditorUserSettings.SetConfigValue(ContainerGuidUserSettingsKey, containerGuid);
                        _updateContentsRequired = true;
                        Repaint();
                        AssetShelfLog.RepaintCallCount++;
                    }
                }
            }
        }

        private void DrawTreeView(Rect rect)
        {
            if (!_treeViewAvailable)
            {
                return;
            }

            _treeView.OnGUI(rect);

            var oldSelectedGroupIndex = _selectedGroupIndex;
            var oldSelectedPath = _selectedPath;
            _treeView.GetSelectedContent(out _selectedGroupIndex, out _selectedPath);
            if (_selectedGroupIndex < 0)
            {
                _selectedGroupIndex = 0;
                _selectedPath = string.Empty;
            }

            if (oldSelectedGroupIndex != _selectedGroupIndex || oldSelectedPath != _selectedPath)
            {
                _assetViewScrollPosition = Vector2.zero;
                _selectedAsset = null;
            }

            if (oldSelectedGroupIndex != _selectedGroupIndex || oldSelectedPath != _selectedPath || !_filteredContentsGenerated)
            {
                EditorUserSettings.SetConfigValue(SelectedGroupIndexUserSettingsKey, _selectedGroupIndex.ToString());
                _filteredContentsGenerated = true;
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

        private void DrawDebugViewLayout()
        {
            if (GUILayout.Button("Clear"))
            {
                AssetShelfLog.Clear();
            }
            GUILayout.Label($"Draw preview: {AssetShelfLog.LastDrawPreviewCount}");
            GUILayout.Label($"Repaint call count: {AssetShelfLog.RepaintCallCount}");
        }

        private Object _selectedAsset = null;
        private Object _grabbedAsset = null;
        private bool _dragStarted;
        private void DrawAssetView(Rect rect)
        {
            var contents = _filteredContents;
            var itemSize = _previewItemSize;
            var spacing = new Vector2(5, 5);
            var scrollbarWidth = 15;

            // Draw grid view
            var viewHeight = AssetShelfGUI.GetGridViewHeight(itemSize, spacing, rect.width - scrollbarWidth, contents.Count);
            var viewRect = new Rect(0, 0, rect.width - scrollbarWidth, viewHeight);
            using (var scrollView = new GUI.ScrollViewScope(rect, _assetViewScrollPosition, viewRect))
            {
                _assetViewScrollPosition = scrollView.scrollPosition;
                var columnCount = AssetShelfGUI.GetGridColumnCount(itemSize, spacing.x, viewRect.width);
                var startRow = Mathf.FloorToInt(_assetViewScrollPosition.y / (itemSize + spacing.y));
                var startIndex = startRow * columnCount;
                var endRow = Mathf.CeilToInt((_assetViewScrollPosition.y + rect.height) / (itemSize + spacing.y));
                var endIndex = endRow * columnCount;
                endIndex = Mathf.Min(endIndex, contents.Count);
                AssetShelfUtility.LoadPreviewsIfNeeded(contents, startIndex, endIndex);
                _isLoadingPreviews = contents.Skip(startIndex).Take(endIndex - startIndex).Any(c => c.Preview == null);
                _loadingStart = startIndex;
                _loadingEnd = endIndex;
                AssetShelfGUI.DrawGridItems(viewRect, itemSize, spacing, contents, startIndex, endIndex, _selectedAsset);
            }

            // Select in Asset Shelf
            if (Event.current.type == EventType.MouseDown)
            {
                var gridViewMousePosition = Event.current.mousePosition - rect.position + _assetViewScrollPosition;
                var selectedIndex = AssetShelfGUI.GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
                if (selectedIndex >= 0
                    && selectedIndex < contents.Count
                    && rect.Contains(Event.current.mousePosition))
                {
                    _selectedAsset = contents[selectedIndex].Asset;
                    _grabbedAsset = contents[selectedIndex].Asset;
                    Repaint();
                    AssetShelfLog.RepaintCallCount++;
                }
                else
                {
                    _grabbedAsset = null;
                }
            }

            // Select in Unity
            if (Event.current.type == EventType.MouseUp)
            {
                var gridViewMousePosition = Event.current.mousePosition - rect.position + _assetViewScrollPosition;
                var selectedIndex = AssetShelfGUI.GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
                if (selectedIndex >= 0
                    && selectedIndex < contents.Count
                    && rect.Contains(Event.current.mousePosition)
                    && contents[selectedIndex].Asset == _selectedAsset)
                {
                    _selectionWithoutPing.Select(_selectedAsset);
                }
            }

            // Select in Unity by drop myself
            if (Event.current.type == EventType.DragUpdated && _grabbedAsset != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }

            if (Event.current.type == EventType.DragPerform)
            {
                if (DragAndDrop.objectReferences.Length == 1 && DragAndDrop.objectReferences[0] == _grabbedAsset)
                {
                    DragAndDrop.AcceptDrag();
                    _selectionWithoutPing.Select(_selectedAsset);
                }
            }

            // Drag and drop
            if (Event.current.type == EventType.MouseDrag)
            {
                if (!_dragStarted)
                {
                    var gridViewMousePosition = Event.current.mousePosition - rect.position + _assetViewScrollPosition;
                    var selectedIndex = AssetShelfGUI.GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
                    if (selectedIndex >= 0
                        && selectedIndex < contents.Count
                        && rect.Contains(Event.current.mousePosition)
                        && _grabbedAsset != null
                        && contents[selectedIndex].Asset == _grabbedAsset)
                    {
                        _dragStarted = true;
                    }
                    else
                    {
                        _grabbedAsset = null;
                    }
                }

                if (_grabbedAsset == null)
                {
                    _dragStarted = false;
                }

                if (_dragStarted && !_selectionWithoutPing.IsRunning)
                {
                    _dragStarted = false;

                    // Clear drag data
                    DragAndDrop.PrepareStartDrag();

                    // Set up drag data
                    DragAndDrop.objectReferences = new Object[] { _grabbedAsset };

                    // Start drag
                    DragAndDrop.StartDrag($"Dragging Asset: {_grabbedAsset.name}");

                    // Make sure no one uses the event after us
                    Event.current.Use();
                }
            }
        }

        private void DrawFooterLayout()
        {
            using (new GUILayout.HorizontalScope())
            {
                var label = _selectedAsset == null ? string.Empty : _selectedAsset.name;
                GUILayout.Label(label);
                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    _previewItemSize = GUILayout.HorizontalSlider(_previewItemSize, 64, 256, GUILayout.MaxWidth(200));
                    if (changeCheck.changed)
                    {
                        EditorUserSettings.SetConfigValue(PreviewItemSizeUserSettingsKey, _previewItemSize.ToString());
                    }
                }

                GUILayout.Space(20);
            }
        }
    }
}
