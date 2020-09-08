using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal partial struct KMeans
    {
        [BurstCompile(CompileSynchronously = true)]
        public struct PostAttemptJob : IJob, ISchedulableJob
        {
            public MemoryArray<float> bestCentroids;
            public MemoryArray<float> centroids;

            public MemoryArray<float> errors;

            public JobHandle ScheduleJob(JobHandle dependsOn)
            {
                return IJobExtensions.Schedule<PostAttemptJob>(this, dependsOn);
            }

            public void Execute()
            {
                if (errors[0] < errors[1])
                {
                    errors[1] = errors[0];

                    centroids.CopyTo(ref bestCentroids);
                }
            }
        }
    }
}
