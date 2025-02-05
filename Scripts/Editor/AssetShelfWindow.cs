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
        private static string PreviewItemSizeUserSettingsKey = "AssetShelfWindow.PreviewItemSize";
        private static string SelectedGroupIndexUserSettingsKey = "AssetShelfWindow.SelectedGroupIndex";
        private static string TreeViewStateUserSettingsKey = "AssetShelfWindow.TreeViewState";

        private AssetShelfContainerController _controller;

        private AssetShelfTreeView _treeView;

        private TreeViewState _treeViewState;

        private bool _isTreeViewInitialized;

        private bool _isDataReloadRequired;

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

            _selectionWithoutPing = new SelectionWithoutPing();
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
                PreviewCache.SetCacheSize(AssetShelfLog.LastDrawPreviewCount * 8);
            }
        }

        private void DrawHeaderLayout()
        {
            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_controller == null))
                {
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), GUILayout.Width(40)))
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

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUILayout.Width(40)))
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
                _assetViewScrollPosition = Vector2.zero;
                _selectedAsset = null;
            }

            if (oldSelectedGroupIndex != _selectedGroupIndex || oldSelectedPath != _selectedPath || !_filteredContentsGenerated)
            {
                EditorUserSettings.SetConfigValue(SelectedGroupIndexUserSettingsKey, _selectedGroupIndex.ToString());
                _filteredContentsGenerated = true;
                _filteredContents.Clear();
                var selectedGroup = _controller.GetContentGroup(_selectedGroupIndex);
                if (!string.IsNullOrEmpty(_selectedPath))
                {

                    _filteredContents.AddRange(selectedGroup.Contents.Where(c => AssetShelfUtility.HasDirectory(c.Path, _selectedPath)));
                }
                else
                {
                    _filteredContents.AddRange(selectedGroup.Contents);
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
            if (_controller == null || !_controller.IsInitialized)
            {
                return;
            }

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
