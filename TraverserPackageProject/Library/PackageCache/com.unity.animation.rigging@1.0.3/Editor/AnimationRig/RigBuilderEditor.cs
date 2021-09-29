using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditorInternal;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(RigBuilder))]
    class RigBuilderEditor : Editor
    {
        static readonly GUIContent k_RigLabel = new GUIContent("Rig Layers");

        SerializedProperty m_Rigs;
        ReorderableList m_ReorderableList;

        void OnEnable()
        {
            m_Rigs = serializedObject.FindProperty("m_RigLayers");
            m_ReorderableList = ReorderableListHelper.Create(serializedObject, m_Rigs, true, true);
            if (m_ReorderableList.count == 0)
                ((RigBuilder)serializedObject.targetObject).layers.Add(new RigLayer(null));

            m_ReorderableList.drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, k_RigLabel);

            m_ReorderableList.onAddCallback = (ReorderableList list) =>
            {
                ((RigBuilder)(serializedObject.targetObject)).layers.Add(new RigLayer(null, true));
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Separator();
            m_ReorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/RigBuilder/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var rigBuilder = command.context as RigBuilder;
            BakeUtils.TransferMotionToConstraint(rigBuilder);
        }

        [MenuItem("CONTEXT/RigBuilder/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var rigBuilder = command.context as RigBuilder;
            BakeUtils.TransferMotionToSkeleton(rigBuilder);
        }

        [MenuItem("CONTEXT/RigBuilder/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/RigBuilder/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var rigBuilder = command.context as RigBuilder;
            return BakeUtils.TransferMotionValidate(rigBuilder);
        }

    }
}
