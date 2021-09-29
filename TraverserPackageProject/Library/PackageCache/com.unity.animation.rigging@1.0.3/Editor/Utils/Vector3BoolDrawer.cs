using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomPropertyDrawer(typeof(Vector3Bool))]
    class Vector3BoolDrawer : PropertyDrawer
    {
        private const int k_Offset = 16;
        private const int k_ToggleWidth = 50;
        private static readonly GUIContent k_XLabel = new GUIContent("X");
        private static readonly GUIContent k_YLabel = new GUIContent("Y");
        private static readonly GUIContent k_ZLabel = new GUIContent("Z");

        private SerializedProperty m_X;
        private SerializedProperty m_Y;
        private SerializedProperty m_Z;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);

            m_X = property.FindPropertyRelative("x");
            m_Y = property.FindPropertyRelative("y");
            m_Z = property.FindPropertyRelative("z");

            rect = EditorGUI.PrefixLabel(rect, label);

            int indentLvl = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            rect.x -= 1;
            var xRect = new Rect (rect.xMin, rect.yMin, k_ToggleWidth, rect.height);
            var yRect = new Rect (xRect.xMax, rect.yMin, k_ToggleWidth, rect.height);
            var zRect = new Rect (yRect.xMax, rect.yMin, k_ToggleWidth, rect.height);

            EditorGUI.BeginChangeCheck();
            DrawToggleField(xRect, m_X, k_XLabel);
            DrawToggleField(yRect, m_Y, k_YLabel);
            DrawToggleField(zRect, m_Z, k_ZLabel);
            if(EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();

            EditorGUI.indentLevel = indentLvl;
            EditorGUI.EndProperty();
        }

        void DrawToggleField(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.LabelField(rect, label);
            rect.x += k_Offset;
            rect.width -= k_Offset;
            EditorGUI.PropertyField(rect, property, GUIContent.none);
        }
    }
}
