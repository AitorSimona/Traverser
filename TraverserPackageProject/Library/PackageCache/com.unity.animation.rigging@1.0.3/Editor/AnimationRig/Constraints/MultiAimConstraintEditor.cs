using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(MultiAimConstraint))]
    class MultiAimConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");
        static readonly GUIContent k_AimAxisLabel = new GUIContent("Aim Axis");
        static readonly GUIContent k_UpAxisLabel = new GUIContent("Up Axis");
        static readonly GUIContent k_WorldUpAxisLabel = new GUIContent("World Up Axis");
        static readonly GUIContent k_WorldUpType = new GUIContent("World Up Type");
        static readonly GUIContent k_WorldUpObject = new GUIContent("World Up Object");
        static readonly string[] k_AxisLabels = { "X", "-X", "Y", "-Y", "Z", "-Z" };
        static readonly GUIContent k_MaintainOffsetLabel = new GUIContent("Maintain Rotation Offset");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_AimAxis;
        SerializedProperty m_UpAxis;
        SerializedProperty m_WorldUpType;
        SerializedProperty m_WorldUpAxis;
        SerializedProperty m_WorldUpObject;
        SerializedProperty m_SourceObjects;
        SerializedProperty m_MaintainOffset;
        SerializedProperty m_Offset;
        SerializedProperty m_ConstrainedAxes;
        SerializedProperty m_MinLimit;
        SerializedProperty m_MaxLimit;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;
        ReorderableList m_ReorderableList;
        MultiAimConstraint m_Constraint;
        WeightedTransformArray m_SourceObjectsArray;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_ConstrainedObject = data.FindPropertyRelative("m_ConstrainedObject");
            m_AimAxis = data.FindPropertyRelative("m_AimAxis");
            m_UpAxis = data.FindPropertyRelative("m_UpAxis");
            m_WorldUpType = data.FindPropertyRelative("m_WorldUpType");
            m_WorldUpAxis = data.FindPropertyRelative("m_WorldUpAxis");
            m_WorldUpObject = data.FindPropertyRelative("m_WorldUpObject");
            m_SourceObjects = data.FindPropertyRelative("m_SourceObjects");
            m_MaintainOffset = data.FindPropertyRelative("m_MaintainOffset");
            m_Offset = data.FindPropertyRelative("m_Offset");
            m_ConstrainedAxes = data.FindPropertyRelative("m_ConstrainedAxes");
            m_MinLimit = data.FindPropertyRelative("m_MinLimit");
            m_MaxLimit = data.FindPropertyRelative("m_MaxLimit");

            m_Constraint = (MultiAimConstraint)serializedObject.targetObject;
            m_SourceObjectsArray = m_Constraint.data.sourceObjects;

            var dataType = m_Constraint.data.GetType();
            var fieldInfo = dataType.GetField("m_SourceObjects", BindingFlags.NonPublic | BindingFlags.Instance);
            var range = fieldInfo.GetCustomAttribute<RangeAttribute>();

            if (m_SourceObjectsArray.Count == 0)
            {
                m_SourceObjectsArray.Add(WeightedTransform.Default(1f));
                m_Constraint.data.sourceObjects = m_SourceObjectsArray;
            }

            m_ReorderableList = WeightedTransformHelper.CreateReorderableList(m_SourceObjects, ref m_SourceObjectsArray, range);

            m_ReorderableList.onChangedCallback = (ReorderableList reorderableList) =>
            {
                Undo.RegisterCompleteObjectUndo(m_Constraint, "Edit MultiAim");
                m_Constraint.data.sourceObjects = (WeightedTransformArray)reorderableList.list;
                if (PrefabUtility.IsPartOfPrefabInstance(m_Constraint))
                    EditorUtility.SetDirty(m_Constraint);
            };

            Undo.undoRedoPerformed += () =>
            {
                m_ReorderableList.list = m_Constraint.data.sourceObjects;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_ConstrainedObject);

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, k_AimAxisLabel, m_AimAxis);
            m_AimAxis.enumValueIndex = EditorGUI.Popup(rect, m_AimAxis.displayName, m_AimAxis.enumValueIndex, k_AxisLabels);
            EditorGUI.EndProperty();

            rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, k_UpAxisLabel, m_UpAxis);
            m_UpAxis.enumValueIndex = EditorGUI.Popup(rect, m_UpAxis.displayName, m_UpAxis.enumValueIndex, k_AxisLabels);
            EditorGUI.EndProperty();

            EditorGUILayout.PropertyField(m_WorldUpType);

            var worldUpType = (MultiAimConstraintData.WorldUpType)m_WorldUpType.intValue;

            using (new EditorGUI.DisabledGroupScope(worldUpType != MultiAimConstraintData.WorldUpType.ObjectRotationUp && worldUpType != MultiAimConstraintData.WorldUpType.Vector))
            {
                rect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(rect, k_UpAxisLabel, m_WorldUpAxis);
                m_WorldUpAxis.enumValueIndex = EditorGUI.Popup(rect, m_WorldUpAxis.displayName, m_WorldUpAxis.enumValueIndex, k_AxisLabels);
                EditorGUI.EndProperty();
            }

            using (new EditorGUI.DisabledGroupScope(worldUpType != MultiAimConstraintData.WorldUpType.ObjectUp && worldUpType != MultiAimConstraintData.WorldUpType.ObjectRotationUp))
            {
                EditorGUILayout.PropertyField(m_WorldUpObject);
            }

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                // Sync list with sourceObjects.
                m_ReorderableList.list = m_Constraint.data.sourceObjects;

                EditorGUI.indentLevel++;
                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MaintainOffset, k_MaintainOffsetLabel);
                EditorGUILayout.PropertyField(m_Offset);
                EditorGUILayout.PropertyField(m_ConstrainedAxes);
                EditorGUILayout.PropertyField(m_MinLimit);
                EditorGUILayout.PropertyField(m_MaxLimit);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/MultiAimConstraint/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var constraint = command.context as MultiAimConstraint;
            BakeUtils.TransferMotionToConstraint(constraint);
        }

        [MenuItem("CONTEXT/MultiAimConstraint/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as MultiAimConstraint;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/MultiAimConstraint/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/MultiAimConstraint/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as MultiAimConstraint;
            return BakeUtils.TransferMotionValidate(constraint);
        }
    }

    [BakeParameters(typeof(MultiAimConstraint))]
    class MultiAimConstraintBakeParameters : BakeParameters<MultiAimConstraint>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => true;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, MultiAimConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            for (int i = 0; i < constraint.data.sourceObjects.Count; ++i)
            {
                var sourceObject = constraint.data.sourceObjects[i];

                EditorCurveBindingUtils.CollectPositionBindings(rigBuilder.transform, sourceObject.transform, bindings);
                EditorCurveBindingUtils.CollectPropertyBindings(rigBuilder.transform, constraint, ((IMultiAimConstraintData)constraint.data).sourceObjectsProperty + ".m_Item" + i + ".weight", bindings);
            }

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, MultiAimConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, constraint.data.constrainedObject, bindings);

            return bindings;
        }
    }
}
