using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AssetShelf
{
    [CustomEditor(typeof(AssetShelfSettings))]
    public class AssetShelfSettingsEditor : Editor
    {
        private ReorderableList _rulesList;

        private void OnEnable()
        {
            _rulesList = new ReorderableList(serializedObject, serializedObject.FindProperty("Rules"), true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Groups"),
                drawElementCallback = OnDrawElement,
                elementHeightCallback = OnElementHeight,
            };
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _rulesList.serializedProperty.GetArrayElementAtIndex(index);
            rect.x += 10;
            rect.width -= 10;
            EditorGUI.PropertyField(rect, element, true);
        }

        private float OnElementHeight(int index)
        {
            var element = _rulesList.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _rulesList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
