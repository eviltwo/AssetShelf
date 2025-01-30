using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class SelectionWithoutPing : System.IDisposable
    {
        private Object _obj;
        private ProjectBrowserLock _lock;
        private bool _isSelectionChanged;

        private bool _isRunning;
        public bool IsRunning => _isRunning;

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
            if (!_isRunning)
            {
                _isRunning = true;
                _obj = obj;
            }
        }

        public void Update()
        {
            if (_isRunning)
            {
                if (_lock == null)
                {
                    if (_obj == null)
                    {
                        _isRunning = false;
                        return;
                    }

                    if (_obj == Selection.activeObject)
                    {
                        _obj = null;
                        _isRunning = false;
                        return;
                    }

                    _lock = new ProjectBrowserLock();
                    _isSelectionChanged = false;
                    Selection.activeObject = _obj;
                }
                else if (_isSelectionChanged)
                {
                    _isRunning = false;
                    _obj = null;
                    _lock.Dispose();
                    _lock = null;
                }
            }
        }

        private void OnSelectionChanged()
        {
            _isSelectionChanged = true;
        }
    }
}
