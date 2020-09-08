using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct ComputeCodesJob : IJobParallelFor
    {
        [ReadOnly]
        public int ksub;

        [ReadOnly]
        public int dsub;

        [ReadOnly]
        public int M;

        [ReadOnly]
        public NativeArray<float> centroids;

        [ReadOnly]
        public MemoryArray<float> features;

        [ReadOnly]
        public int strideFeatures;

        public MemoryArray<byte> codes;

        public int strideCodes;

        public int startIndex;


        public void Execute(int sampleIndex)
        {
            int index = startIndex + sampleIndex;

            MemoryArray<float> sliceFeatures = new MemoryArray<float>(features, index * strideFeatures, strideFeatures);

            var sliceCodes = new MemoryArray<byte>(codes, index * strideCodes, strideCodes);

            unsafe
            {
                float* distances = stackalloc float[ksub];

                for (int m = 0; m < M; m++)
                {
                    int ind = -1;
                    float minimumDistance = float.MaxValue;

                    int mdsub = m * dsub;
                    int mksubdsub = m * ksub * dsub;

                    for (int i = 0; i < ksub; ++i)
                    {
                        int idsub = i * dsub;

                        float l2square = 0.0f;
                        for (int j = 0; j < dsub; j++)
                        {
                            float d = sliceFeatures[mdsub + j] - centroids[mksubdsub + idsub + j];
                            l2square += d * d;
                        }

                        distances[i] = l2square;
                    }

                    //
                    // Find best centroid
                    //

                    for (int i = 0; i < ksub; i++)
                    {
                        float distance = distances[i];
                        if (distance < minimumDistance)
                        {
                            minimumDistance = distance;
                            ind = i;
                        }
                    }

                    sliceCodes[m] = (byte)ind;
                }
            }
        }
    }
}
