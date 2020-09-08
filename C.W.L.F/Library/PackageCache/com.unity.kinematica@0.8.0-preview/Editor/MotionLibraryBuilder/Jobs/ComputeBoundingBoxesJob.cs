using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct ComputeBoundingBoxesJob : IJobParallelFor
    {
        [ReadOnly]
        public Builder.FragmentArray fragmentArray;

        [ReadOnly]
        public int numTransformedFeatures;

        [ReadOnly]
        public int transformedIndex;

        [WriteOnly]
        public NativeArray<Builder.BoundingBox> boundingBoxes;

        public void Execute(int featureIndex)
        {
            NativeArray<float3> pointCloud = new NativeArray<float3>(fragmentArray.numFragments, Allocator.Temp);

            for (int fragmentIndex = 0; fragmentIndex < fragmentArray.numFragments; ++fragmentIndex)
            {
                pointCloud[fragmentIndex] = fragmentArray.Feature(fragmentIndex, transformedIndex + featureIndex);
            }

            Builder.BoundingBox boundingBox = Builder.BoundingBox.Create(pointCloud);

            boundingBoxes[featureIndex] = boundingBox;
        }
    }
}
