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

        private List<AssetShelfContentGroup> _contentGroups = new List<AssetShelfContentGroup>();

        private bool _updateContentsRequired;

        private int _selectedGroupIndex = -1;

        private Vector2 _scrollPosition;

        // Debug
        private static int _debug_loadPreviewCount;

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
                }
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

            GUILayout.Label($"Load Preview Count: {_debug_loadPreviewCount}");
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
            var viewHeight = GetGridViewHeight(itemSize, spacing, rect.width - scrollbarWidth, contents.Count);
            var viewRect = new Rect(0, 0, rect.width - scrollbarWidth, viewHeight);
            using (var scrollView = new GUI.ScrollViewScope(rect, _scrollPosition, viewRect))
            {
                _scrollPosition = scrollView.scrollPosition;
                var columnCount = GetGridColumnCount(itemSize, spacing.x, viewRect.width);
                var startRow = Mathf.FloorToInt(_scrollPosition.y / (itemSize + spacing.y));
                var startIndex = startRow * columnCount;
                var endRow = Mathf.CeilToInt((_scrollPosition.y + rect.height) / (itemSize + spacing.y));
                var endIndex = endRow * columnCount;
                LoadPreviewsIfNeeded(contents, startIndex, endIndex);
                DrawGridItems(viewRect, itemSize, spacing, contents, startIndex, endIndex);
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                var gridViewMousePosition = Event.current.mousePosition - rect.position + _scrollPosition;
                var selectedIndex = GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
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

        private static float GetGridViewHeight(float itemSize, Vector2 spacing, float width, int itemCount)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, width);
            var rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            return rowCount * (itemSize + spacing.y) - spacing.y;
        }

        private static int GetGridColumnCount(float itemSize, float spacing, float width)
        {
            var columnCount = Mathf.FloorToInt((width - itemSize) / (itemSize + spacing)) + 1;
            columnCount = Mathf.Max(1, columnCount);
            return columnCount;
        }

        private static void LoadPreviewsIfNeeded(IReadOnlyList<AssetShelfContent> contents, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count)
                {
                    break;
                }

                var content = contents[i];
                if (content == null || content.Asset == null)
                {
                    continue;
                }
                if (content.MiniPreview == null)
                {
                    content.MiniPreview = AssetPreview.GetMiniThumbnail(content.Asset);
                }
                if (content.Preview == null && !content.SkipPreview)
                {
                    content.Preview = AssetPreview.GetAssetPreview(content.Asset);
                    _debug_loadPreviewCount++;
                    if (content.Preview == null && !AssetPreview.IsLoadingAssetPreview(content.Asset.GetInstanceID()))
                    {
                        content.SkipPreview = true;
                    }
                }
            }
        }

        private static void DrawGridItems(Rect rect, float itemSize, Vector2 spacing, IReadOnlyList<AssetShelfContent> contents, int start, int end)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            for (int i = start; i < end; i++)
            {
                if (i >= contents.Count)
                {
                    break;
                }

                var content = contents[i];
                if (content == null)
                {
                    continue;
                }

                var preview = content.Preview;
                if (preview == null)
                {
                    preview = content.MiniPreview;
                }
                if (preview == null)
                {
                    preview = EditorGUIUtility.whiteTexture;
                }

                var imageRect = new Rect(
                    rect.x + (i % columnCount) * (itemSize + spacing.x),
                    rect.y + (i / columnCount) * (itemSize + spacing.y),
                    itemSize,
                    itemSize
                );
                GUI.DrawTexture(imageRect, preview);
            }
        }

        private static int GetIndexInGridView(float itemSize, Vector2 spacing, Rect rect, Vector2 position)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            var column = Mathf.FloorToInt((position.x - rect.x) / (itemSize + spacing.x));
            var row = Mathf.FloorToInt((position.y - rect.y) / (itemSize + spacing.y));
            return column + row * columnCount;
        }
    }
}
