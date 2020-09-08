using System;

using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using static AnimationCurveBake.ThreadSafe;
using static AnimationCurveBake;

namespace Unity.Kinematica.Editor
{
    internal partial class AnimationSampler : IDisposable
    {
        AnimationRig targetRig;
        AnimationClip animationClip;

        KeyframeAnimation   editorAnimation;
        KeyframeAnimation?   bakedAnimation;

        PoseSamplePostProcess poseSamplePostProcess;

        public AnimationClip AnimationClip => animationClip;

        public AnimationRig TargetRig => targetRig;

        public PoseSamplePostProcess PoseSamplePostProcess => poseSamplePostProcess;

        internal AnimationSampler(AnimationRig targetRig, AnimationClip animationClip)
        {
            this.targetRig = targetRig;

            this.animationClip = animationClip;

            int numJoints = targetRig.NumJoints;

            editorAnimation = KeyframeAnimation.Create(this, animationClip);
            bakedAnimation = null;

            try
            {
                poseSamplePostProcess = new PoseSamplePostProcess(targetRig, animationClip, editorAnimation.JointSamplers[0][0]);
            }
            catch (Exception e)
            {
                editorAnimation.Dispose();
                throw e;
            }
        }

        public void Dispose()
        {
            editorAnimation.Dispose();
            bakedAnimation?.Dispose();

            poseSamplePostProcess.Dispose();
        }

        public TransformBuffer.Memory SamplePose(float sampleTimeInSeconds)
        {
            int numJoints = editorAnimation.JointSamplers.Length;

            TransformBuffer.Memory buffer = TransformBuffer.Memory.Allocate(numJoints, Allocator.Temp);
            ref TransformBuffer pose = ref buffer.Ref;

            for (int jointIndex = 0; jointIndex < numJoints; ++jointIndex)
            {
                pose[jointIndex] = editorAnimation.JointSamplers[jointIndex].Evaluate(sampleTimeInSeconds);
            }

            poseSamplePostProcess.Apply(pose.transforms);

            return buffer;
        }

        public AffineTransform SampleTrajectory(float sampleTimeInSeconds)
        {
            return editorAnimation.JointSamplers[0].Evaluate(sampleTimeInSeconds);
        }

        internal void AllocateBakedAnimation(float sampleRate)
        {
            if (bakedAnimation == null)
            {
                bakedAnimation = editorAnimation.AllocateCopyAtFixedSampleRate(sampleRate);
            }
        }

        internal RangeSampler PrepareRangeSampler(float sampleRate, SampleRange sampleRange, int destinationStartFrameIndex, NativeArray<AffineTransform> outTransforms)
        {
            AllocateBakedAnimation(sampleRate);

            if (bakedAnimation.Value.NumFrames < sampleRange.startFrameIndex + sampleRange.numFrames)
            {
                throw new ArgumentException($"Trying to sample {sampleRange.startFrameIndex + sampleRange.numFrames - 1} frame from {animationClip.name} whose last frame is {bakedAnimation.Value.NumFrames - 1} (resampled at {sampleRate} fps)", "sampleRange");
            }

            int numJoints = editorAnimation.JointSamplers.Length;

            NativeArray<AffineTransform> localPoses = new NativeArray<AffineTransform>(sampleRange.numFrames * numJoints, Allocator.Persistent);

            BakeJob bakeJob = ConfigureBake(editorAnimation.AnimationCurves, sampleRate, bakedAnimation.Value.AnimationCurves, Allocator.Persistent, sampleRange);

            SampleLocalPosesJob sampleLocalPosesJob = new SampleLocalPosesJob()
            {
                jointSamplers = new MemoryArray<TransformSampler>(bakedAnimation.Value.JointSamplers),
                localPoses = localPoses,
                sampleRange = sampleRange,
                poseSamplePostProcess = poseSamplePostProcess.Clone()
            };

            ConvertToGlobalPosesJob convertToGlobalPoseJob = new ConvertToGlobalPosesJob()
            {
                localPoses = localPoses,
                globalTransforms = new MemoryArray<AffineTransform>(outTransforms),
                numJoints = numJoints,
                sampleRange = sampleRange,
                poseSamplePostProcess = poseSamplePostProcess.Clone(),
                destinationStartFrameIndex = destinationStartFrameIndex,
            };

            return new RangeSampler()
            {
                localPoses = localPoses,
                bakeJob = bakeJob,
                sampleLocalPosesJob = sampleLocalPosesJob,
                convertToGlobalPosesJob = convertToGlobalPoseJob
            };
        }
    }
}
