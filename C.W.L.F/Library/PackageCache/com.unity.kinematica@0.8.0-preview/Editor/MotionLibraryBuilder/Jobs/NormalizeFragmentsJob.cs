using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct NormalizeFragmentsJob : IJobParallelFor
    {
        [ReadOnly]
        public int numTransformedFeatures;

        [ReadOnly]
        public int transformedIndex;

        [ReadOnly]
        public NativeArray<Builder.BoundingBox> boundingBoxes;

        public Builder.FragmentArray fragmentArray;

        public void Execute(int fragmentIndex)
        {
            MemoryArray<float3> fragmentFeatures = fragmentArray.FragmentFeatures(fragmentIndex);

            for (int featureIndex = 0; featureIndex < numTransformedFeatures; ++featureIndex)
            {
                fragmentFeatures[transformedIndex + featureIndex] = boundingBoxes[featureIndex].normalize(fragmentFeatures[transformedIndex + featureIndex]);
            }
        }
    }
}
