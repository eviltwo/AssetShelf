using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfContent
    {
        private static Stopwatch Stopwatch = new Stopwatch();
        public static bool IsLimitted => Stopwatch.IsRunning && Stopwatch.ElapsedMilliseconds > 33; // 30 fps

        public static void ResetLoadAssetCount()
        {
            Stopwatch.Stop();
            Stopwatch.Reset();
        }

        private static void StartLoadAssetCount()
        {
            if (!Stopwatch.IsRunning)
            {
                Stopwatch.Start();
            }
        }

        private Object _asset;

        public Object Asset
        {
            get
            {
                if (_asset == null && !IsLimitted)
                {
                    StartLoadAssetCount();
                    _asset = AssetDatabase.LoadAssetAtPath<Object>(Path);
                }
                return _asset;
            }
        }

        public string Path;
        public Texture2D Preview;
    }

    public class AssetShelfContentGroup
    {
        public string Name;
        public List<AssetShelfContent> Contents = new List<AssetShelfContent>();
    }
}
