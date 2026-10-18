using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public static class PreviewCache
    {

        private static readonly Dictionary<Object, CacheEntry> _entries = new();

        private static readonly int _textureSize = 16;

        private static int _cacheSize = 128;

        public static void ReleaseEntryResources()
        {
            foreach (var entry in _entries.Values)
            {
                entry.ReleaseResources();
            }
            
            _entries.Clear();
        }

        public static void PushTexture(Object sourceObject, Texture2D sourceTex)
        {
            if (sourceObject == null || sourceTex == null)
            {
                return;
            }
            
            var hasEntry = _entries.TryGetValue(sourceObject, out var entry);
            if (!hasEntry)
            {
                if (_entries.Count >= _cacheSize)
                {
                    RemoveOldEntry();
                }

                entry = new CacheEntry();
                _entries.Add(sourceObject, entry);
            }

            if (entry.SourceTextureReference != null && entry.SourceTextureReference == sourceTex)
            {
                // the texture is already cached.
                return;
            }

            entry.ReleaseResources();
            entry.GeneratedTexture = new Texture2D(_textureSize, _textureSize);
            var tempRT = RenderTexture.GetTemporary(_textureSize, _textureSize);
            var beforeRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            Graphics.Blit(sourceTex, tempRT);
            entry.GeneratedTexture.ReadPixels(new Rect(0, 0, _textureSize, _textureSize), 0, 0);
            entry.GeneratedTexture.Apply(false, true);
            RenderTexture.active = beforeRT;
            RenderTexture.ReleaseTemporary(tempRT);

            entry.LastAccessTimestamp = Time.realtimeSinceStartup;
            entry.SourceTextureReference = sourceTex;
        }

        private static void RemoveOldEntry()
        {
            var oldTimestamp = float.MaxValue;
            Object oldKey = null;
            CacheEntry oldEntry = null;
            foreach (var keyValue in _entries)
            {
                if (keyValue.Key == null)
                {
                    keyValue.Value?.ReleaseResources();
                    _entries.Remove(keyValue.Key);
                    return;
                }

                if (keyValue.Value.LastAccessTimestamp < oldTimestamp)
                {
                    oldTimestamp = keyValue.Value.LastAccessTimestamp;
                    oldKey = keyValue.Key;
                    oldEntry = keyValue.Value;
                }
            }

            if (oldKey != null)
            {
                oldEntry?.ReleaseResources();
                _entries.Remove(oldKey);
            }
        }

        public static Texture2D GetTexture(Object sourceObject)
        {
            if (_entries.TryGetValue(sourceObject, out var entry)
                && entry.GeneratedTexture != null)
            {
                entry.LastAccessTimestamp = Time.realtimeSinceStartup;
                return entry.GeneratedTexture;
            }

            return null;
        }

        public static void SetCacheSize(int capacity)
        {
            _cacheSize = GetSquaredSize(capacity);
            var removeCount = _entries.Count - _cacheSize;
            for (int i = 0; i < removeCount; i++)
            {
                RemoveOldEntry();
            }
        }

        private static int GetSquaredSize(int capacity)
        {
            const int maxSquare = 64;
            for (int i = 0; i < maxSquare; i++)
            {
                if (capacity <= i * i)
                {
                    return i * i;
                }
            }

            return maxSquare * maxSquare;
        }

        private class CacheEntry
        {
            public float LastAccessTimestamp;
            public Texture2D SourceTextureReference;
            public Texture2D GeneratedTexture;

            public void ReleaseResources()
            {
                Object.DestroyImmediate(GeneratedTexture);
                GeneratedTexture = null;
            }
        }
    }
}
