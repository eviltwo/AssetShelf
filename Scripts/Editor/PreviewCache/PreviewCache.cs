using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public static class PreviewCache
    {
        private static bool _initialized;

        private static readonly BiDictionary<Object, int> _objectPosMap = new();

        private static float[] _times;

        private static readonly Dictionary<int, Texture2D> _posToPreviewTex = new();

        private static Texture2D[] _textures;

        private static readonly int _textureSize = 16;

        private static int _cacheSize = 128;

        public static int CacheSize => _cacheSize;

        public static void ReleaseResources()
        {
            _initialized = false;
            if (_textures != null)
            {
                foreach (var tex in _textures)
                {
                    Object.DestroyImmediate(tex);
                }
                _textures = null;
            }
        }

        private static void Initialize()
        {
            _initialized = true;

            if (_textures == null)
            {
                _textures = new Texture2D[_cacheSize];
            }

            if (_times == null)
            {
                _times = new float[_cacheSize];
            }
        }

        public static void PushTexture(Object sourceObject, Texture2D previewTex)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (previewTex == null)
            {
                return;
            }

            var hasEntry = _objectPosMap.TryGetValue(sourceObject, out var pos);
            if (!hasEntry)
            {
                pos = GetOldestPosition();
                _objectPosMap.RemoveEntry(pos);
                _objectPosMap.AddEntry(sourceObject, pos);
            }

            if (_textures[pos] != null)
            {
                if (_posToPreviewTex.TryGetValue(pos, out var tex) && tex == previewTex)
                {
                    // the texture is already cached.
                    return;
                }
                else
                {
                    Object.DestroyImmediate(_textures[pos]);
                    _textures[pos] = null;
                }
            }

            if (_textures[pos] == null)
            {
                _textures[pos] = new Texture2D(_textureSize, _textureSize);
            }

            var tempRT = RenderTexture.GetTemporary(_textureSize, _textureSize);
            var beforeRT = RenderTexture.active;
            RenderTexture.active = tempRT;
            Graphics.Blit(previewTex, tempRT);
            _textures[pos].ReadPixels(new Rect(0, 0, _textureSize, _textureSize), 0, 0);
            _textures[pos].Apply(false, true);
            RenderTexture.active = beforeRT;
            RenderTexture.ReleaseTemporary(tempRT);

            _times[pos] = Time.realtimeSinceStartup;
            _posToPreviewTex[pos] = previewTex;
        }

        private static int GetOldestPosition()
        {
            var minValue = float.MaxValue;
            var pos = 0;
            for (int i = 0; i < _times.Length; i++)
            {
                if (_times[i] == 0)
                {
                    return i;
                }

                if (_times[i] < minValue)
                {
                    minValue = _times[i];
                    pos = i;
                }
            }

            return pos;
        }

        public static Texture2D GetTexture(Object sourceObject)
        {
            if (_objectPosMap.TryGetValue(sourceObject, out var pos) && _textures[pos] != null)
            {
                _times[pos] = Time.realtimeSinceStartup;
                return _textures[pos];
            }

            return null;
        }

        public static void SetCacheSize(int capacity)
        {
            if (!_initialized)
            {
                Initialize();
            }

            var nextSize = GetSquaredSize(capacity);
            if (_cacheSize == nextSize)
            {
                return;
            }

            var nextTimes = new float[nextSize];
            var nextTexs = new Texture2D[nextSize];
            for (int i = 0; i < _cacheSize; i++)
            {
                if (i >= nextSize)
                {
                    _objectPosMap.RemoveEntry(i);
                    Object.DestroyImmediate(_textures[i]);
                    continue;
                }
                nextTimes[i] = _times[i];
                nextTexs[i] = _textures[i];
            }
            _times = nextTimes;
            _textures = nextTexs;
            _cacheSize = nextSize;
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
        
        private class BiDictionary<TKey, TValue>
        {
            private readonly Dictionary<TKey, TValue> _keyToValue = new();
            private readonly Dictionary<TValue, TKey> _valueToKey = new();

            public bool TryGetValue(TKey key, out TValue value)
            {
                return _keyToValue.TryGetValue(key, out value);
            }

            public bool TryGetKey(TValue value, out TKey key)
            {
                return _valueToKey.TryGetValue(value, out key);
            }

            public void AddEntry(TKey key, TValue value)
            {
                _keyToValue.Add(key, value);
                _valueToKey.Add(value, key);
            }

            public void RemoveEntry(TValue value)
            {
                if (_valueToKey.TryGetValue(value, out var key))
                {
                    _keyToValue.Remove(key);
                    _valueToKey.Remove(value);
                }
            }
        }
    }
}
