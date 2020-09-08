using System;
using Unity.Collections;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial struct KMeans : IDisposable
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

            public static Settings Create()
            {
                return new Settings
                {
                    numIterations = 25,
                    numAttempts = 1,
                    seed = 1234
                };
            }

            public static Settings Default => Create();
        }

        //
        // Settings for KMeans clustering
        //

        public Settings settings;

        //
        // dimension of the vectors
        //

        public int d;

        //
        // number of centroids
        //

        public int k;

        //
        // centroids (k * d)
        //

        public NativeArray<float> centroids;
        NativeArray<distance> distances;

        NativeArray<float> bestCentroids;

        NativeArray<float> errors;

        public KMeans(int d, int k, int nx, Settings settings)
        {
            this.d = d;
            this.k = k;

            this.settings = settings;

            centroids = new NativeArray<float>(d * k, Allocator.Persistent);

            distances = new NativeArray<distance>(nx, Allocator.Persistent);

            bestCentroids = new NativeArray<float>(d * k, Allocator.Persistent);

            errors = new NativeArray<float>(2, Allocator.Persistent);
        }

        public void Dispose()
        {
            centroids.Dispose();
            distances.Dispose();
            bestCentroids.Dispose();
            errors.Dispose();
        }

        public JobQueue PrepareTrainingJobQueue(MemoryArray<float> features, int numSamples, int batchCount)
        {
            Debug.Assert(centroids.Length == d * k);

            JobQueue jobQueue = JobQueue.Create(batchCount);

            errors[1] = float.MaxValue;

            for (int attempt = 0; attempt < numAttempts; ++attempt)
            {
                PreAttemptJob preAttemptJob = new PreAttemptJob()
                {
                    d = d,
                    k = k,
                    numSamples = numSamples,
                    randomSeed = randomSeed,
                    attempt = attempt,
                    features = features,
                    centroids = new MemoryArray<float>(centroids),
                };

                jobQueue.AddJob(preAttemptJob);

                for (int iteration = 0; iteration < numIterations; ++iteration)
                {
                    TrainIterationJob trainIterationJob = new TrainIterationJob()
                    {
                        d = d,
                        k = k,
                        numSamples = numSamples,
                        features = features,
                        centroids = new MemoryArray<float>(centroids),
                        distances = new MemoryArray<distance>(distances),
                        errors = new MemoryArray<float>(errors),
                        error = 0.0f
                    };

                    jobQueue.AddJob(trainIterationJob);
                }

                if (numAttempts > 1)
                {
                    PostAttemptJob postAttemptJob = new PostAttemptJob()
                    {
                        bestCentroids = new MemoryArray<float>(bestCentroids),
                        centroids = new MemoryArray<float>(centroids),
                        errors = new MemoryArray<float>(errors)
                    };

                    jobQueue.AddJob(postAttemptJob);
                }
            }

            if (numAttempts > 1)
            {
                PostTrainingJob postTrainingJob = new PostTrainingJob()
                {
                    bestCentroids = new MemoryArray<float>(bestCentroids),
                    centroids = new MemoryArray<float>(centroids)
                };

                jobQueue.AddJob(postTrainingJob);
            }

            return jobQueue;
        }

        int numIterations => settings.numIterations;

        int numAttempts => settings.numAttempts;

        int randomSeed => settings.seed;

        public struct distance
        {
            public int index;
            public float l2;
        }


        unsafe float ImbalanceFactor(int n, int k, NativeArray<distance> assign)
        {
            int* histogram = stackalloc int[k];
            for (int i = 0; i < k; ++i)
            {
                histogram[i] = 0;
            }

            for (int i = 0; i < n; i++)
            {
                histogram[assign[i].index]++;
            }

            float total = 0.0f;
            float result = 0.0f;

            for (int i = 0; i < k; i++)
            {
                total += histogram[i];
                result += histogram[i] * (float)histogram[i];
            }
            result = result * k / (total * total);

            return result;
        }
    }
}
