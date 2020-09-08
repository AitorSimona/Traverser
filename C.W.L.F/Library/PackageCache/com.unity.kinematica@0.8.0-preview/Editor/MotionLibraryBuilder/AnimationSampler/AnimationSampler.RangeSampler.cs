using System;

using Unity.Collections;
using static AnimationCurveBake.ThreadSafe;
using Unity.Jobs;

using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal partial class AnimationSampler
    {
        internal struct RangeSampler : IDisposable
        {
            public NativeArray<AffineTransform> localPoses;

            public BakeJob bakeJob;
            public SampleLocalPosesJob sampleLocalPosesJob;
            public ConvertToGlobalPosesJob convertToGlobalPosesJob;

            JobHandle bakeHandle;
            JobHandle sampleLocalHandle;
            JobHandle convertToGlobalHandle;

            public void Dispose()
            {
                bakeJob.Dispose();
                sampleLocalPosesJob.Dispose();
                convertToGlobalPosesJob.Dispose();

                localPoses.Dispose();
            }

            public bool IsComplete => convertToGlobalHandle.IsCompleted;

            public void Schedule()
            {
                bakeHandle = bakeJob.Schedule(bakeJob.curves.Length, 1);
                sampleLocalHandle = sampleLocalPosesJob.Schedule(sampleLocalPosesJob.sampleRange.numFrames, 1, bakeHandle);
                convertToGlobalHandle = convertToGlobalPosesJob.Schedule(convertToGlobalPosesJob.sampleRange.numFrames, 1, sampleLocalHandle);
            }

            public void Complete()
            {
                convertToGlobalHandle.Complete();
            }
        }
    }
}
