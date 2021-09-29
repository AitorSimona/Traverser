using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(RigLayer))]
    class RigLayerDrawer : PropertyDrawer
    {
        const int k_Padding = 6;
        const int k_TogglePadding = 30;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            var w = rect.width - k_TogglePadding;
            var weightRect = new Rect(rect.x + w + k_Padding, rect.y, rect.width - w - k_Padding, rect.height);
            rect.width = w;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("m_Rig"), label);

            var indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(weightRect, property.FindPropertyRelative("m_Active"), GUIContent.none);
            EditorGUI.indentLevel = indentLvl;

            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }
}

