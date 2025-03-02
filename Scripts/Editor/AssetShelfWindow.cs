using System.Collections.Generic;
using System.IO;
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
        private static string PreviewItemSizeUserSettingsKey = "AssetShelfWindow.PreviewItemSize";
        private static string SelectedGroupIndexUserSettingsKey = "AssetShelfWindow.SelectedGroupIndex";
        private static string TreeViewStateUserSettingsKey = "AssetShelfWindow.TreeViewState";

        private class BuiltinResources
        {
            public GUIContent RefreshIcon;
            public GUIContent PlusIcon;
            public void Load()
            {
                RefreshIcon = EditorGUIUtility.IconContent("d_Refresh");
                PlusIcon = EditorGUIUtility.IconContent("d_Toolbar Plus");
            }
        }

        private BuiltinResources _builtinResources;

        private AssetShelfContainerController _controller;

        private AssetShelfTreeView _treeView;

        private TreeViewState _treeViewState;

        private bool _isTreeViewInitialized;

        private bool _isDataReloadRequired;

        private int _selectedGroupIndex = 0;

        private string _selectedPath = string.Empty;

        private SearchField _searchField;

        private string _searchText = string.Empty;

        private string _searchTextFilterUsed = string.Empty;

        private bool _filteredContentsGenerated;

        private List<AssetShelfContent> _filteredContents = new List<AssetShelfContent>();

        private float _previewItemSize = 100;

        private float _previewNameSize = 20;

        private bool _isLoadingPreviews;

        private GridView _gridView;

        private bool _showDebugView;

        private SelectionWithoutPing _selectionWithoutPing;

        private bool _resetUserDataAndClose;

        private void OnEnable()
        {
            ObjectChangeEvents.changesPublished += OnObjectChangesPublished;

            // TreeView
            try
            {
                var json = EditorUserSettings.GetConfigValue(TreeViewStateUserSettingsKey);
                if (string.IsNullOrEmpty(json))
                {
                    _treeViewState = new TreeViewState();
                }
                else
                {
                    _treeViewState = JsonUtility.FromJson<TreeViewState>(json);
                }
            }
            catch (System.Exception)
            {
                _treeViewState = new TreeViewState();
            }

            _treeView = new AssetShelfTreeView(_treeViewState);

            // Controller
            var containerGuid = EditorUserSettings.GetConfigValue(ContainerGuidUserSettingsKey);
            if (!string.IsNullOrEmpty(containerGuid))
            {
                var container = AssetDatabase.LoadAssetAtPath<AssetShelfContainer>(AssetDatabase.GUIDToAssetPath(containerGuid));
                if (container != null)
                {
                    _controller = new AssetShelfContainerController(container);
                    _isDataReloadRequired = true;
                }
            }

            // Preview item size
            try
            {
                _previewItemSize = float.Parse(EditorUserSettings.GetConfigValue(PreviewItemSizeUserSettingsKey));
            }
            catch (System.Exception)
            {
                _previewItemSize = 100;
            }

            // Selection
            try
            {
                _selectedGroupIndex = int.Parse(EditorUserSettings.GetConfigValue(SelectedGroupIndexUserSettingsKey));
            }
            catch (System.Exception)
            {
                _selectedGroupIndex = 0;
            }

            _gridView = new GridView();
            _selectionWithoutPing = new SelectionWithoutPing();
            _searchField = new SearchField();
            _builtinResources = new BuiltinResources();
            _builtinResources.Load();
        }

        private void OnDisable()
        {
            if (_treeViewState != null && !_resetUserDataAndClose)
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

            ObjectChangeEvents.changesPublished -= OnObjectChangesPublished;
            _controller = null;
            _isTreeViewInitialized = false;
            _treeView = null;
            _treeViewState = null;
            _selectionWithoutPing.Dispose();
            PreviewCache.ReleaseResources();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Show Debug View"), _showDebugView, () => _showDebugView = !_showDebugView);
            menu.AddItem(new GUIContent("Reset User Data"), false, () =>
            {
                EditorUserSettings.SetConfigValue(ContainerGuidUserSettingsKey, null);
                EditorUserSettings.SetConfigValue(SelectedGroupIndexUserSettingsKey, null);
                EditorUserSettings.SetConfigValue(PreviewItemSizeUserSettingsKey, null);
                EditorUserSettings.SetConfigValue(TreeViewStateUserSettingsKey, null);
                _resetUserDataAndClose = true;
                Close();
            });
        }

        private void OnObjectChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (_controller == null)
            {
                return;
            }

            ChangeAssetObjectPropertiesEventArgs data;
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    stream.GetChangeAssetObjectPropertiesEvent(i, out data);
                    if (data.instanceId == _controller.ContainerInstanceID)
                    {
                        _isDataReloadRequired = true;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            // Check end of loading previews
            if (_isLoadingPreviews && _gridView != null)
            {
                var hasLoading = false;
                for (int i = 0; i < _gridView.LastDrawResultItemCount; i++)
                {
                    var itemIndex = _gridView.LastDrawResultResultIndex + i;
                    if (itemIndex < 0 || itemIndex >= _filteredContents.Count)
                    {
                        continue;
                    }

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

            // Reset loading count and repaint
            if (AssetShelfContent.IsLimitted)
            {
                Repaint();
                AssetShelfLog.RepaintCallCount++;
            }
            AssetShelfContent.ResetLoadAssetCount();

            // Selection
            _selectionWithoutPing?.Update();
        }

        public void OnGUI()
        {
            if (_isDataReloadRequired)
            {
                _isDataReloadRequired = false;

                if (_controller != null)
                {
                    _controller.Initialize();
                    _treeView.Setup(_controller.Container);
                    _treeView.Reload();
                    _isTreeViewInitialized = true;
                }

                _filteredContentsGenerated = false;
                _filteredContents.Clear();
            }

            var headerHeight = EditorGUIUtility.singleLineHeight * 1 + 4;
            var sidebarWidth = 200;
            var debugViewHeight = EditorGUIUtility.singleLineHeight * 4;
            var footerHeight = EditorGUIUtility.singleLineHeight * 1;

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
                PreviewCache.SetCacheSize(AssetShelfLog.LastDrawPreviewCount * 8);
            }
        }

        private void DrawHeaderLayout()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_controller == null))
                {
                    if (GUILayout.Button(_builtinResources.RefreshIcon, GUILayout.Width(40)))
                    {
                        _isDataReloadRequired = true;
                        Repaint();
                        AssetShelfLog.RepaintCallCount++;
                    }
                }

                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    var container = EditorGUILayout.ObjectField(_controller?.Container, typeof(AssetShelfContainer), false) as AssetShelfContainer;
                    if (changeCheck.changed)
                    {
                        _selectedAsset = null;
                        if (container == null)
                        {
                            _controller = null;
                        }
                        else
                        {
                            _controller = new AssetShelfContainerController(container);
                            var containerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(container));
                            EditorUserSettings.SetConfigValue(ContainerGuidUserSettingsKey, containerGuid);
                            _isDataReloadRequired = true;
                            _isTreeViewInitialized = false;
                        }

                        Repaint();
                        AssetShelfLog.RepaintCallCount++;
                    }
                }

                if (GUILayout.Button(_builtinResources.PlusIcon, GUILayout.Width(40)))
                {
                    var path = EditorUtility.SaveFilePanelInProject("Create Asset Shelf Settings", "AssetShelfSettings", "asset", "Create a new Asset Shelf Settings.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var container = CreateInstance<AssetShelfSettings>();
                        AssetDatabase.CreateAsset(container, path);
                        _controller = new AssetShelfContainerController(container);
                        var containerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(container));
                        EditorUserSettings.SetConfigValue(ContainerGuidUserSettingsKey, containerGuid);
                        _isDataReloadRequired = true;
                        _isTreeViewInitialized = false;

                        Repaint();
                        AssetShelfLog.RepaintCallCount++;
                    }
                }
            }
        }

        private void DrawTreeView(Rect rect)
        {
            if (_controller == null || !_controller.IsInitialized || !_isTreeViewInitialized)
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
                _gridView.ScrollPosition = 0;
            }

            if (oldSelectedGroupIndex != _selectedGroupIndex
                || oldSelectedPath != _selectedPath || !_filteredContentsGenerated
                || _searchTextFilterUsed != _searchText)
            {
                EditorUserSettings.SetConfigValue(SelectedGroupIndexUserSettingsKey, _selectedGroupIndex.ToString());
                _filteredContentsGenerated = true;
                _filteredContents.Clear();
                var selectedGroup = _controller.GetContentGroup(_selectedGroupIndex);
                if (!string.IsNullOrEmpty(_selectedPath))
                {
                    var filter = selectedGroup.Contents
                        .Where(c => AssetShelfUtility.HasDirectory(c.Path, _selectedPath))
                        .Where(c => SearchUtility.IsMatched(_searchText, Path.GetFileNameWithoutExtension(c.Path)));
                    _filteredContents.AddRange(filter);
                }
                else
                {
                    var filter = selectedGroup.Contents
                        .Where(c => SearchUtility.IsMatched(_searchText, Path.GetFileNameWithoutExtension(c.Path)));
                    _filteredContents.AddRange(filter);
                }

                if (_selectedAsset != null && !_filteredContents.Exists(v => v.Asset == _selectedAsset))
                {
                    _selectedAsset = null;
                }

                _searchTextFilterUsed = _searchText;
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
            if (_controller == null || !_controller.IsInitialized)
            {
                return;
            }

            // Search bar
            var headerHeight = EditorGUIUtility.singleLineHeight * 1;
            var headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            GUI.Box(headerRect, GUIContent.none);
            using (new GUILayout.AreaScope(headerRect))
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var searchRect = GUILayoutUtility.GetRect(100, EditorGUIUtility.singleLineHeight, GUILayout.MaxWidth(300));
                _searchText = _searchField.OnGUI(searchRect, _searchText);
            }

            var contents = _filteredContents;
            var spacing = new Vector2(5, 5);

            // Draw grid view
            var gridViewRect = new Rect(rect.x, rect.y + headerHeight, rect.width, rect.height - headerHeight);
            _gridView.Draw(gridViewRect, contents.Count, new Vector2(_previewItemSize, _previewItemSize + _previewNameSize), spacing, OnDrawGridItem);

            // Select in Asset Shelf
            if (Event.current.type == EventType.MouseDown)
            {
                var selectedIndex = _gridView.GetIndexInLastLayout(Event.current.mousePosition);
                if (selectedIndex >= 0)
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
                var selectedIndex = _gridView.GetIndexInLastLayout(Event.current.mousePosition);
                if (selectedIndex >= 0 && contents[selectedIndex].Asset == _selectedAsset)
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
                    var selectedIndex = _gridView.GetIndexInLastLayout(Event.current.mousePosition);
                    if (selectedIndex >= 0 && _grabbedAsset != null && contents[selectedIndex].Asset == _grabbedAsset)
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

        private void OnDrawGridItem(Rect rect, int index)
        {
            if (Event.current.type == EventType.Repaint)
            {
                AssetShelfLog.LastDrawPreviewCount++;
                var content = _filteredContents[index];
                AssetShelfUtility.LoadPreviewIfNeeded(content);
                _isLoadingPreviews |= content.Preview == null;
                AssetShelfGUI.DrawGridItem(rect, content, content?.Asset == _selectedAsset);
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
