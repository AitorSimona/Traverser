using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal abstract class ManipulatorPropertyDrawer : PropertyDrawer
    {
        static GUIContent[] s_ManipulatorToolContent = {EditorGUIUtility.IconContent("TransformTool")};
        static int k_ToggleSize = 32;

        protected SerializedProperty m_Property;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Property = property;

            var manipulatorActivated = ManipulatorGizmo.Instance.IsTarget(property);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            GUILayout.Space(EditorGUI.indentLevel * 10);
            var selected = GUILayout.Toolbar(manipulatorActivated ? 0 : -1, s_ManipulatorToolContent, GUILayout.Width(k_ToggleSize));

            if (EditorGUI.EndChangeCheck())
            {
                if (manipulatorActivated)
                {
                    OnHandleToggled(property, false);
                }
                else
                {
                    OnHandleToggled(property, selected == 0);
                }
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property,  true);
            if (EditorGUI.EndChangeCheck())
            {
                ReTransform();
            }

            EditorGUILayout.EndHorizontal();
        }

        void OnHandleToggled(SerializedProperty property, bool toggled)
        {
            if (toggled)
            {
                Hook(property);
                RegisterToTransformGizmo();
            }
            else if (ManipulatorGizmo.Instance.IsTarget(property))
            {
                ManipulatorGizmo.Instance.Unhook();
            }
        }

        protected abstract void ReTransform();

        protected abstract void Hook(SerializedProperty property);

        public void RegisterToTransformGizmo()
        {
            ManipulatorGizmo.Instance.PositionChanged += OnPositionChanged;
            ManipulatorGizmo.Instance.RotationChanged += OnRotationChanged;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        protected abstract void OnPositionChanged(Vector3 v);
        protected abstract void OnRotationChanged(Quaternion q);
    }
}
