using System.Collections.Generic;
using UnityEngine;

namespace AssetShelf
{
    public static class PreviewCache
    {
        private static bool _initialized;

        private static int[] _ids;

        private static float[] _times;

        private static RenderTexture[] _rTexs;

        private static Dictionary<int, int> _idPosMap = new Dictionary<int, int>();

        private static int _textureSize = 16;

        private static int _cacheSize = 128;

        public static int CacheSize => _cacheSize;

        public static void ReleaseResources()
        {
            _initialized = false;
            if (_rTexs != null)
            {
                foreach (var rTex in _rTexs)
                {
                    rTex?.Release();
                }
                _rTexs = null;
            }
        }

        private static void Initialize()
        {
            _initialized = true;

            if (_rTexs == null)
            {
                _rTexs = new RenderTexture[_cacheSize];
            }

            if (_ids == null)
            {
                _ids = new int[_cacheSize];
            }

            if (_times == null)
            {
                _times = new float[_cacheSize];
            }
        }

        public static void PushTexture(int instanceID, Texture2D previewTex)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (!_idPosMap.TryGetValue(instanceID, out var pos))
            {
                pos = GetOldestPosition();
                _idPosMap.Remove(_ids[pos]);
                _idPosMap[instanceID] = pos;
                _ids[pos] = instanceID;
            }

            if (_rTexs[pos] == null)
            {
                _rTexs[pos] = new RenderTexture(_textureSize, _textureSize, 0, RenderTextureFormat.ARGB32);
            }

            var tempRT = RenderTexture.active;
            RenderTexture.active = _rTexs[pos];
            Graphics.Blit(previewTex, _rTexs[pos]);
            RenderTexture.active = tempRT;
            _times[pos] = Time.realtimeSinceStartup;
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

        public static bool TryGetTexture(int instanceID, Texture2D tex)
        {
            if (_idPosMap.TryGetValue(instanceID, out var pos))
            {
                if (tex.width != _textureSize || tex.height != _textureSize)
                {
                    tex.Reinitialize(_textureSize, _textureSize, TextureFormat.ARGB32, false);
                }

                var renderTexture = RenderTexture.GetTemporary(_textureSize, _textureSize, 0, RenderTextureFormat.ARGB32);

                Graphics.Blit(_rTexs[pos], renderTexture);

                RenderTexture.active = renderTexture;
                tex.ReadPixels(new Rect(0, 0, _textureSize, _textureSize), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                RenderTexture.ReleaseTemporary(renderTexture);

                _times[pos] = Time.realtimeSinceStartup;
                return true;
            }
            return false;
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
            var nextRTexs = new RenderTexture[nextSize];
            for (int i = 0; i < _cacheSize; i++)
            {
                if (i >= nextSize)
                {
                    _idPosMap.Remove(_ids[i]);
                    _rTexs[i]?.Release();
                    continue;
                }
                nextIds[i] = _ids[i];
                nextTimes[i] = _times[i];
                nextRTexs[i] = _rTexs[i];
            }
            _ids = nextIds;
            _times = nextTimes;
            _rTexs = nextRTexs;
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
