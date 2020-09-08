using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct CopyKMeansCentroidsJob : IJob, ISchedulableJob
    {
        [ReadOnly]
        public int index;

        [ReadOnly]
        public int numFloats;

        public KMeans kmeans;

        public MemoryArray<float> centroids;

        public void Execute()
        {
            for (int i = 0; i < numFloats; ++i)
            {
                centroids[index + i] = kmeans.centroids[i];
            }
        }

        public JobHandle ScheduleJob(JobHandle dependsOn)
        {
            return IJobExtensions.Schedule<CopyKMeansCentroidsJob>(this, dependsOn);
        }
    }
}
