using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class SelectionWithoutPing : System.IDisposable
    {
        private Object _obj;
        private ProjectBrowserLock _lock;
        private bool _isSelectionChanged;

        public SelectionWithoutPing()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            _lock?.Dispose();
            _lock = null;
        }

        public void Select(Object obj)
        {
            _obj = obj;
        }

        public void Update()
        {
            if (_obj != null && _obj == Selection.activeObject)
            {
                _obj = null;
            }

            if (_obj != null)
            {
                if (_lock != null)
                {
                    _lock.Dispose();
                }
                _lock = new ProjectBrowserLock();
                Selection.activeObject = _obj;
                _obj = null;
                _isSelectionChanged = false;
            }

            if (_lock != null && _isSelectionChanged)
            {
                _lock.Dispose();
                _lock = null;
            }
        }

        private void OnSelectionChanged()
        {
            _isSelectionChanged = true;
        }
    }
}
