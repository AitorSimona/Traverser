using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(OverrideTransform))]
    class OverrideTransformEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_OverrideSource;
        SerializedProperty m_OverridePosition;
        SerializedProperty m_OverrideRotation;
        SerializedProperty m_Space;
        SerializedProperty m_PositionWeight;
        SerializedProperty m_RotationWeight;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_ConstrainedObject = data.FindPropertyRelative("m_ConstrainedObject");
            m_OverrideSource = data.FindPropertyRelative("m_OverrideSource");
            m_OverridePosition = data.FindPropertyRelative("m_OverridePosition");
            m_OverrideRotation = data.FindPropertyRelative("m_OverrideRotation");
            m_Space = data.FindPropertyRelative("m_Space");
            m_PositionWeight = data.FindPropertyRelative("m_PositionWeight");
            m_RotationWeight = data.FindPropertyRelative("m_RotationWeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_ConstrainedObject);

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_OverrideSource);
                using (new EditorGUI.DisabledScope(m_OverrideSource.objectReferenceValue != null))
                {
                    EditorGUILayout.PropertyField(m_OverridePosition);
                    EditorGUILayout.PropertyField(m_OverrideRotation);
                }
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Space);
                EditorGUILayout.PropertyField(m_PositionWeight);
                EditorGUILayout.PropertyField(m_RotationWeight);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/OverrideTransform/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as OverrideTransform;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/OverrideTransform/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as OverrideTransform;
            return BakeUtils.TransferMotionValidate(constraint);
        }
    }

    [BakeParameters(typeof(OverrideTransform))]
    class OverrideTransformBakeParameters : BakeParameters<OverrideTransform>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => false;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, OverrideTransform constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            if (constraint.data.sourceObject != null)
            {
                EditorCurveBindingUtils.CollectTRBindings(rigBuilder.transform, constraint.data.sourceObject, bindings);
            }
            else
            {
                var data = (IOverrideTransformData)constraint.data;
                EditorCurveBindingUtils.CollectVector3Bindings(rigBuilder.transform, constraint, data.positionVector3Property, bindings);
                EditorCurveBindingUtils.CollectVector3Bindings(rigBuilder.transform, constraint, data.rotationVector3Property, bindings);
            }

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, OverrideTransform constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            EditorCurveBindingUtils.CollectTRSBindings(rigBuilder.transform, constraint.data.constrainedObject, bindings);

            return bindings;
        }
    }
}
