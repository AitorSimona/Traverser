using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial struct KMeans : IDisposable
    {
        [BurstCompile(CompileSynchronously = true)]
        public struct PreAttemptJob : IJob, ISchedulableJob
        {
            public int d;
            public int k;
            public int numSamples;
            public int randomSeed;
            public int attempt;

            public MemoryArray<float> features;
            public MemoryArray<float> centroids;

            public void Execute()
            {
                var perm = new NativeArray<int>(numSamples, Allocator.Temp);
                {
                    RandomPermutation(perm, randomSeed + 1 + attempt * 15486557);

                    for (int i = 0; i < k; ++i)
                    {
                        int index = perm[i % perm.Length];

                        for (int j = 0; j < d; ++j)
                        {
                            centroids[i * d + j] = features[index * d + j];
                        }
                    }
                }
                perm.Dispose();
            }

            public JobHandle ScheduleJob(JobHandle dependsOn)
            {
                return IJobExtensions.Schedule<PreAttemptJob>(this, dependsOn);
            }

            void RandomPermutation(NativeArray<int> perm, int seed)
            {
                int n = perm.Length;

                for (int i = 0; i < n; i++)
                {
                    perm[i] = i;
                }

                var random = new Unity.Mathematics.Random((uint)seed);

                for (int i = 0; i + 1 < n; i++)
                {
                    int i2 = i + random.NextInt(n - i);
                    int t = perm[i];
                    perm[i] = perm[i2];
                    perm[i2] = t;
                }
            }
        }
    }
}
