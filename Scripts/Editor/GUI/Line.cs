using UnityEditor;
using UnityEngine;

namespace AssetShelf
{
    public static partial class AssetShelfGUI
    {
        public static void HorizontalLine(Vector2 leftPosition, float width, float tickness, Color color)
        {
            var rect = new Rect(leftPosition.x, leftPosition.y, width, tickness);
            EditorGUI.DrawRect(rect, color);
        }

        public static void VerticalLine(Vector2 topPosition, float height, float tickness, Color color)
        {
            var rect = new Rect(topPosition.x, topPosition.y, tickness, height);
            EditorGUI.DrawRect(rect, color);
        }
    }
}
