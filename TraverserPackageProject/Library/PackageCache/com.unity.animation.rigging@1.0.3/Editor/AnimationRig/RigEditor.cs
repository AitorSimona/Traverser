using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(Rig))]
    [CanEditMultipleObjects]
    class RigEditor : Editor
    {
        SerializedProperty m_Weight;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
        }

        public override void OnInspectorGUI()
        {
            bool isEditingMultipleObjects = targets.Length > 1;

            serializedObject.Update();
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(m_Weight);
            serializedObject.ApplyModifiedProperties();

            if (!isEditingMultipleObjects)
            {
                var rig = target as Rig;
                var rigBuilder = rig.GetComponentInParent<RigBuilder>();

                if (rigBuilder == null)
                {
                    EditorGUILayout.HelpBox("Rig component is not child of a GameObject with a RigBuilder component.", MessageType.Warning, true);
                }
                else
                {
                    var inRigBuilder = false;
                    var layers = rigBuilder.layers;

                    for (int i = 0; i < layers.Count; ++i)
                    {
                        if (layers[i].rig == rig && layers[i].active)
                        {
                            inRigBuilder = true;
                            break;
                        }
                    }

                    if (!inRigBuilder)
                        EditorGUILayout.HelpBox("Rig component is not referenced or not active in the RigBuilder component.", MessageType.Warning, true);
                }
            }
        }

        [MenuItem("CONTEXT/Rig/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var rig = command.context as Rig;
            BakeUtils.TransferMotionToConstraint(rig);
        }

        [MenuItem("CONTEXT/Rig/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var rig = command.context as Rig;
            BakeUtils.TransferMotionToSkeleton(rig);
        }

        [MenuItem("CONTEXT/Rig/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/Rig/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var rig = command.context as Rig;
            return BakeUtils.TransferMotionValidate(rig);
        }
    }
}
