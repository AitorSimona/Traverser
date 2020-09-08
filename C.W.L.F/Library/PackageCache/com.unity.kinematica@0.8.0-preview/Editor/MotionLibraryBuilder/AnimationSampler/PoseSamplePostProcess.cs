using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal struct PoseSamplePostProcess : IDisposable
    {
        public NativeArray<int>    parentIndices;
        public int bodyJointIndex;
        public bool isRootInTrajectorySpace;

        // Unity animation import root options
        public bool applyRootImportOptions;
        public AffineTransform clipFirstFrameTrajectory;
        public float heightOffset;
        public float rotationOffset;
        public bool lockRootPositionXZ;
        public bool lockRootHeightY;
        public bool lockRootRotation;
        public bool keepOriginalPositionY;
        public bool keepOriginalPositionXZ;
        public bool keepOriginalOrientation;

        internal PoseSamplePostProcess(AnimationRig targetRig, AnimationClip animationClip, AffineTransform clipFirstFrameTrajectory)
        {
            bodyJointIndex = targetRig.BodyJointIndex;
            isRootInTrajectorySpace = animationClip.hasMotionCurves && !animationClip.hasRootCurves;

            if (!isRootInTrajectorySpace && bodyJointIndex < 0)
            {
                throw new Exception($"Animation clip {AssetDatabase.GetAssetPath(animationClip)} requires root bone, please setup root node in avatar {AssetDatabase.GetAssetPath(targetRig.Avatar)}");
            }

            parentIndices = targetRig.GenerateParentIndices();

            ModelImporterClipAnimation clipImporter = Utility.GetImporterFromClip(animationClip);
            applyRootImportOptions = animationClip.hasRootCurves && !animationClip.hasMotionCurves;
            if (applyRootImportOptions && clipImporter != null)
            {
                this.clipFirstFrameTrajectory = clipFirstFrameTrajectory;
                heightOffset = clipImporter.heightOffset;
                rotationOffset = clipImporter.rotationOffset;
                lockRootPositionXZ = clipImporter.lockRootPositionXZ;
                lockRootHeightY = clipImporter.lockRootHeightY;
                lockRootRotation = clipImporter.lockRootRotation;
                keepOriginalPositionY = clipImporter.keepOriginalPositionY;
                keepOriginalPositionXZ = clipImporter.keepOriginalPositionXZ;
                keepOriginalOrientation = clipImporter.keepOriginalOrientation;
            }
            else
            {
                this.clipFirstFrameTrajectory = AffineTransform.identity;
                heightOffset = 0.0f;
                rotationOffset = 0.0f;
                lockRootPositionXZ = false;
                lockRootHeightY = false;
                lockRootRotation = false;
                keepOriginalPositionY = false;
                keepOriginalPositionXZ = false;
                keepOriginalOrientation = false;
            }
        }

        public void Dispose()
        {
            parentIndices.Dispose();
        }

        public PoseSamplePostProcess Clone()
        {
            PoseSamplePostProcess clone = this;
            clone.parentIndices = new NativeArray<int>(parentIndices, Allocator.TempJob);
            return clone;
        }

        public void Apply(NativeSlice<AffineTransform> localPose)
        {
            if (applyRootImportOptions)
            {
                AffineTransform trajectory = localPose[0];
                ApplyRootImportOptions(ref trajectory);
                localPose[0] = trajectory;
            }

            if (!isRootInTrajectorySpace)
            {
                //
                // Body and trajectory transform are both in world space.
                // Adjust body transform to be relative to trajectory transform.
                //

                // There can be joints in the hierarchy between the trajectory (first joint) and the body joint. We accumulate the transforms of those in-between joints
                // in order to compute correctly the transform of the body joint relative to the trajectory. i.e.we compute the new body transform `body'` relative to trajectory so that
                // trajectory * bodyOffset * body' = bodyOffset * body

                AffineTransform bodyOffset = AffineTransform.identity;

                int jointIndex = parentIndices[bodyJointIndex];
                while (jointIndex > 0)
                {
                    bodyOffset = localPose[jointIndex] * bodyOffset;
                    jointIndex = parentIndices[jointIndex];
                }

                localPose[bodyJointIndex] = (localPose[0] * bodyOffset).inverseTimes(bodyOffset * localPose[bodyJointIndex]);
            }
        }

        void ApplyRootImportOptions(ref AffineTransform trajectory)
        {
            trajectory = trajectory.alignHorizontally();

            AffineTransform offset = AffineTransform.identity;

            offset.t.y = heightOffset;
            offset.q = quaternion.RotateY(math.radians(rotationOffset));

            if (lockRootPositionXZ)
            {
                trajectory.t.x = clipFirstFrameTrajectory.t.x;
                trajectory.t.z = clipFirstFrameTrajectory.t.z;
            }

            if (lockRootHeightY)
            {
                trajectory.t.y = clipFirstFrameTrajectory.t.y;
            }

            if (lockRootRotation)
            {
                trajectory.q = clipFirstFrameTrajectory.q;
            }

            if (keepOriginalPositionY)
            {
                offset.t.y -= clipFirstFrameTrajectory.t.y;
            }

            if (keepOriginalPositionXZ)
            {
                offset.t.x -= clipFirstFrameTrajectory.t.x;
                offset.t.z -= clipFirstFrameTrajectory.t.z;
            }

            if (keepOriginalOrientation)
            {
                offset.q = math.mul(offset.q, math.conjugate(clipFirstFrameTrajectory.q));
            }

            trajectory.t = offset.t + trajectory.t;
            trajectory.q = math.mul(offset.q, trajectory.q);
        }
    }
}
