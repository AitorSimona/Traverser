 using System;
 using System.Runtime.InteropServices;
 using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Jobs.Tests
{
    // Why V2? (see JobCustomTests.cs)
    // The "JobProducer IS the job struct" and "JobProducer HAS a job struct" are painfully
    // different to the code gen. This test covers "IS the job struct", and is
    // more minimal than JobCustomTest - but nevertheless checks that the lights are on.

    [JobProducerType(typeof(ICustomJobExtensionsV2.CustomJobProcessV2<>))]
    public interface ICustomJobV2
    {
        void Execute(int addMe);
    }

    public static class ICustomJobExtensionsV2
    {
        public static unsafe JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = default)
            where T : struct, ICustomJobV2
        {
            var parameters = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobData),
                CustomJobProcessV2<T>.Initialize(),
                dependsOn,
                ScheduleMode.Batched
#if JOBS_CODEGEN_SAMPLE
                ,UnsafeUtility.SizeOf<CustomJobData<T>>()            // A size for memory allocation
                ,data.UserJobData.PrepareJobAtScheduleTimeFn_Gen()   // The return parameter does nothing except debug checks.
                                                                     // Just a reasonable place to find and insert the
                                                                     // call to PrepareJobAtScheduleTimeFn_Gen
#endif
                );

            return JobsUtility.Schedule(ref parameters);
        }

        internal struct CustomJobProcessV2<T> where T : struct, ICustomJobV2
        {
            static IntPtr jobReflectionData;

            public static unsafe IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(T), typeof(T),
                        JobType.Single,
                        (ExecuteJobFunction)Execute
#if JOBS_CODEGEN_SAMPLE
                        ,(JobsUtility.ManagedJobDelegate)ProducerExecuteFn_Gen
                        ,(JobsUtility.ManagedJobDelegate)ProducerCleanupFn_Gen      // Only used for Parallel jobs
#endif
                        );
                }
                return jobReflectionData;
            }

            public delegate void ExecuteJobFunction(ref T jobData, IntPtr additionalData,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref T jobData, IntPtr additionalData,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                jobData.Execute(42);
            }
        }
    }

    public class JobCustom2Tests
    {
        struct CustomJobV2 : ICustomJobV2
        {
            public NativeArray<int> result;
            public NativeArray<int> a;

            public void Execute(int addMe)
            {
                result[0] = a[0] + addMe;
            }
        }

        [Test]
        public void ScheduleCustomJobV2()
        {
            NativeArray<int> input = new NativeArray<int>(1, Allocator.TempJob);
            NativeArray<int> result = new NativeArray<int>(1, Allocator.TempJob);

            input[0] = 1;

            CustomJobV2 job = new CustomJobV2()
            {
                result = result,
                a = input
            };
            var handle = job.Schedule();
            handle.Complete();

            Assert.AreEqual(42 + 1, result[0]);
            result.Dispose();
            input.Dispose();
        }
    }
}
