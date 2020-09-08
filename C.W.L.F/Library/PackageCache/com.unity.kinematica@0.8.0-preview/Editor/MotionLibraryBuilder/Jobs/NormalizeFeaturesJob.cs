using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct NormalizeFeaturesJob : IJobParallelFor
    {
        [ReadOnly]
        public int numQuantizedFeatures;

        [ReadOnly]
        public NativeArray<Builder.Quantizer> quantizers;

        public MemoryArray<byte> quantizedValues;

        public Builder.FragmentArray fragmentArray;

        public void Execute(int fragmentIndex)
        {
            MemoryArray<float3> fragmentFeatures = fragmentArray.FragmentFeatures(fragmentIndex);

            for (int featureIndex = 0; featureIndex < numQuantizedFeatures; ++featureIndex)
            {
                float3 featureValue = fragmentFeatures[featureIndex];

                float length = math.length(featureValue);

                byte code = quantizers[featureIndex].Encode(length);

                quantizedValues[numQuantizedFeatures * fragmentIndex + featureIndex] = code;

                float3 defaultDirection = Missing.zero;

                if (length < 0.02f)
                {
                    fragmentFeatures[featureIndex] = defaultDirection;
                }
                else
                {
                    fragmentFeatures[featureIndex] =
                        math.normalizesafe(
                            featureValue, defaultDirection);
                }
            }
        }
    }
}
