namespace AssetShelf
{
    public static class AssetShelfLog
    {
        public static int LoadPreviewTotalCount;
        public static int LastDrawPreviewCount;
        public static int RepaintCallCount;

        public static void Clear()
        {
            LoadPreviewTotalCount = 0;
            LastDrawPreviewCount = 0;
            RepaintCallCount = 0;
        }
    }
}
