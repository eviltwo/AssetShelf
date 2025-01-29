using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public class ProjectBrowserLock : IDisposable
    {
        private static int LockCount;
        private static bool BeforeLockState;

        private readonly EditorWindow _projectWindow;
        private readonly bool _lockStateChanged;

        public ProjectBrowserLock()
        {
            try
            {
                _projectWindow = GetProjectWindow();
                if (_projectWindow == null)
                {
                    return;
                }

                if (LockCount == 0)
                {
                    BeforeLockState = GetLockState();
                }

                SetLockState(true);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            LockCount++;
            _lockStateChanged = true;
        }

        public void Dispose()
        {
            if (!_lockStateChanged || _projectWindow == null)
            {
                return;
            }

            LockCount--;
            if (LockCount == 0)
            {
                try
                {
                    SetLockState(BeforeLockState);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        //
        // Reflections
        //

        private static Type ProjectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
        private static PropertyInfo IsLockedProperty = ProjectBrowserType.GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

        private EditorWindow GetProjectWindow()
        {
            return EditorWindow.GetWindow(ProjectBrowserType, false, "Project", false);
        }

        private void SetLockState(bool isLock)
        {
            IsLockedProperty.SetValue(_projectWindow, isLock);
        }

        private bool GetLockState()
        {
            return (bool)IsLockedProperty.GetValue(_projectWindow);
        }
    }
}
