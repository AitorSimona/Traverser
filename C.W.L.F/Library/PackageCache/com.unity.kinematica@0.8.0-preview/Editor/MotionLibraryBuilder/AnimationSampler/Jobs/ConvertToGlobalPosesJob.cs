using System;

using Unity.Collections;
using static AnimationCurveBake;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct ConvertToGlobalPosesJob : IJobParallelFor, IDisposable
    {
        public NativeArray<AffineTransform> localPoses; // input read/write contiguous local poses that will be converted to global
        public MemoryArray<AffineTransform> globalTransforms; // output global transforms interleaved array (all transforms from motion library for each joint are contiguous)

        // settings
        [ReadOnly]
        public int numJoints;

        [ReadOnly]
        public SampleRange sampleRange;

        [ReadOnly]
        public PoseSamplePostProcess poseSamplePostProcess;

        [ReadOnly]
        public int destinationStartFrameIndex; // frame index of the sample range start in the binary motion library

        public void Execute(int index)
        {
            int frameIndex = sampleRange.startFrameIndex + index;

            int numFrames = globalTransforms.Length / numJoints;

            MemoryArray<AffineTransform> localPose = new MemoryArray<AffineTransform>(localPoses, numJoints * index, numJoints);

            AffineTransform trajectory = localPose[0];

            // convert to global
            localPose[0] = AffineTransform.identity;
            for (int jointIndex = 1; jointIndex < numJoints; ++jointIndex)
            {
                int parentIndex = poseSamplePostProcess.parentIndices[jointIndex];

                if (parentIndex >= 0)
                {
                    localPose[jointIndex] = localPose[parentIndex] * localPose[jointIndex];
                }
            }
            localPose[0] = trajectory;

            // write result global pose in interleaved array
            for (int jointIndex = 0; jointIndex < numJoints; ++jointIndex)
            {
                globalTransforms[jointIndex * numFrames + destinationStartFrameIndex + index] = localPose[jointIndex];
            }
        }

        public void Dispose()
        {
            poseSamplePostProcess.Dispose();
        }
    }
}
