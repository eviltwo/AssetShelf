using UnityEngine;

namespace AssetShelf
{
    public class GridView
    {
        private Vector2 _scrollPosition;

        public float ScrollPosition
        {
            get { return _scrollPosition.y; }
            set { _scrollPosition.y = value; }
        }

        public delegate void DrawItemCallback(Rect rect, int index);

        public Rect LastDrawRect { get; private set; }

        public int LastDrawItemCount { get; private set; }

        public Vector2 LastDrawItemSize { get; private set; }

        public Vector2 LastDrawSpacing { get; private set; }

        public Vector2 LastDrawScrollPosition { get; private set; }

        public int LastDrawResultResultIndex { get; private set; }

        public int LastDrawResultItemCount { get; private set; }

        public GridView()
        {
        }

        public void Draw(Rect rect, int itemCount, Vector2 itemSize, Vector2 spacing, DrawItemCallback onDrawItem)
        {
            const float scrollbarWidth = 15;
            var columnCount = CalculateColumnCount(itemSize.x, spacing.x, rect.width - scrollbarWidth);
            var rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
            var contentsHeight = rowCount * (itemSize.y + spacing.y) - spacing.y;
            var contentsRect = new Rect(0, 0, rect.width - scrollbarWidth, contentsHeight);
            using (var scrollView = new GUI.ScrollViewScope(rect, _scrollPosition, contentsRect))
            {
                _scrollPosition = scrollView.scrollPosition;
                var startRow = Mathf.FloorToInt(_scrollPosition.y / (itemSize.y + spacing.y));
                var startIndex = Mathf.Min(startRow * columnCount, itemCount - 1);
                var endRow = Mathf.CeilToInt((_scrollPosition.y + rect.height) / (itemSize.y + spacing.y));
                var endIndex = Mathf.Min(endRow * columnCount, itemCount - 1);
                for (var i = startIndex; i <= endIndex; i++)
                {
                    var itemRect = new Rect(
                        (i % columnCount) * (itemSize.x + spacing.x),
                        (i / columnCount) * (itemSize.y + spacing.y),
                        itemSize.x,
                        itemSize.y);
                    onDrawItem(itemRect, i);
                }

                LastDrawResultResultIndex = startIndex;
                LastDrawResultItemCount = endIndex - startIndex;
            }

            LastDrawRect = rect;
            LastDrawItemCount = itemCount;
            LastDrawItemSize = itemSize;
            LastDrawSpacing = spacing;
            LastDrawScrollPosition = _scrollPosition;
        }

        public int GetIndexInLastLayout(Vector2 position)
        {
            if (!LastDrawRect.Contains(position))
            {
                return -1;
            }

            var columnCount = CalculateColumnCount(LastDrawItemSize.x, LastDrawSpacing.x, LastDrawRect.width);
            var row = Mathf.FloorToInt((position.y - LastDrawRect.y + LastDrawScrollPosition.y) / (LastDrawItemSize.y + LastDrawSpacing.y));
            var column = Mathf.FloorToInt((position.x - LastDrawRect.x) / (LastDrawItemSize.x + LastDrawSpacing.x));
            if (column >= columnCount)
            {
                return -1;
            }

            var index = row * columnCount + column;
            if (index >= LastDrawItemCount)
            {
                return -1;
            }

            return index;
        }

        private static int CalculateColumnCount(float itemWidth, float spacing, float width)
        {
            var columnCount = Mathf.FloorToInt((width - itemWidth) / (itemWidth + spacing)) + 1;
            columnCount = Mathf.Max(1, columnCount);
            return columnCount;
        }
    }
}
