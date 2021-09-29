using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(MultiRotationConstraint))]
    class MultiRotationConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");
        static readonly GUIContent k_MaintainOffsetLabel = new GUIContent("Maintain Rotation Offset");

        SerializedProperty m_Weight;
        SerializedProperty m_ConstrainedObject;
        SerializedProperty m_ConstrainedAxes;
        SerializedProperty m_SourceObjects;
        SerializedProperty m_MaintainOffset;
        SerializedProperty m_Offset;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;
        ReorderableList m_ReorderableList;
        MultiRotationConstraint m_Constraint;
        WeightedTransformArray m_SourceObjectsArray;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_ConstrainedObject = data.FindPropertyRelative("m_ConstrainedObject");
            m_ConstrainedAxes = data.FindPropertyRelative("m_ConstrainedAxes");
            m_SourceObjects = data.FindPropertyRelative("m_SourceObjects");
            m_MaintainOffset = data.FindPropertyRelative("m_MaintainOffset");
            m_Offset = data.FindPropertyRelative("m_Offset");

            m_Constraint = (MultiRotationConstraint)serializedObject.targetObject;
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
                Undo.RegisterCompleteObjectUndo(m_Constraint, "Edit MultiRotation");
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
            EditorGUILayout.PropertyField(m_ConstrainedAxes);

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
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/MultiRotationConstraint/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var constraint = command.context as MultiRotationConstraint;

            var axesMask = new Vector3(
                System.Convert.ToSingle(constraint.data.constrainedXAxis),
                System.Convert.ToSingle(constraint.data.constrainedYAxis),
                System.Convert.ToSingle(constraint.data.constrainedZAxis));

            if (Vector3.Dot(axesMask, axesMask) < 3f)
            {
                Debug.LogWarning("Multi-Rotation constraint with one or more Constrained Axes toggled off may lose precision when transferring its motion to constraint.");
            }

            BakeUtils.TransferMotionToConstraint(constraint);
        }

        [MenuItem("CONTEXT/MultiRotationConstraint/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as MultiRotationConstraint;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/MultiRotationConstraint/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/MultiRotationConstraint/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as MultiRotationConstraint;
            return BakeUtils.TransferMotionValidate(constraint);
        }
    }

    [BakeParameters(typeof(MultiRotationConstraint))]
    class MultiRotationConstraintBakeParameters : BakeParameters<MultiRotationConstraint>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => true;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, MultiRotationConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            for (int i = 0; i < constraint.data.sourceObjects.Count; ++i)
            {
                var sourceObject = constraint.data.sourceObjects[i];

                EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, sourceObject.transform, bindings);
                EditorCurveBindingUtils.CollectPropertyBindings(rigBuilder.transform, constraint, ((IMultiRotationConstraintData)constraint.data).sourceObjectsProperty + ".m_Item" + i + ".weight", bindings);
            }

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, MultiRotationConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, constraint.data.constrainedObject, bindings);

            return bindings;
        }
    }
}
