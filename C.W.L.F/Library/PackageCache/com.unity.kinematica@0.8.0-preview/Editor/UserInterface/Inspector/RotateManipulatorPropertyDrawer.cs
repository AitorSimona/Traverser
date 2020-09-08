using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    public class RotateManipulator : UnityEngine.PropertyAttribute {}

    [CustomPropertyDrawer(typeof(RotateManipulator), true)]
    internal class RotateManipulatorPropertyDrawer : ManipulatorPropertyDrawer
    {
        protected override void OnPositionChanged(Vector3 position) {}

        protected override void OnRotationChanged(Quaternion rotation)
        {
            m_Property.quaternionValue = rotation;
            m_Property.serializedObject.ApplyModifiedProperties();
            InspectorWindowUtils.RepaintInspectors();
        }

        protected override void Hook(SerializedProperty property)
        {
            ManipulatorGizmo.Instance.Hook(property, property.quaternionValue);
        }

        protected override void ReTransform()
        {
            ManipulatorGizmo.Instance.ReTransform(m_Property, m_Property.quaternionValue);
        }
    }
}
