using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct ComputeQuantizersJob : IJobParallelFor
    {
        [ReadOnly]
        public Builder.FragmentArray fragmentArray;

        [WriteOnly]
        public NativeArray<Builder.Quantizer> quantizers;

        public void Execute(int featureIndex)
        {
            float minimum = float.MaxValue;
            float maximum = float.MinValue;

            for (int fragmentIndex = 0; fragmentIndex < fragmentArray.numFragments; ++fragmentIndex)
            {
                float3 feature = fragmentArray.Feature(fragmentIndex, featureIndex);

                float length = math.length(feature);

                minimum = math.min(minimum, length);
                maximum = math.max(maximum, length);
            }

            float range = maximum - minimum;

            quantizers[featureIndex] = Builder.Quantizer.Create(minimum, range);
        }
    }
}
