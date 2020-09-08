using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;

using UnityEditor;
using Unity.Collections;

namespace Unity.Kinematica.Editor
{
    internal class AnimationRig
    {
        public struct Joint
        {
            public string name;
            public int parentIndex;
            public AffineTransform localTransform;
        }

        Joint[] joints;
        string[] jointPaths;

        int bodyJointIndex = -1;

        Avatar avatar;

        public Joint[] Joints => joints;

        public string[] JointPaths => jointPaths;

        public int BodyJointIndex => bodyJointIndex;

        public int NumJoints => Joints.Length;

        public Avatar Avatar => avatar;

        public static AnimationRig Create(Avatar avatar)
        {
            return new AnimationRig(avatar);
        }

        public int GetParentJointIndex(int index)
        {
            Assert.IsTrue(index < NumJoints);
            return joints[index].parentIndex;
        }

        public int GetJointIndex(EditorCurveBinding binding)
        {
            for (int i = 0; i < jointPaths.Length; ++i)
            {
                if (jointPaths[i] == binding.path)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetJointIndexFromName(string name)
        {
            for (int i = 0; i < Joints.Length; ++i)
            {
                if (Joints[i].name == name)
                {
                    return i;
                }
            }

            return -1;
        }

        public int GetJointIndexFromPath(string path)
        {
            for (int i = 0; i < Joints.Length; ++i)
            {
                if (jointPaths[i] == path)
                {
                    return i;
                }
            }

            return -1;
        }

        internal AffineTransform ComputeGlobalJointTransform(ref TransformBuffer localPose, int jointIndex)
        {
            AffineTransform globalTransform = localPose[jointIndex];
            while (joints[jointIndex].parentIndex >= 0)
            {
                jointIndex = joints[jointIndex].parentIndex;
                globalTransform = localPose[jointIndex] * globalTransform;
            }

            return globalTransform;
        }

        public Transform[] MapRigOnTransforms(Transform root)
        {
            Transform[] transforms = new Transform[NumJoints];

            transforms[0] = root;

            Transform FindChildRecursive(Transform t, string name)
            {
                Transform child = t.Find(name);
                if (child != null)
                {
                    return child;
                }

                foreach (Transform c in t)
                {
                    child = FindChildRecursive(c, name);
                    if (child != null)
                    {
                        return child;
                    }
                }

                return null;
            }

            for (int i = 1; i < NumJoints; ++i)
            {
                transforms[i] = FindChildRecursive(root, joints[i].name);
            }

            return transforms;
        }

        public NativeArray<int> GenerateParentIndices()
        {
            NativeArray<int> parents = new NativeArray<int>(NumJoints, Allocator.Persistent);

            for (int jointIndex = 0; jointIndex < NumJoints; ++jointIndex)
            {
                parents[jointIndex] = joints[jointIndex].parentIndex;
            }

            return parents;
        }

        AnimationRig(Avatar avatar)
        {
            this.avatar = avatar;

            string assetPath = AssetDatabase.GetAssetPath(avatar);
            GameObject avatarRootObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
            if (avatarRootObject == null)
            {
                throw new Exception($"Avatar {avatar.name} asset not found at path {assetPath}");
            }

            joints = avatar.GetAvatarJoints().ToArray();

            GenerateJointPaths();

            if (avatar.isHuman)
            {
                foreach (HumanBone humanBone in avatar.humanDescription.human)
                {
                    if (humanBone.humanName == "Hips")
                    {
                        bodyJointIndex = GetJointIndexFromName(humanBone.boneName);
                        break;
                    }
                }
            }
            else
            {
                string rootJointName = Utility.GetImporterRootJointName(assetPath);
                if (rootJointName == null)
                {
                    bodyJointIndex = -1;
                }
                else
                {
                    bodyJointIndex = GetJointIndexFromName(rootJointName);
                }
            }
        }

        internal static void CollectJointsRecursive(List<Joint> jointsList, Transform transform, int parentIndex)
        {
            int jointIndex = jointsList.Count;

            jointsList.Add(new Joint()
            {
                name = transform.name,
                parentIndex = parentIndex,
                localTransform = new AffineTransform(
                    transform.localPosition,
                    transform.localRotation)
            });

            foreach (Transform child in transform)
            {
                CollectJointsRecursive(jointsList, child, jointIndex);
            }
        }

        void GenerateJointPaths()
        {
            jointPaths = new string[NumJoints];

            jointPaths[0] = "";

            for (int i = 1; i < NumJoints; ++i)
            {
                int parentIndex = joints[i].parentIndex;
                if (parentIndex < 0 || jointPaths[parentIndex].Length == 0)
                {
                    jointPaths[i] = joints[i].name;
                }
                else
                {
                    jointPaths[i] = jointPaths[parentIndex] + "/" + joints[i].name;
                }
            }
        }
    }
}
