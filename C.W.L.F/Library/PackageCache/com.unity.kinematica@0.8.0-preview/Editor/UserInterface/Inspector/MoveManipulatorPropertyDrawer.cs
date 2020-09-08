using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    public class MoveManipulatorAttribute : UnityEngine.PropertyAttribute {}

    [CustomPropertyDrawer(typeof(MoveManipulatorAttribute), true)]
    internal class MoveManipulatorPropertyDrawer : ManipulatorPropertyDrawer
    {
        protected override void OnPositionChanged(Vector3 position)
        {
            m_Property.vector3Value = position;
            m_Property.serializedObject.ApplyModifiedProperties();
            InspectorWindowUtils.RepaintInspectors();
        }

        protected override void OnRotationChanged(Quaternion rotation) {}

        protected override void Hook(SerializedProperty property)
        {
            ManipulatorGizmo.Instance.Hook(property, property.vector3Value);
        }

        protected override void ReTransform()
        {
            ManipulatorGizmo.Instance.ReTransform(m_Property, m_Property.vector3Value);
        }
    }
}
