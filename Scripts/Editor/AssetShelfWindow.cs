using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfWindow : EditorWindow
    {
        [MenuItem("Window/Asset Shelf")]
        private static void Open()
        {
            var window = GetWindow<AssetShelfWindow>();
            window.titleContent = new GUIContent("Asset Shelf");
            window.Show();
        }

        private AssetShelfContainer _container;

        private int _lastContainerPropertyChangeCount;

        private List<AssetShelfContentGroup> _contentGroups = new List<AssetShelfContentGroup>();

        private bool _updateContentsRequired;

        private int _selectedGroupIndex = 0;

        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _updateContentsRequired = true;
        }

        public void OnGUI()
        {
            if (_updateContentsRequired)
            {
                _updateContentsRequired = false;
                _contentGroups.Clear();
                if (_container != null)
                {
                    _container.CollectContentGroupsWithoutPreview(_contentGroups);
                    _lastContainerPropertyChangeCount = _container.PropertyChangeCount;
                }
            }

            if (_container != null && _lastContainerPropertyChangeCount != _container.PropertyChangeCount)
            {
                _lastContainerPropertyChangeCount = _container.PropertyChangeCount;
                _updateContentsRequired = true;
            }

            var headerRect = new Rect(0, 0, position.width, 40);
            GUI.Box(headerRect, GUIContent.none);
            using (new GUILayout.AreaScope(headerRect))
            {
                DrawHeaderLayout();
            }

            var sidebarRect = new Rect(0, headerRect.height, 200, position.height - headerRect.height);
            GUI.Box(sidebarRect, GUIContent.none);
            using (new GUILayout.AreaScope(sidebarRect))
            {
                DrawSidebarLayout();
            }

            var assetViewRect = new Rect(sidebarRect.width, headerRect.height, position.width - sidebarRect.width, position.height - headerRect.height);
            DrawAssetView(assetViewRect);
        }

        private void DrawHeaderLayout()
        {
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                _container = EditorGUILayout.ObjectField(_container, typeof(AssetShelfContainer), false) as AssetShelfContainer;
                if (changeCheck.changed)
                {
                    _updateContentsRequired = true;
                }
            }
        }

        private string[] _groupTitleBufer = new string[0];
        private void DrawSidebarLayout()
        {
            if (_groupTitleBufer.Length != _contentGroups.Count)
            {
                _groupTitleBufer = new string[_contentGroups.Count];
            }
            for (int i = 0; i < _contentGroups.Count; i++)
            {
                _groupTitleBufer[i] = _contentGroups[i].Name;
            }
            _selectedGroupIndex = GUILayout.SelectionGrid(_selectedGroupIndex, _groupTitleBufer, 1);

            EditorGUILayout.Space();

            GUILayout.Label($"Load preview total: {AssetShelfLog.LoadPreviewTotalCount}");
            GUILayout.Label($"Last draw preview: {AssetShelfLog.LastDrawPreviewCount}");
        }

        private void DrawAssetView(Rect rect)
        {
            if (_selectedGroupIndex < 0 || _selectedGroupIndex >= _contentGroups.Count)
            {
                return;
            }

            var contents = _contentGroups[_selectedGroupIndex].Contents;
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
                AssetShelfGUI.LoadPreviewsIfNeeded(contents, startIndex, endIndex);
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
