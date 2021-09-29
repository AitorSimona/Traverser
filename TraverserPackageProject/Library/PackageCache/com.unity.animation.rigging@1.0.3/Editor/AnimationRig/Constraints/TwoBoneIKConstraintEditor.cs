using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(TwoBoneIKConstraint))]
    class TwoBoneIKConstraintEditor : Editor
    {
        static readonly GUIContent k_SourceObjectsLabel = new GUIContent("Source Objects");
        static readonly GUIContent k_SettingsLabel = new GUIContent("Settings");
        static readonly GUIContent k_MaintainTargetOffsetLabel = new GUIContent("Maintain Target Offset");

        SerializedProperty m_Weight;
        SerializedProperty m_Root;
        SerializedProperty m_Mid;
        SerializedProperty m_Tip;
        SerializedProperty m_Target;
        SerializedProperty m_Hint;
        SerializedProperty m_TargetPositionWeight;
        SerializedProperty m_TargetRotationWeight;
        SerializedProperty m_HintWeight;
        SerializedProperty m_MaintainTargetPositionOffset;
        SerializedProperty m_MaintainTargetRotationOffset;

        SerializedProperty m_SourceObjectsToggle;
        SerializedProperty m_SettingsToggle;

        void OnEnable()
        {
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_SourceObjectsToggle = serializedObject.FindProperty("m_SourceObjectsGUIToggle");
            m_SettingsToggle = serializedObject.FindProperty("m_SettingsGUIToggle");

            var data = serializedObject.FindProperty("m_Data");
            m_Root = data.FindPropertyRelative("m_Root");
            m_Mid = data.FindPropertyRelative("m_Mid");
            m_Tip = data.FindPropertyRelative("m_Tip");
            m_Target = data.FindPropertyRelative("m_Target");
            m_Hint = data.FindPropertyRelative("m_Hint");
            m_TargetPositionWeight = data.FindPropertyRelative("m_TargetPositionWeight");
            m_TargetRotationWeight = data.FindPropertyRelative("m_TargetRotationWeight");
            m_HintWeight = data.FindPropertyRelative("m_HintWeight");
            m_MaintainTargetPositionOffset = data.FindPropertyRelative("m_MaintainTargetPositionOffset");
            m_MaintainTargetRotationOffset = data.FindPropertyRelative("m_MaintainTargetRotationOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Root);
            EditorGUILayout.PropertyField(m_Mid);
            EditorGUILayout.PropertyField(m_Tip);

            m_SourceObjectsToggle.boolValue = EditorGUILayout.Foldout(m_SourceObjectsToggle.boolValue, k_SourceObjectsLabel);
            if (m_SourceObjectsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Target);
                EditorGUILayout.PropertyField(m_Hint);
                EditorGUI.indentLevel--;
            }

            m_SettingsToggle.boolValue = EditorGUILayout.Foldout(m_SettingsToggle.boolValue, k_SettingsLabel);
            if (m_SettingsToggle.boolValue)
            {
                EditorGUI.indentLevel++;
                MaintainOffsetHelper.DoDropdown(k_MaintainTargetOffsetLabel, m_MaintainTargetPositionOffset, m_MaintainTargetRotationOffset);
                EditorGUILayout.PropertyField(m_TargetPositionWeight);
                EditorGUILayout.PropertyField(m_TargetRotationWeight);
                EditorGUILayout.PropertyField(m_HintWeight);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/TwoBoneIKConstraint/Transfer motion to constraint", false, 611)]
        public static void TransferMotionToConstraint(MenuCommand command)
        {
            var constraint = command.context as TwoBoneIKConstraint;
            BakeUtils.TransferMotionToConstraint(constraint);
        }

        [MenuItem("CONTEXT/TwoBoneIKConstraint/Transfer motion to skeleton", false, 612)]
        public static void TransferMotionToSkeleton(MenuCommand command)
        {
            var constraint = command.context as TwoBoneIKConstraint;
            BakeUtils.TransferMotionToSkeleton(constraint);
        }

        [MenuItem("CONTEXT/TwoBoneIKConstraint/Transfer motion to constraint", true)]
        [MenuItem("CONTEXT/TwoBoneIKConstraint/Transfer motion to skeleton", true)]
        public static bool TransferMotionValidate(MenuCommand command)
        {
            var constraint = command.context as TwoBoneIKConstraint;
            return BakeUtils.TransferMotionValidate(constraint);
        }

        [MenuItem("CONTEXT/TwoBoneIKConstraint/Auto Setup from Tip Transform", false, 631)]
        public static void TwoBoneIKAutoSetup(MenuCommand command)
        {
            var constraint = command.context as TwoBoneIKConstraint;
            var tip = constraint.data.tip;
            var animator = constraint.GetComponentInParent<Animator>()?.transform;
            var dirty = false;

            if (!tip)
            {
                var selection = Selection.transforms;
                var constraintInSelection = false;

                // Take transform from selection that is part of the animator hierarchy & not the constraint transform.
                for (int i = 0; i < selection.Length; i++)
                {
                    if (selection[i].IsChildOf(animator))
                    {
                        if (selection[i] != constraint.transform)
                        {
                            tip = selection[i];
                            break;
                        }
                        else
                        {
                            constraintInSelection = true;
                        }
                    }
                }

                // If the constraint itself was selected and we haven't found anything use that.
                if (!tip && constraintInSelection)
                    tip = constraint.transform;

                // If there is still no tip return.
                if (!tip)
                {
                    Debug.LogWarning("Please provide a tip before running auto setup!");
                    return;
                }
                else
                {
                    Undo.RecordObject(constraint, "Setup tip bone from use selection");
                    constraint.data.tip = tip;
                    dirty = true;
                }
            }

            if (!constraint.data.mid)
            {
                Undo.RecordObject(constraint, "Setup mid bone for TwoBoneIK");
                constraint.data.mid = tip.parent;
                dirty = true;
            }

            if (!constraint.data.root)
            {
                Undo.RecordObject(constraint, "Setup root bone for TwoBoneIK");
                constraint.data.root = tip.parent.parent;
                dirty = true;
            }

            if (!constraint.data.target)
            {
                var target = constraint.transform.Find(constraint.gameObject.name + "_target");
                if (target == null)
                {
                    var t = new GameObject();
                    Undo.RegisterCreatedObjectUndo(t, "Created target");
                    t.name = constraint.gameObject.name + "_target";
                    t.transform.localScale = .1f * t.transform.localScale;
                    Undo.SetTransformParent(t.transform, constraint.transform, "Set new parent");
                    target = t.transform;
                }
                constraint.data.target = target;
                dirty = true;
            }

            if (!constraint.data.hint)
            {
                var hint = constraint.transform.Find(constraint.gameObject.name + "_hint");
                if (hint == null)
                {
                    var t = new GameObject();
                    Undo.RegisterCreatedObjectUndo(t, "Created hint");
                    t.name = constraint.gameObject.name + "_hint";
                    t.transform.localScale = .1f * t.transform.localScale;
                    Undo.SetTransformParent(t.transform, constraint.transform, "Set new parent");
                    hint = t.transform;
                }
                constraint.data.hint = hint;
                dirty = true;
            }

            if (dirty && PrefabUtility.IsPartOfPrefabInstance(constraint))
                EditorUtility.SetDirty(constraint);
        }
    }

    [BakeParameters(typeof(TwoBoneIKConstraint))]
    class TwoBoneIKConstraintBakeParameters : BakeParameters<TwoBoneIKConstraint>
    {
        public override bool canBakeToSkeleton => true;
        public override bool canBakeToConstraint => true;

        public override IEnumerable<EditorCurveBinding> GetSourceCurveBindings(RigBuilder rigBuilder, TwoBoneIKConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            EditorCurveBindingUtils.CollectTRBindings(rigBuilder.transform, constraint.data.target, bindings);

            if (constraint.data.hint != null)
                EditorCurveBindingUtils.CollectPositionBindings(rigBuilder.transform, constraint.data.hint, bindings);

            return bindings;
        }

        public override IEnumerable<EditorCurveBinding> GetConstrainedCurveBindings(RigBuilder rigBuilder, TwoBoneIKConstraint constraint)
        {
            var bindings = new List<EditorCurveBinding>();

            var root = constraint.data.root;
            var mid = constraint.data.mid;
            var tip = constraint.data.tip;

            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, root, bindings);
            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, mid, bindings);
            EditorCurveBindingUtils.CollectRotationBindings(rigBuilder.transform, tip, bindings);

            return bindings;
        }
    }
}
