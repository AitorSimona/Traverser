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
        public struct PostTrainingJob : IJob, ISchedulableJob
        {
            public MemoryArray<float> bestCentroids;
            public MemoryArray<float> centroids;

            public JobHandle ScheduleJob(JobHandle dependsOn)
            {
                return IJobExtensions.Schedule<PostTrainingJob>(this, dependsOn);
            }

            public void Execute()
            {
                bestCentroids.CopyTo(ref centroids);
            }
        }
    }
}
