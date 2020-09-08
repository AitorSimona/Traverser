#if UNITY_2020_1_OR_NEWER
#define USE_HUMAN_POSE_HANDLER_RETARGETING
#endif

using System;

using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica.Editor
{
    internal abstract class AnimationRetargeter : IDisposable
    {
        protected AnimationRig sourceRig;

        public AnimationRig SourceAvatarRig
        {
            get => sourceRig;
        }

        public AnimationRetargeter(AnimationRig sourceRig)
        {
            this.sourceRig = sourceRig;
        }

        public virtual void Dispose()
        {
        }

        public abstract void RetargetPose(ref TransformBuffer sourceTransformBuffer, ref TransformBuffer targetTransformBuffer);

        public abstract AffineTransform RetargetTrajectory(AffineTransform sourceTrajectory);

        public virtual int GetSourceToTargetJointIndex(int sourceJointIndex)
        {
            return -1;
        }

        public virtual float GetSourceToTargetScale()
        {
            return 1.0f;
        }
    }

    internal class HumanoidAnimationRetargeter : AnimationRetargeter
    {
        HumanPoseHandler sourceHumanPoseHandler;
        HumanPoseHandler targetHumanPoseHandler;

        AnimationRig targetRig = null;

        float sourceToTargetScale = 0f;

        NativeArray<AffineTransform> sourceAvatarPose;
        NativeArray<AffineTransform> targetAvatarPose;

        HumanPose humanPose;

        bool enableIK;

        LimbPairIK feetIK;
        LimbPairIK handsIK;

        NativeArray<int> sourceToTargetJointIndices;

        float pelvisOffset;

        internal struct LimbPairIK
        {
            public int leftLimbSourceJointIndex;
            public int rightLimbSourceJointIndex;

            public TwoBonesIK leftLimbChainIK;
            public TwoBonesIK rightLimbChainIK;

            public void Solve(ref TransformBuffer targetTransformBuffer, float3 leftTargetPosition, float3 rightTargetPosition)
            {
                leftLimbChainIK.Solve(ref targetTransformBuffer, leftTargetPosition);
                rightLimbChainIK.Solve(ref targetTransformBuffer, rightTargetPosition);
            }
        }

        public HumanoidAnimationRetargeter(AnimationRig sourceRig, AnimationRig targetRig) : base(sourceRig)
        {
#if USE_HUMAN_POSE_HANDLER_RETARGETING
            var sourceAvatar = sourceRig.Avatar;
            var targetAvatar = targetRig.Avatar;

            sourceHumanPoseHandler = new HumanPoseHandler(sourceRig.Avatar, sourceRig.JointPaths);
            targetHumanPoseHandler = new HumanPoseHandler(targetRig.Avatar, targetRig.JointPaths);

            this.targetRig = targetRig;
            sourceToTargetScale = GetAvatarHumanScale(targetAvatar) / GetAvatarHumanScale(sourceAvatar);

            pelvisOffset = GetAvatarHumanScale(targetAvatar) * 0.05f; // lower pelvis of 5 percent to avoid overextension

            sourceAvatarPose = new NativeArray<AffineTransform>(sourceRig.NumJoints, Allocator.Persistent);
            targetAvatarPose = new NativeArray<AffineTransform>(targetRig.NumJoints, Allocator.Persistent);
            humanPose = new HumanPose();

            try
            {
                // Compute "half stretched" pose where all humanoid joints are folded at 90 degrees angle (corresponding to the muscle value being 0)
                // and store it into targetAvatarPose. Those joints at 90 degrees will serve at starting mid joint rotations for IK in order to ensure stability
                int affineTransformSize = UnsafeUtility.SizeOf<AffineTransform>();
                targetHumanPoseHandler.GetInternalHumanPose(ref humanPose);
                for (int i = 0; i < humanPose.muscles.Length; ++i)
                {
                    humanPose.muscles[i] = 0.0f;
                }
                targetHumanPoseHandler.SetInternalHumanPose(ref humanPose);
                targetHumanPoseHandler.GetInternalAvatarPose(targetAvatarPose.Reinterpret<float>(affineTransformSize));

                CreateLimbPairIK("UpperLeg", "LowerLeg", "Foot", out feetIK);
                CreateLimbPairIK("UpperArm", "LowerArm", "Hand", out handsIK);

                enableIK = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                enableIK = false;
            }

            sourceToTargetJointIndices = new NativeArray<int>(sourceRig.NumJoints, Allocator.Persistent);
            for (int i = 0; i < sourceRig.NumJoints; ++i)
            {
                sourceToTargetJointIndices[i] = -1;
            }

            for (int i = 0; i < sourceAvatar.humanDescription.human.Length; ++i)
            {
                int sourceJointIndex = sourceRig.GetJointIndexFromName(sourceAvatar.humanDescription.human[i].boneName);
                if (sourceJointIndex >= 0)
                {
                    int targetJointIndex = -1;
                    try
                    {
                        targetJointIndex = GetJointIndexFromHumanBone(targetRig, sourceAvatar.humanDescription.human[i].humanName);
                    }
                    catch (Exception)
                    {}

                    sourceToTargetJointIndices[sourceJointIndex] = targetJointIndex;
                }
            }
#endif
        }

        public override void Dispose()
        {
            base.Dispose();

#if USE_HUMAN_POSE_HANDLER_RETARGETING
            sourceAvatarPose.Dispose();
            targetAvatarPose.Dispose();

            sourceToTargetJointIndices.Dispose();
#endif
        }

        public override void RetargetPose(ref TransformBuffer sourceTransformBuffer, ref TransformBuffer targetTransformBuffer)
        {
#if USE_HUMAN_POSE_HANDLER_RETARGETING
            int affineTransformSize = UnsafeUtility.SizeOf<AffineTransform>();

            AffineTransform leftFootSource = AffineTransform.identity;
            AffineTransform rightFootSource = AffineTransform.identity;
            AffineTransform leftHandSource = AffineTransform.identity;
            AffineTransform rightHandSource = AffineTransform.identity;

            // 1. Compute IK target positions from source
            if (enableIK)
            {
                leftFootSource = sourceRig.ComputeGlobalJointTransform(ref sourceTransformBuffer, feetIK.leftLimbSourceJointIndex);
                rightFootSource = sourceRig.ComputeGlobalJointTransform(ref sourceTransformBuffer, feetIK.rightLimbSourceJointIndex);

                leftHandSource = sourceRig.ComputeGlobalJointTransform(ref sourceTransformBuffer, handsIK.leftLimbSourceJointIndex);
                rightHandSource = sourceRig.ComputeGlobalJointTransform(ref sourceTransformBuffer, handsIK.rightLimbSourceJointIndex);
            }

            // 2. Convert source transform buffer (joint transforms) to human pose (universal muscles values)
            AffineTransform rootTransform = sourceTransformBuffer[0];
            sourceTransformBuffer[0] = AffineTransform.identity;

            for (int i = 0; i < sourceTransformBuffer.Length; ++i)
            {
                sourceAvatarPose[i] = sourceTransformBuffer[i];
            }
            sourceHumanPoseHandler.SetInternalAvatarPose(sourceAvatarPose.Reinterpret<float>(affineTransformSize));
            sourceHumanPoseHandler.GetInternalHumanPose(ref humanPose);

            // 3. Convert human poses (muscles) to target transform buffer, retargeting
            targetHumanPoseHandler.SetInternalHumanPose(ref humanPose);
            targetHumanPoseHandler.GetInternalAvatarPose(targetAvatarPose.Reinterpret<float>(affineTransformSize));
            for (int i = 0; i < targetTransformBuffer.Length; ++i)
            {
                targetTransformBuffer[i] = targetAvatarPose[i];
            }

            // 4. Scale root motion and slightly lower pelvis
            targetTransformBuffer[0] = rootTransform * sourceToTargetScale;
            targetTransformBuffer[targetRig.BodyJointIndex] = new AffineTransform(targetTransformBuffer[targetRig.BodyJointIndex].t - new float3(0.0f, pelvisOffset, 0.0f), targetTransformBuffer[targetRig.BodyJointIndex].q);

            // 5. Solve feet & hands IK
            if (enableIK)
            {
                feetIK.Solve(ref targetTransformBuffer, leftFootSource.t * sourceToTargetScale, rightFootSource.t * sourceToTargetScale);
                handsIK.Solve(ref targetTransformBuffer, leftHandSource.t * sourceToTargetScale, rightHandSource.t * sourceToTargetScale);
            }
#endif
        }

        public override AffineTransform RetargetTrajectory(AffineTransform sourceTrajectory)
        {
            return sourceTrajectory * sourceToTargetScale;
        }

        public override int GetSourceToTargetJointIndex(int sourceJointIndex)
        {
#if USE_HUMAN_POSE_HANDLER_RETARGETING
            return sourceToTargetJointIndices[sourceJointIndex];
#else
            return -1;
#endif
        }

        public override float GetSourceToTargetScale()
        {
            return sourceToTargetScale;
        }

        int SetupTwoBonesIK(string rootJointName, string midJointName, string tipJointName, out TwoBonesIK twoBonesIK)
        {
            int sourceJointIndex = GetJointIndexFromHumanBone(sourceRig, tipJointName);

            twoBonesIK = TwoBonesIK.CreateTwoBonesIK(targetRig,
                GetJointIndexFromHumanBone(targetRig, rootJointName),
                GetJointIndexFromHumanBone(targetRig, midJointName),
                GetJointIndexFromHumanBone(targetRig, tipJointName),
                ref targetAvatarPose);

            return sourceJointIndex;
        }

        void CreateLimbPairIK(string rootJointBaseName, string midJointBaseName, string tipJointBaseName, out LimbPairIK limbPairIK)
        {
            limbPairIK = new LimbPairIK();

            limbPairIK.leftLimbSourceJointIndex = SetupTwoBonesIK("Left" + rootJointBaseName, "Left" + midJointBaseName, "Left" + tipJointBaseName, out limbPairIK.leftLimbChainIK);
            limbPairIK.rightLimbSourceJointIndex = SetupTwoBonesIK("Right" + rootJointBaseName, "Right" + midJointBaseName, "Right" + tipJointBaseName, out limbPairIK.rightLimbChainIK);
        }

        static int GetJointIndexFromHumanBone(AnimationRig rig, string humanBoneName)
        {
            var avatar = rig.Avatar;

            for (int i = 0; i < avatar.humanDescription.human.Length; ++i)
            {
                ref HumanBone humanBone = ref avatar.humanDescription.human[i];
                if (humanBone.humanName == humanBoneName)
                {
                    int jointIndex = rig.GetJointIndexFromName(humanBone.boneName);
                    if (jointIndex < 0)
                    {
                        throw new Exception($"Avatar {avatar.name} joint {humanBone.boneName} not found");
                    }
                    return jointIndex;
                }
            }

            throw new Exception($"Avatar {avatar.name} human bone {humanBoneName} not found");
        }

        static float GetAvatarHumanScale(Avatar avatar)
        {
            GameObject avatarRoot = GetAvatarRoot(avatar);
            Animator animator = avatarRoot.GetComponent<Animator>();
            return animator.humanScale > 0.0f ? animator.humanScale : 1.0f;
        }

        static GameObject GetAvatarRoot(Avatar avatar)
        {
            string assetPath = AssetDatabase.GetAssetPath(avatar);
            GameObject avatarRootObject = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
            if (avatarRootObject == null)
            {
                throw new Exception($"Avatar {avatar.name} asset not found at path {assetPath}");
            }

            return avatarRootObject;
        }
    }
}
