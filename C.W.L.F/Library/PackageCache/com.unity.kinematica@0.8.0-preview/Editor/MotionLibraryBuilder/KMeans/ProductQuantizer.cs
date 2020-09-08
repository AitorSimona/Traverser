using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica.Editor
{
    internal class ProductQuantizer : IDisposable
    {
        public struct Settings
        {
            //
            // Number of clustering attempts
            //

            public int numAttempts;

            //
            // Number of clustering iterations
            //

            public int numIterations;

            //
            // seed for the random number generator
            //

            public int seed;

            //
            // minimum and maximum samples per centroid
            //

            public int minimumNumberSamples;
            public int maximumNumberSamples;

            public static Settings Create()
            {
                return new Settings
                {
                    numIterations = 25,
                    minimumNumberSamples = 32,
                    maximumNumberSamples = 256,
                    numAttempts = 1,
                    seed = 1234
                };
            }

            public static Settings Default => Create();
        }

        //
        // Size of the input vectors
        //

        int d;

        //
        // Number of sub-quantizers
        //

        int M;

        //
        // Number of bits per quantization index
        //

        int numBits;

        //
        // dimensionality of each sub-vector
        //

        int dsub;

        //
        // byte per indexed vector
        //

        int codeSize;

        //
        // number of centroids for each sub-quantizer
        //

        int ksub;

        //
        // Settings used during clustering
        //

        Settings settings;

        //
        // Centroid table, size M * ksub * dsub
        //

        public NativeArray<float> centroids;

        //
        // Returns the centroids associated with sub-vector m
        //

        public NativeSlice<float> GetCentroids(int m)
        {
            return new NativeSlice<float>(
                centroids, m * ksub * dsub, ksub * dsub);
        }

        //
        // d - dimensionality of the input vectors
        // M - number of sub-quantizers
        //

        public ProductQuantizer(int d, int M, Settings settings)
        {
            this.d = d;
            this.M = M;

            numBits = Binary.CodeBook.kNumCodeBits;

            Debug.Assert(d % M == 0);

            dsub = d / M;

            int numBytesPerIndex = (numBits + 7) / 8;
            codeSize = numBytesPerIndex * M;

            ksub = 1 << numBits;

            centroids =
                new NativeArray<float>(
                    d * ksub, Allocator.Persistent);

            this.settings = settings;
        }

        public void Dispose()
        {
            centroids.Dispose();
        }

        //
        // Train the product quantizer on a set of points.
        //


        public struct TrainingData : IDisposable
        {
            public NativeArray<int> permutation;
            public NativeArray<float> slice;

            public JobQueue[] jobQueues;
            public KMeans[] featureKmeans;

            public float FrameUpdate()
            {
                int numFinishedQueues = 0;

                float progression = 0.0f;

                for (int i = 0; i < jobQueues.Length; ++i)
                {
                    float featureProgression = jobQueues[i].FrameUpdate();
                    if (featureProgression >= 1.0f)
                    {
                        ++numFinishedQueues;
                    }

                    progression += featureProgression;
                }

                if (numFinishedQueues == jobQueues.Length)
                {
                    return 1.0f;
                }
                else
                {
                    // average progression
                    return progression / jobQueues.Length;
                }
            }

            public void ForceCompleteCurrentBatch()
            {
                for (int i = 0; i < jobQueues.Length; ++i)
                {
                    jobQueues[i].ForceCompleteBatch();
                }
            }

            public void Dispose()
            {
                permutation.Dispose();
                slice.Dispose();

                foreach (KMeans kmeans in featureKmeans)
                {
                    kmeans.Dispose();
                }
            }
        }

        public TrainingData ScheduleTraining(ref Builder.FragmentArray fragmentArray)
        {
            //
            // TODO: Variable bitrate encoding.
            // TODO: Use Jobs to train slices in parallel,
            //       requires alternative random generator.
            //

            var numInputSamples = fragmentArray.numFragments;

            var numTrainingSamples = math.clamp(
                numInputSamples, settings.minimumNumberSamples * ksub,
                settings.maximumNumberSamples * ksub);

            TrainingData trainingData = new TrainingData()
            {
                permutation = new NativeArray<int>(numTrainingSamples, Allocator.Persistent),
                slice = new NativeArray<float>(numTrainingSamples * dsub, Allocator.Persistent),
            };

            var random = new RandomGenerator(settings.seed);

            if ((numTrainingSamples < numInputSamples) || (numTrainingSamples > numInputSamples))
            {
                for (int i = 0; i < numTrainingSamples; i++)
                {
                    trainingData.permutation[i] = random.Integer(numInputSamples);
                }
            }
            else
            {
                for (int i = 0; i < numTrainingSamples; i++)
                {
                    trainingData.permutation[i] = i;
                }
            }

            for (int i = 0; i + 1 < numTrainingSamples; i++)
            {
                int i2 = i + random.Integer(numTrainingSamples - i);
                int t = trainingData.permutation[i];

                trainingData.permutation[i] = trainingData.permutation[i2];
                trainingData.permutation[i2] = t;
            }

            //
            // Loop over features (M)
            //

            for (int m = 0; m < M; m++)
            {
                //
                // Prepare feature slice for all samples (n)
                //

                int writeOffset = 0;

                unsafe
                {
                    for (int i = 0; i < numTrainingSamples; i++)
                    {
                        int sampleIndex = trainingData.permutation[i];

                        Assert.IsTrue(fragmentArray.FragmentFeatures(sampleIndex).Length == M);

                        float* x = (float*)fragmentArray.FragmentFeatures(sampleIndex).ptr;

                        int readOffset = m * dsub;

                        for (int j = 0; j < dsub; ++j)
                        {
                            trainingData.slice[writeOffset++] = x[readOffset++];
                        }
                    }
                }

                Assert.IsTrue(writeOffset == trainingData.slice.Length);
            }

            trainingData.jobQueues = new JobQueue[M];
            trainingData.featureKmeans = new KMeans[M];

            for (int m = 0; m < M; m++)
            {
                var kms = KMeans.Settings.Default;

                kms.numIterations = settings.numIterations;
                kms.numAttempts = settings.numAttempts;
                kms.seed = settings.seed;

                KMeans kmeans = new KMeans(dsub, ksub, numTrainingSamples, kms);

                Assert.IsTrue(numTrainingSamples >= settings.minimumNumberSamples * ksub);
                Assert.IsTrue(numTrainingSamples <= settings.maximumNumberSamples * ksub);

                trainingData.jobQueues[m] = kmeans.PrepareTrainingJobQueue(new MemoryArray<float>(trainingData.slice), numTrainingSamples, 5);
                trainingData.jobQueues[m].AddJob(new CopyKMeansCentroidsJob()
                {
                    index = m * ksub * dsub,
                    numFloats = ksub * dsub,
                    kmeans = kmeans,
                    centroids = new MemoryArray<float>(centroids)
                });

                trainingData.featureKmeans[m] = kmeans;
            }

            return trainingData;
        }

        // Quantize single or multiple vectors with the product quantizer


        unsafe void ComputeCode(float* x_ptr, byte* code_ptr)
        {
            float* distances = stackalloc float[ksub];

            float* y_ptr = (float*)NativeArrayUnsafeUtility.GetUnsafePtr(centroids);

            for (int m = 0; m < M; m++)
            {
                int index = -1;
                float minimumDistance = float.MaxValue;

                int mdsub = m * dsub;
                int mksubdsub = m * ksub * dsub;

                for (int i = 0; i < ksub; ++i)
                {
                    int idsub = i * dsub;

                    float l2square = 0.0f;
                    for (int j = 0; j < dsub; j++)
                    {
                        float d = x_ptr[mdsub + j] - y_ptr[mksubdsub + idsub + j];
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
                        index = i;
                    }
                }

                code_ptr[m] = (byte)index;
            }
        }

        public unsafe void ComputeCodes(ref Builder.FragmentArray fragmentArray, NativeSlice<byte> codes)
        {
            ComputeCodesJob job = new ComputeCodesJob()
            {
                ksub = ksub,
                dsub = dsub,
                M = M,
                centroids = centroids,
                features = fragmentArray.Features.Reinterpret<float>(),
                strideFeatures = fragmentArray.numFeatures * 3,
                codes = new MemoryArray<byte>(codes),
                strideCodes = codeSize,
                startIndex = 0
            };

            JobHandle handle = job.Schedule(fragmentArray.numFragments, 1);
            handle.Complete();
        }
    }
}
