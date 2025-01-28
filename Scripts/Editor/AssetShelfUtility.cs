using System.IO;

namespace AssetShelf
{
    public static class AssetShelfUtility
    {
        public static bool HasDirectory(string path, string directoryName)
        {
            var pathParts = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar);
            var dirParts = directoryName.Split(Path.DirectorySeparatorChar);
            for (int i = 0; i < dirParts.Length; i++)
            {
                if (i >= pathParts.Length)
                {
                    return false;
                }
                if (pathParts[i] != dirParts[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
