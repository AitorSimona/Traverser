using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct CreateTrajectoryFragmentsJob : IJobParallelFor, Builder.ICreateFragmentsJob
    {
        [ReadOnly]
        public MemoryRef<Binary> binary;

        [ReadOnly]
        public int metricIndex;

        [ReadOnly]
        public int numFragments;

        [ReadOnly]
        public int numFeatures;

        [ReadOnly]
        public NativeArray<SamplingTime> samplingTimes;

        [WriteOnly]
        public MemoryArray<float3> features; // flat array of fragments features

        ref Binary Binary => ref binary.Ref;

        public static CreateTrajectoryFragmentsJob Prepare(ref Builder.FragmentArray fragmentArray, ref Binary binary)
        {
            return new CreateTrajectoryFragmentsJob()
            {
                binary = new MemoryRef<Binary>(ref binary),
                metricIndex = fragmentArray.metricIndex,
                numFragments = fragmentArray.numFragments,
                numFeatures = fragmentArray.numFeatures,
                samplingTimes = fragmentArray.samplingTimes,
                features = new MemoryArray<float3>(fragmentArray.features)
            };
        }

        public void Execute(int fragmentIndex)
        {
            Binary.TrajectoryFragment poseFragment = Binary.TrajectoryFragment.Create(ref Binary, metricIndex, samplingTimes[fragmentIndex]);
            for (int i = 0; i < numFeatures; ++i)
            {
                features[fragmentIndex * numFeatures + i] = poseFragment.array[i];
            }

            poseFragment.Dispose();
        }

        public JobHandle Schedule()
        {
            return IJobParallelForExtensions.Schedule<CreateTrajectoryFragmentsJob>(this, numFragments, 64);
        }
    }
}
