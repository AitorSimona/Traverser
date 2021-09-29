using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    internal static class AnimationRiggingEditorUtils
    {
        public static void RigSetup(Transform transform)
        {
            var rigBuilder = transform.GetComponent<RigBuilder>();

            if (rigBuilder == null)
                rigBuilder = Undo.AddComponent<RigBuilder>(transform.gameObject);
            else
                Undo.RecordObject(rigBuilder, "Rig Builder Component Added.");

            var name = "Rig";
            var cnt = 1;
            while (rigBuilder.transform.Find(string.Format("{0} {1}", name, cnt)) != null)
            {
                cnt++;
            }
            name = string.Format("{0} {1}", name, cnt);
            var rigGameObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(rigGameObject, name);
            rigGameObject.transform.SetParent(rigBuilder.transform);

            var rig = Undo.AddComponent<Rig>(rigGameObject);
            rigBuilder.layers.Add(new RigLayer(rig));

            if (PrefabUtility.IsPartOfPrefabInstance(rigBuilder))
                EditorUtility.SetDirty(rigBuilder);
        }

        public static void BoneRendererSetup(Transform transform)
        {
            var boneRenderer = transform.GetComponent<BoneRenderer>();
            if (boneRenderer == null)
                boneRenderer = Undo.AddComponent<BoneRenderer>(transform.gameObject);
            else
                Undo.RecordObject(boneRenderer, "Bone renderer setup.");

            var animator = transform.GetComponent<Animator>();
            var renderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            var bones = new List<Transform>();
            if (animator != null && renderers != null && renderers.Length > 0)
            {
                for (int i = 0; i < renderers.Length; ++i)
                {
                    var renderer = renderers[i];
                    for (int j = 0; j < renderer.bones.Length; ++j)
                    {
                        var bone = renderer.bones[j];
                        if (!bones.Contains(bone))
                        {
                            bones.Add(bone);

                            for (int k = 0; k < bone.childCount; k++)
                            {
                                if (!bones.Contains(bone.GetChild(k)))
                                    bones.Add(bone.GetChild(k));
                            }
                        }
                    }
                }
            }
            else
            {
                bones.AddRange(transform.GetComponentsInChildren<Transform>());
            }

            boneRenderer.transforms = bones.ToArray();

            if (PrefabUtility.IsPartOfPrefabInstance(boneRenderer))
                EditorUtility.SetDirty(boneRenderer);
        }

        public static void RestoreBindPose(Transform transform)
        {
            var animator = transform.GetComponentInParent<Animator>();
            var root = (animator) ? animator.transform : transform;
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>();

            if (renderers.Length == 0)
            {
                Debug.LogError(
                    string.Format(
                        "Could not restore bind pose because no SkinnedMeshRenderers " +
                        "were found  on {0} or any of its children.", root.name));
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Restore bind pose");

            var bones = new Dictionary<Transform, Matrix4x4>();
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.bones.Length; ++i)
                {
                    if (!bones.ContainsKey(renderer.bones[i]))
                        bones.Add(renderer.bones[i], renderer.sharedMesh.bindposes[i]);
                }
            }

            var transforms = transform.GetComponentsInChildren<Transform>();
            var restoredPose = false;
            foreach (var t in transforms)
            {
                if (!bones.ContainsKey(t))
                    continue;

                // The root bone is the only bone in the skeleton
                // hierarchy that does not have a parent bone.
                var isRootBone = !bones.ContainsKey(t.parent);

                var matrix = bones[t];
                var wMatrix = matrix.inverse;

                if (!isRootBone)
                {
                    if (t.parent)
                        matrix *= bones[t.parent].inverse;
                    matrix = matrix.inverse;

                    t.localScale = new Vector3(
                        matrix.GetColumn(0).magnitude,
                        matrix.GetColumn(1).magnitude,
                        matrix.GetColumn(2).magnitude
                        );
                    t.localPosition = matrix.MultiplyPoint(Vector3.zero);
                }
                t.rotation = wMatrix.rotation;

                restoredPose = true;
            }

            if (!restoredPose)
            {
                Debug.LogWarning(
                    string.Format(
                        "No valid bindpose(s) have been found for the selected transform: {0}.",
                        transform.name));
            }
        }
    }
}
