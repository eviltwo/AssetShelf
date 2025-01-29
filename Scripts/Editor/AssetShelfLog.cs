namespace AssetShelf
{
    public static class AssetShelfLog
    {
        public static int PreviewRequestCount;
        public static int LastDrawPreviewCount;
        public static int RepaintCallCount;

        public static void Clear()
        {
            PreviewRequestCount = 0;
            LastDrawPreviewCount = 0;
            RepaintCallCount = 0;
        }
    }
}
