using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public static class PreviewCache
    {
        private static bool _initialized;

        private static int[] _assetIds;

        private static float[] _times;

        private static int[] _srcTexIds;

        private static Texture2D[] _textures;

        private static Dictionary<int, int> _idPosMap = new Dictionary<int, int>();

        private static int _textureSize = 16;

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

            if (_assetIds == null)
            {
                _assetIds = new int[_cacheSize];
            }

            if (_times == null)
            {
                _times = new float[_cacheSize];
            }

            if (_srcTexIds == null)
            {
                _srcTexIds = new int[_cacheSize];
            }
        }

        public static void PushTexture(int instanceID, Texture2D previewTex)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (previewTex == null)
            {
                return;
            }

            if (!_idPosMap.TryGetValue(instanceID, out var pos))
            {
                pos = GetOldestPosition();
                _idPosMap.Remove(_assetIds[pos]);
                _idPosMap[instanceID] = pos;
                _assetIds[pos] = instanceID;
            }

            if (_textures[pos] != null)
            {
                // If the instance ID is the same, the texture is already cached.
                // If the instance ID is different, the texture is stale.
                if (_srcTexIds[pos] == previewTex.GetInstanceID())
                {
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
            _srcTexIds[pos] = previewTex.GetInstanceID();
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

        public static Texture2D GetTexture(int instanceID)
        {
            if (_idPosMap.TryGetValue(instanceID, out var pos) && _textures[pos] != null)
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

            var nextIds = new int[nextSize];
            var nextTimes = new float[nextSize];
            var nextTexs = new Texture2D[nextSize];
            var nextSrcTexIds = new int[nextSize];
            for (int i = 0; i < _cacheSize; i++)
            {
                if (i >= nextSize)
                {
                    _idPosMap.Remove(_assetIds[i]);
                    Object.DestroyImmediate(_textures[i]);
                    continue;
                }
                nextIds[i] = _assetIds[i];
                nextTimes[i] = _times[i];
                nextTexs[i] = _textures[i];
                nextSrcTexIds[i] = _srcTexIds[i];
            }
            _assetIds = nextIds;
            _times = nextTimes;
            _textures = nextTexs;
            _srcTexIds = nextSrcTexIds;
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
    }
}
