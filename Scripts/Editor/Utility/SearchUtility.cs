namespace AssetShelf
{
    public static class SearchUtility
    {
        public static bool IsMatched(string search, string target)
        {
            if (string.IsNullOrEmpty(search))
            {
                return true;
            }

            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            return target.ToLower().Contains(search.ToLower());
        }
    }
}
