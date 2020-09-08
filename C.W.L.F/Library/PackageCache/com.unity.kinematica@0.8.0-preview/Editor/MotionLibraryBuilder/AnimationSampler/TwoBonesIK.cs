using System;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    struct TwoBonesIK
    {
        int rootIndex;
        int midIndex;
        int tipIndex;

        int[] jointIndices;

        AffineTransform[] localTransforms;
        AffineTransform[] globalTransforms;

        quaternion halfStretchedMidRotation;

        AnimationRig rig;

        float3 RootPos => globalTransforms[rootIndex].t;

        float3 MidPos => globalTransforms[midIndex].t;

        float3 TipPos => globalTransforms[tipIndex].t;

        public static TwoBonesIK CreateTwoBonesIK(AnimationRig rig, int rootJointIndex, int midJointIndex, int tipJointIndex, ref NativeArray<AffineTransform> halfStretchedPose)
        {
            TwoBonesIK twoBonesIK = new TwoBonesIK
            {
                rig = rig
            };

            var avatar = rig.Avatar;

            bool rootJointFound = false;
            bool midJointFound = false;
            bool rootHasParent = false;

            // gather joints starting from tip, moving upward in the hierarchy until root joint is reached
            List<int> jointIndicesList = new List<int>();
            int jointIndex = tipJointIndex;
            while (jointIndex >= 0)
            {
                jointIndicesList.Add(jointIndex);

                if (jointIndex == rootJointIndex)
                {
                    rootJointFound = true;
                    int rootParent = rig.Joints[jointIndex].parentIndex;
                    if (rootParent >= 0)
                    {
                        rootHasParent = true;
                        jointIndicesList.Add(rootParent);
                    }
                    break;
                }

                jointIndex = rig.Joints[jointIndex].parentIndex;
            }

            if (!rootJointFound)
            {
                throw new Exception($"TwoBonesIK error : avatar {avatar.name} joint {rig.Joints[rootJointIndex].name} isn't above {rig.Joints[tipJointIndex].name} in rig hierarchy");
            }

            jointIndicesList.Reverse();
            twoBonesIK.jointIndices = jointIndicesList.ToArray();
            twoBonesIK.localTransforms = new AffineTransform[twoBonesIK.jointIndices.Length];
            twoBonesIK.globalTransforms = new AffineTransform[twoBonesIK.jointIndices.Length];
            twoBonesIK.halfStretchedMidRotation = halfStretchedPose[midJointIndex].q;

            twoBonesIK.rootIndex = rootHasParent ? 1 : 0;
            for (int i = twoBonesIK.rootIndex + 1; i < twoBonesIK.jointIndices.Length; ++i)
            {
                if (twoBonesIK.jointIndices[i] == midJointIndex)
                {
                    twoBonesIK.midIndex = i;
                    midJointFound = true;
                    break;
                }
            }

            if (!midJointFound)
            {
                throw new Exception($"TwoBonesIK error : avatar {avatar.name} joint {rig.Joints[midJointIndex].name} isn't above {rig.Joints[tipJointIndex].name} in rig hierarchy");
            }

            twoBonesIK.tipIndex = twoBonesIK.jointIndices.Length - 1;

            return twoBonesIK;
        }

        public void Solve(ref TransformBuffer localPose, float3 target)
        {
            ReadPose(ref localPose);

            quaternion tipGlobalRotation = globalTransforms[tipIndex].q;

            // 1. Force mid joint rotation to half stretch pose (override animation), forcing rotation will ensure the fold axis will be the same across all animation poses,
            // ensuring IK stability over time. This will also make sure the joint isn't broken by being fold along the wrong axis and in the wrong direction
            SetLocalRotation(midIndex, halfStretchedMidRotation);

            float3 midToRoot = RootPos - MidPos;
            float3 midToTip = TipPos - MidPos;

            // 2. Get current axis and angle rotation between 2 limbs
            quaternion upperToLowerRot = Missing.forRotation(midToRoot, midToTip);
            float upperToLowerAngle;
            float3 upperToLowerAxis = Missing.axisAngle(upperToLowerRot, out upperToLowerAngle);

            // 3. Extend or compress arm in order to reach distance to target
            float desiredAngle = GetTriangleAngle(midToRoot, midToTip, target - RootPos);
            quaternion unfoldRotation = quaternion.AxisAngle(upperToLowerAxis, desiredAngle - upperToLowerAngle);
            SetGlobalRotation(midIndex, math.mul(unfoldRotation, globalTransforms[midIndex].q));

            // 4. Align tip of the arm with target
            quaternion alignRotation = Missing.forRotation(TipPos - RootPos, target - RootPos);
            SetGlobalRotation(rootIndex, math.mul(alignRotation, globalTransforms[rootIndex].q));

            // 5. Preserve tip global rotation
            SetGlobalRotation(tipIndex, tipGlobalRotation);

            WritePose(ref localPose);
        }

        // Get angle in radians in point C
        static float GetTriangleAngle(float3 vA, float3 vB, float3 vC)
        {
            float a = math.length(vA);
            float b = math.length(vB);
            float c = math.length(vC);

            if (Missing.equalEps(a, 0.0f, 0.01f) || Missing.equalEps(b, 0.0f, 0.01f))
            {
                return 0.0f;
            }

            float cosAngle = math.clamp((a * a + b * b - c * c) / (2 * a * b), -1.0f, 1.0f);

            return math.acos(cosAngle);
        }

        void SetLocalRotation(int index, quaternion localRotation)
        {
            localTransforms[index].q = localRotation;

            PropagateGlobalRotations(math.max(index, 1));
        }

        void SetGlobalRotation(int index, quaternion globalRotation)
        {
            globalTransforms[index].q = globalRotation;
            localTransforms[index].q = math.mul(math.inverse(globalTransforms[index - 1].q), globalTransforms[index].q);

            PropagateGlobalRotations(index + 1);
        }

        void PropagateGlobalRotations(int startIndex)
        {
            for (int i = startIndex; i < globalTransforms.Length; ++i)
            {
                globalTransforms[i] = globalTransforms[i - 1] * localTransforms[i];
            }
        }

        void ReadPose(ref TransformBuffer localPose)
        {
            localTransforms[0] = localPose[jointIndices[0]];
            globalTransforms[0] = rig.ComputeGlobalJointTransform(ref localPose, jointIndices[0]);

            for (int i = 1; i < jointIndices.Length; ++i)
            {
                AffineTransform localJoint = localPose[jointIndices[i]];
                localTransforms[i] = localJoint;
                globalTransforms[i] = globalTransforms[i - 1] * localJoint;
            }
        }

        void WritePose(ref TransformBuffer localPose)
        {
            for (int i = rootIndex; i < jointIndices.Length; ++i)
            {
                localPose[jointIndices[i]] = localTransforms[i];
            }
        }
    }
}
