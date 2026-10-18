using UnityEngine;

namespace AssetShelf
{
    public static class AssetShelfLog
    {
        private static int _lastDrawPreviewCount = 0;
        public static int LastDrawPreviewCount => _lastDrawPreviewCount;

        private static int _maxDrawPreviewCount = 0;
        public static int MaxDrawPreviewCount => _maxDrawPreviewCount;
        
        private static int _repaintCount = 0;
        public static int RepaintCount => _repaintCount;

        public static void ClearDrawCount()
        {
            _lastDrawPreviewCount = 0;
        }

        public static void AddDrawCount()
        {
            _lastDrawPreviewCount++;
            _maxDrawPreviewCount = Mathf.Max(_lastDrawPreviewCount, MaxDrawPreviewCount);
        }

        public static void ClearRepaintCount()
        {
            _repaintCount = 0;
        }

        public static void AddRepaintCount()
        {
            _repaintCount++;
        }
    }
}
