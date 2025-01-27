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

        private Object _targetFolder;
        private List<string> _foundAssetPaths = new List<string>();
        private List<Object> _foundAssets = new List<Object>();
        private List<Texture2D> _foundAssetMiniThumbnails = new List<Texture2D>();
        private List<Texture2D> _foundAssetThumbnails = new List<Texture2D>();

        private Vector2 _scrollPosition;

        public void OnGUI()
        {
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
                _targetFolder = EditorGUILayout.ObjectField(_targetFolder, typeof(Object), false);
                if (changeCheck.changed && _targetFolder != null)
                {
                    var path = AssetDatabase.GetAssetPath(_targetFolder);
                    if (!AssetDatabase.IsValidFolder(path))
                    {
                        _targetFolder = null;
                    }
                }
            }

            if (_targetFolder != null && GUILayout.Button("Update"))
            {
                var paths = AssetDatabase.FindAssets("", new[] { AssetDatabase.GetAssetPath(_targetFolder) });
                _foundAssetPaths.Clear();
                _foundAssetPaths.AddRange(paths);
                _foundAssets.Clear();
                _foundAssetMiniThumbnails.Clear();
                _foundAssetThumbnails.Clear();
                foreach (var path in _foundAssetPaths)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(path));
                    _foundAssets.Add(asset);
                    var miniThumbnail = AssetPreview.GetMiniThumbnail(asset);
                    _foundAssetMiniThumbnails.Add(miniThumbnail);
                    var thumbnail = AssetPreview.GetAssetPreview(asset);
                    _foundAssetThumbnails.Add(thumbnail);
                }
            }
        }

        private void DrawSidebarLayout()
        {
            for (int i = 0; i < 20; i++)
            {
                GUILayout.Label($"Dummy Item {i}");
            }
        }

        private void DrawAssetView(Rect rect)
        {
            var itemSize = 64;
            var spacing = new Vector2(5, 5);
            var scrollbarWidth = 15;
            var viewHeight = GetGridViewHeight(itemSize, spacing, rect.width - scrollbarWidth, _foundAssets.Count);
            var viewRect = new Rect(0, 0, rect.width - scrollbarWidth, viewHeight);
            using (var scrollView = new GUI.ScrollViewScope(rect, _scrollPosition, viewRect))
            {
                _scrollPosition = scrollView.scrollPosition;
                var columnCount = GetGridColumnCount(itemSize, spacing.x, viewRect.width);
                var startRow = Mathf.FloorToInt(_scrollPosition.y / (itemSize + spacing.y));
                var startIndex = startRow * columnCount;
                var endRow = Mathf.CeilToInt((_scrollPosition.y + rect.height) / (itemSize + spacing.y));
                var endIndex = endRow * columnCount;
                DrawGridItems(viewRect, itemSize, spacing, startIndex, endIndex);
            }

            if (Event.current.type == EventType.MouseDrag)
            {
                var gridViewMousePosition = Event.current.mousePosition - rect.position + _scrollPosition;
                var selectedIndex = GetIndexInGridView(itemSize, spacing, viewRect, gridViewMousePosition);
                if (selectedIndex >= 0 && selectedIndex < _foundAssets.Count)
                {
                    var selectedObject = _foundAssets[selectedIndex];
                    if (selectedObject != null)
                    {
                        // Clear drag data
                        DragAndDrop.PrepareStartDrag();

                        // Set up drag data
                        DragAndDrop.objectReferences = new Object[] { selectedObject };

                        // Start drag
                        DragAndDrop.StartDrag($"Dragging Asset: {selectedObject.name}");

                        // Make sure no one uses the event after us
                        Event.current.Use();
                    }
                }
            }
        }

        private float GetGridViewHeight(float itemSize, Vector2 spacing, float width, int itemCount)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, width);
            var rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            return rowCount * (itemSize + spacing.y) - spacing.y;
        }

        private int GetGridColumnCount(float itemSize, float spacing, float width)
        {
            var columnCount = Mathf.FloorToInt((width - itemSize) / (itemSize + spacing)) + 1;
            columnCount = Mathf.Max(1, columnCount);
            return columnCount;
        }

        private void DrawGridItems(Rect rect, float itemSize, Vector2 spacing, int start, int end)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            for (int i = start; i < end; i++)
            {
                if (i >= _foundAssets.Count)
                {
                    break;
                }
                var thumbnail = _foundAssetThumbnails[i];
                if (thumbnail == null)
                {
                    thumbnail = _foundAssetMiniThumbnails[i];
                }

                var thmbnailRect = new Rect(
                    rect.x + (i % columnCount) * (itemSize + spacing.x),
                    rect.y + (i / columnCount) * (itemSize + spacing.y),
                    itemSize,
                    itemSize
                );
                GUI.DrawTexture(thmbnailRect, thumbnail);
            }
        }

        private int GetIndexInGridView(float itemSize, Vector2 spacing, Rect rect, Vector2 position)
        {
            var columnCount = GetGridColumnCount(itemSize, spacing.x, rect.width);
            var column = Mathf.FloorToInt((position.x - rect.x) / (itemSize + spacing.x));
            var row = Mathf.FloorToInt((position.y - rect.y) / (itemSize + spacing.y));
            return column + row * columnCount;
        }
    }
}
