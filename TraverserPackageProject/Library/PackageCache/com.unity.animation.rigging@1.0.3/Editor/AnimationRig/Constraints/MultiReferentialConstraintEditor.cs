using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(MultiReferentialConstraint))]
    class MultiReferentialConstraintEditor : Editor
    {
        static readonly GUIContent k_DrivingLabel = new GUIContent("Driving");
        static readonly GUIContent k_ReferenceObjectsLabel = new GUIContent("Reference Objects");

        SerializedProperty m_Weight;
        SerializedProperty m_Driver;
        SerializedProperty m_SourceObjects;

        SerializedProperty m_SourceObjectsToggle;
        List<string> m_DrivingLabels;
        ReorderableList m_ReorderableList;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_Driver = data.FindPropertyRelative("m_Driver");
            m_SourceObjects = data.FindPropertyRelative("m_SourceObjects");

            m_ReorderableList = ReorderableListHelper.Create(serializedObject, m_SourceObjects, false);

            m_DrivingLabels = new List<string>();
            UpdateDrivingList(m_ReorderableList.count);
            if (m_ReorderableList.count == 0)
                ((MultiReferentialConstraint)serializedObject.targetObject).data.sourceObjects.Add(null);

            m_ReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ((MultiReferentialConstraint)serializedObject.targetObject).data.sourceObjects.Add(null);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);

            UpdateDrivingList(m_ReorderableList.count);
            UpdateDrivingLabels();

            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, k_DrivingLabel, m_Driver);
            m_Driver.intValue = EditorGUI.Popup(rect, k_DrivingLabel.text, m_Driver.intValue, m_DrivingLabels.ToArray());
            EditorGUI.EndProperty();

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_ReferenceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                m_ReorderableList.DoLayoutList();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void UpdateDrivingList(int size)
        {
            int count = m_DrivingLabels.Count;
            if (count == size)
                return;

            if (size < count)
                m_DrivingLabels.RemoveRange(size, count - size);
            else if (size > count)
            {
                if (size > m_DrivingLabels.Capacity)
                    m_DrivingLabels.Capacity = size;
                m_DrivingLabels.AddRange(Enumerable.Repeat("", size - count));
            }
        }

        void UpdateDrivingLabels()
        {
            int count = Mathf.Min(m_DrivingLabels.Count, m_SourceObjects.arraySize);
            for (int i = 0; i < count; ++i)
            {
                var element = m_SourceObjects.GetArrayElementAtIndex(i);
                var name = element.objectReferenceValue == null ? "None" : element.objectReferenceValue.name;
                m_DrivingLabels[i] = i + " : " + name;
            }
        }

        [MenuItem("CONTEXT/MultiReferentialConstraint/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var constraint = command.context as MultiReferentialConstraint;
            BakeUtils.TransferMotionToConstraint(constraint);
        }

        [MenuItem("CONTEXT/MultiReferentialConstraint/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as MultiReferentialConstraint;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/MultiReferentialConstraint/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/MultiReferentialConstraint/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as MultiReferentialConstraint;
            return BakeUtils.TransferMotionValidate(constraint);
        }
    }

    [BakeParameters(typeof(MultiReferentialConstraint))]
    class MultiReferentialConstraintBakeParameters : BakeParameters<MultiReferentialConstraint>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => true;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, MultiReferentialConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            var sources = constraint.data.sourceObjects;
            for (int i = 1; i < sources.Count; ++i)
                EditorCurveBindingUtils.CollectTRBindings(rigBuilder.transform, sources[i], bindings);

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, MultiReferentialConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            var transform = constraint.data.sourceObjects[0];
            EditorCurveBindingUtils.CollectTRBindings(rigBuilder.transform, transform, bindings);

            return bindings;
        }
    }
}
