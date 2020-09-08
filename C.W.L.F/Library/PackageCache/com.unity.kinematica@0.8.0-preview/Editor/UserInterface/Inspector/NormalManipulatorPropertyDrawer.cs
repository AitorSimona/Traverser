using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    public class NormalManipulatorAttribute : UnityEngine.PropertyAttribute {}

    [CustomPropertyDrawer(typeof(NormalManipulatorAttribute), true)]
    internal class NormalManipulatorPropertyDrawer : ManipulatorPropertyDrawer
    {
        protected override void OnPositionChanged(Vector3 position) {}

        protected override void OnRotationChanged(Quaternion rotation)
        {
            m_Property.vector3Value = rotation * Vector3.forward;
            m_Property.serializedObject.ApplyModifiedProperties();
            InspectorWindowUtils.RepaintInspectors();
        }

        protected override void Hook(SerializedProperty property)
        {
            ManipulatorGizmo.Instance.Hook(property, Quaternion.LookRotation(property.vector3Value.normalized), ManipulatorGizmo.GizmoMode.RotateNormal);
        }

        protected override void ReTransform()
        {
            ManipulatorGizmo.Instance.ReTransform(m_Property, Quaternion.LookRotation(m_Property.vector3Value.normalized));
        }
    }
}
