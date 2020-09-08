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
        public struct TrainIterationJob : IJob, ISchedulableJob
        {
            public int d;
            public int k;
            public int numSamples;
            public MemoryArray<float> features;

            public MemoryArray<float> centroids;
            public MemoryArray<distance> distances;
            public MemoryArray<float> errors;

            public float error;

            public JobHandle ScheduleJob(JobHandle dependsOn)
            {
                return IJobExtensions.Schedule<TrainIterationJob>(this, dependsOn);
            }

            public void Execute()
            {
                MeasureCentroids();

                error = 0.0f;
                for (int j = 0; j < numSamples; j++)
                {
                    error += distances[j].l2;
                }

                errors[0] = error;

                UpdateCentroids();
            }

            unsafe void MeasureCentroids()
            {
                for (int i = 0; i < numSamples; i++)
                {
                    distances[i].index = -1;
                    distances[i].l2 = float.MaxValue;

                    int id = i * d;

                    for (int j = 0; j < k; j++)
                    {
                        var jd = j * d;

                        float disij = 0.0f;
                        for (int k = 0; k < d; ++k)
                        {
                            float d = features[id + k] - centroids[jd + k];
                            disij += d * d;
                        }

                        if (disij < distances[i].l2)
                        {
                            distances[i].index = j;
                            distances[i].l2 = disij;
                        }
                    }
                }
            }

            unsafe int UpdateCentroids()
            {
                int* hassign = stackalloc int[k];
                for (int i = 0; i < k; ++i)
                {
                    hassign[i] = 0;
                }

                Debug.Assert(centroids.Length == d * k);

                for (int i = 0; i < centroids.Length; ++i)
                {
                    centroids[i] = 0.0f;
                }

                for (int i = 0; i < numSamples; ++i)
                {
                    int ci = distances[i].index;
                    Debug.Assert(ci >= 0 && ci < k);
                    hassign[ci]++;
                    for (int j = 0; j < d; ++j)
                    {
                        centroids[ci * d + j] += features[i * d + j];
                    }
                }

                for (int ci = 0; ci < k; ci++)
                {
                    var ni = hassign[ci];
                    if (ni != 0)
                    {
                        for (int j = 0; j < d; ++j)
                        {
                            centroids[ci * d + j] /= (float)ni;
                        }
                    }
                }

                //
                // Take care of void clusters
                //

                int nsplit = 0;

                var random = new Unity.Mathematics.Random(1234);

                for (int ci = 0; ci < k; ++ci)
                {
                    //
                    // need to redefine a centroid
                    //

                    if (hassign[ci] == 0)
                    {
                        int cj = 0;
                        while (true)
                        {
                            //
                            // probability to pick this cluster for split
                            //

                            float p = (hassign[cj] - 1.0f) / (float)(numSamples - k);
                            float r = random.NextFloat();

                            if (r < p)
                            {
                                //
                                // found our cluster to be split
                                //
                                break;
                            }

                            cj = (cj + 1) % k;
                        }

                        for (int j = 0; j < d; ++j)
                        {
                            centroids[ci * d + j] = centroids[cj * d + j];
                        }

                        //
                        // small symmetric perturbation
                        //

                        float eps = 1.0f / 1024.0f;
                        for (int j = 0; j < d; ++j)
                        {
                            if (j % 2 == 0)
                            {
                                centroids[ci * d + j] *= 1 + eps;
                                centroids[cj * d + j] *= 1 - eps;
                            }
                            else
                            {
                                centroids[ci * d + j] *= 1 - eps;
                                centroids[cj * d + j] *= 1 + eps;
                            }
                        }

                        //
                        // assume even split of the cluster
                        //

                        hassign[ci] = hassign[cj] / 2;
                        hassign[cj] -= hassign[ci];

                        nsplit++;
                    }
                }

                return nsplit;
            }
        }
    }
}
