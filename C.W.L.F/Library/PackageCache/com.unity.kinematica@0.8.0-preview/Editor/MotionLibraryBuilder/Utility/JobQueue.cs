using Unity.Jobs;
using System.Collections.Generic;
using Unity.Mathematics;
using System.Linq;

namespace Unity.Kinematica.Editor
{
    internal interface ISchedulableJob
    {
        JobHandle ScheduleJob(JobHandle dependsOn);
    }

    internal struct JobQueue
    {
        List<ISchedulableJob> jobs;

        List<JobHandle> jobHandles;

        int numFinishedJobs;

        int batchCount;

        public static JobQueue Create(int batchCount)
        {
            return new JobQueue()
            {
                jobs = new List<ISchedulableJob>(),
                jobHandles = new List<JobHandle>(batchCount),
                numFinishedJobs = 0,
                batchCount = batchCount,
            };
        }

        public void AddJob(ISchedulableJob job)
        {
            jobs.Add(job);
        }

        public float FrameUpdate()
        {
            if (numFinishedJobs == jobs.Count)
            {
                return 1.0f;
            }

            int numFinishedJobsBatch = jobHandles.Count(j => j.IsCompleted);
            if (jobHandles.Count == numFinishedJobsBatch)
            {
                numFinishedJobs += numFinishedJobsBatch;

                if (numFinishedJobs == jobs.Count)
                {
                    return 1.0f;
                }

                ScheduleBatch();

                return numFinishedJobs / (float)jobs.Count;
            }

            return (numFinishedJobs + numFinishedJobsBatch) / (float)jobs.Count;
        }

        public void ForceCompleteBatch()
        {
            foreach (JobHandle jobHandle in jobHandles)
            {
                jobHandle.Complete();
            }
        }

        void ScheduleBatch()
        {
            ForceCompleteBatch();

            jobHandles.Clear();

            JobHandle dependsOn = default(JobHandle);
            for (int i = numFinishedJobs; i < math.min(numFinishedJobs + batchCount, jobs.Count); ++i)
            {
                dependsOn = jobs[i].ScheduleJob(dependsOn);
                jobHandles.Add(dependsOn);
            }
        }
    }
}
