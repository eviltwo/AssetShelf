using UnityEngine;

namespace AssetShelf
{
    public class AssetShelfContainerController
    {
        private readonly AssetShelfContainer _container;

        public AssetShelfContainer Container => _container;

        public int ContainerInstanceID => _container?.GetInstanceID() ?? 0;

        private int _contentGroupCount;

        private string[] _contentGroupNames = new string[0];

        private AssetShelfContentGroup[] _contentGroups = new AssetShelfContentGroup[0];

        private bool _isInitialized;
        public bool IsInitialized => _isInitialized && _container != null;

        public AssetShelfContainerController(AssetShelfContainer container)
        {
            if (container == null)
            {
                Debug.LogError("AssetShelfContainer is null.");
                return;
            }

            _container = container;
        }

        public void Initialize()
        {
            _contentGroupCount = _container.GetContentGroupCount();
            _contentGroups = new AssetShelfContentGroup[_contentGroupCount];
            _contentGroupNames = new string[_contentGroupCount];
            for (int i = 0; i < _contentGroupCount; i++)
            {
                _contentGroupNames[i] = _container.GetContentGroupName(i);
            }

            _isInitialized = true;
        }

        public void Clear()
        {
            _isInitialized = false;
        }

        public AssetShelfContentGroup GetContentGroup(int index)
        {
            if (!IsInitialized)
            {
                return null;
            }

            if (index < 0 || index >= _contentGroupCount)
            {
                Debug.LogError($"Invalid index: {index}, contentGroupCount: {_contentGroupCount}");
                return null;
            }

            if (_contentGroups[index] == null)
            {
                _contentGroups[index] = _container.GetContentGroupWithoutPreview(index);
            }

            return _contentGroups[index];
        }
    }
}
