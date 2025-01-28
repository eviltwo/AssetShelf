using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetShelf
{
    public class AssetShelfContentDirectoryAnalyzer
    {
        private readonly AssetShelfContentDirectory _root;

        public AssetShelfContentDirectory Root => _root;

        public AssetShelfContentDirectoryAnalyzer(AssetShelfContentGroup contentGroup)
        {
            _root = new AssetShelfContentDirectory();
            var paths = contentGroup.Contents.Select(c => Path.GetDirectoryName(c.Path)).Where(v => !string.IsNullOrEmpty(v)).Distinct().OrderBy(v => v).ToList();
            foreach (var path in paths)
            {
                var parts = path.Split(Path.DirectorySeparatorChar);
                AddDirectory(_root, parts, 0);
            }
            for (int i = 0; i < 10; i++)
            {
                if (_root.Childs.Count == 1)
                {
                    _root = _root.Childs[0];
                }
                else
                {
                    break;
                }
            }
            _root.Path = "";
            _root.ShortName = "";
        }

        private void AddDirectory(AssetShelfContentDirectory dir, string[] paths, int index)
        {
            var found = false;
            foreach (var child in dir.Childs)
            {
                if (child.ShortName == paths[index])
                {
                    found = true;
                    if (index + 1 < paths.Length)
                    {
                        AddDirectory(child, paths, index + 1);
                    }
                    break;
                }
            }
            if (!found)
            {
                var newDir = new AssetShelfContentDirectory
                {
                    Path = string.Join(Path.DirectorySeparatorChar.ToString(), paths.Take(index + 1)),
                    ShortName = paths[index]
                };
                dir.Childs.Add(newDir);
                if (index + 1 < paths.Length)
                {
                    AddDirectory(newDir, paths, index + 1);
                }
            }
        }
    }

    public class AssetShelfContentDirectory
    {
        public string Path;
        public string ShortName;
        public List<AssetShelfContentDirectory> Childs = new List<AssetShelfContentDirectory>();
    }
}
