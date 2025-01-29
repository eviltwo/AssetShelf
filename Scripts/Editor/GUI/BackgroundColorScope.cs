using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        public class BackgroundColorScope : System.IDisposable
        {
            private Color _previousColor;

            public BackgroundColorScope(Color color)
            {
                _previousColor = GUI.backgroundColor;
                GUI.backgroundColor = color;
            }

            public void Dispose()
            {
                GUI.backgroundColor = _previousColor;
            }
        }
    }
}
