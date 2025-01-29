namespace AssetShelf
{
    public static class AssetShelfLog
    {
        public static int LastDrawPreviewCount;
        public static int RepaintCallCount;

        public static void Clear()
        {
            LastDrawPreviewCount = 0;
            RepaintCallCount = 0;
        }
    }
}
