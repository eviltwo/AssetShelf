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

            search = search.ToLower();
            target = target.ToLower();

            var splited = search.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in splited)
            {
                if (!target.Contains(s))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
