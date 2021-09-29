using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(TwistCorrection))]
    class TwistCorrectionEditor : Editor
    {
        static readonly GUIContent k_TwistNodesLabel = new GUIContent("Twist Nodes");

        SerializedProperty m_Weight;
        SerializedProperty m_Source;
        SerializedProperty m_TwistAxis;
        SerializedProperty m_TwistNodes;

        SerializedProperty m_TwistNodesToggle;
        ReorderableList m_ReorderableList;
        TwistCorrection m_Constraint;
        WeightedTransformArray m_TwistNodesArray;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_TwistNodesToggle = serializedObject.FindProperty("m_TwistNodesGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_Source = data.FindPropertyRelative("m_Source");
            m_TwistAxis = data.FindPropertyRelative("m_TwistAxis");
            m_TwistNodes = data.FindPropertyRelative("m_TwistNodes");

            m_Constraint = (TwistCorrection)serializedObject.targetObject;
            m_TwistNodesArray = m_Constraint.data.twistNodes;

            var dataType = m_Constraint.data.GetType();
            var fieldInfo = dataType.GetField("m_TwistNodes", BindingFlags.NonPublic | BindingFlags.Instance);
            var range = fieldInfo.GetCustomAttribute<RangeAttribute>();

            if (m_TwistNodesArray.Count == 0)
            {
                m_TwistNodesArray.Add(WeightedTransform.Default(0f));
                m_Constraint.data.twistNodes = m_TwistNodesArray;
            }

            m_ReorderableList = WeightedTransformHelper.CreateReorderableList(m_TwistNodes, ref m_TwistNodesArray, range);

            m_ReorderableList.onChangedCallback = (ReorderableList reorderableList) =>
            {
                Undo.RegisterCompleteObjectUndo(m_Constraint, "Edit TwistCorrection");
                m_Constraint.data.twistNodes = (WeightedTransformArray)reorderableList.list;
                if (PrefabUtility.IsPartOfPrefabInstance(m_Constraint))
                    EditorUtility.SetDirty(m_Constraint);
            };

            Undo.undoRedoPerformed += () =>
            {
                m_ReorderableList.list = m_Constraint.data.twistNodes;
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Source);
            EditorGUILayout.PropertyField(m_TwistAxis);

            m_TwistNodesToggle.boolValue = EditorGUILayout.Foldout(m_TwistNodesToggle.boolValue, k_TwistNodesLabel);
            if (m_TwistNodesToggle.boolValue)
            {
                EditorGUI.indentLevel++;

                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/TwistCorrection/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as TwistCorrection;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/TwistCorrection/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as TwistCorrection;
            return BakeUtils.TransferMotionValidate(constraint);
        }
    }

    [BakeParameters(typeof(TwistCorrection))]
    class TwistCorrectionBakeParameters : BakeParameters<TwistCorrection>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => false;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, TwistCorrection constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, constraint.data.sourceObject, bindings);

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, TwistCorrection constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            foreach (var node in constraint.data.twistNodes)
                EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, node.transform, bindings);

            return bindings;
        }
    }
}
