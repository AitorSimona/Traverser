 using System;
 using System.Runtime.InteropServices;
 using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Jobs.Tests
{
    // The purpose of this test is twofold:
    // 1. Test the simplest custom job.
    // 2. (In the #if JOBS_CODEGEN_SAMPLE) show what code code-gen is injecting.

    [JobProducerType(typeof(ICustomJobExtensions.CustomJobProcess<>))]
    public interface ICustomJob
#if JOBS_CODEGEN_SAMPLE
        : IJobBase
#endif
    {
        void Execute(ref TestData data);
    }

    public struct TestData
    {
        internal int a;
        internal int b;
    }

    public struct CustomWorld
    {
        internal TestData abData;
    }

    public static class ICustomJobExtensions
    {
        public static unsafe JobHandle Schedule<T>(this T jobData, ref CustomWorld world, JobHandle dependsOn)
            where T : struct, ICustomJob
        {
            var data = new CustomJobData<T>
            {
                UserJobData = jobData,
                abData = world.abData,
                testArray = new NativeArray<int>(10, Allocator.TempJob)
            };

            var parameters = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref data),
                CustomJobProcess<T>.Initialize(),
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

        internal struct CustomJobData<T> where T : struct
        {
            public T UserJobData;
            public TestData abData;
            [DeallocateOnJobCompletion]
            public NativeArray<int> testArray;
        }

        internal struct CustomJobProcess<T> where T : struct, ICustomJob
        {
            static IntPtr jobReflectionData;

            public static unsafe IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(CustomJobData<T>), typeof(T),
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

#if JOBS_CODEGEN_SAMPLE
            static unsafe void ProducerExecuteFn_Gen(void* structPtr)
            {
                CustomJobData<T> jobStruct = *(CustomJobData<T>*)structPtr;
                var jobRanges = new JobRanges();
                Execute(ref jobStruct, new IntPtr(0), new IntPtr(0), ref jobRanges, 0);
                UnsafeUtility.Free(structPtr, Allocator.TempJob);
            }
#endif
            public delegate void ExecuteJobFunction(ref CustomJobData<T> jobData, IntPtr additionalData,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref CustomJobData<T> jobData, IntPtr additionalData,
                IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
#if JOBS_CODEGEN_SAMPLE
                jobData.UserJobData.PrepareJobAtExecuteTimeFn_Gen(jobIndex);
#endif
                jobData.UserJobData.Execute(ref jobData.abData);
#if JOBS_CODEGEN_SAMPLE
                jobData.UserJobData.CleanupJobFn_Gen(void* structPtr);
#endif
            }
        }
    }

    public class JobCustomTests
    {
        struct CustomJob1 : ICustomJob
        {
            [DeallocateOnJobCompletion]
            public NativeArray<uint> jobData;    // Both the wrapper and CustomJob1 have [DeallocateOnJobCompletion]
            public NativeArray<int> result;

            public void Execute(ref TestData data)
            {
                result[0] = data.a + data.b;
            }

#if JOBS_CODEGEN_SAMPLE
            public int PrepareJobAtScheduleTimeFn_Gen() { /* safety handles */ }
            public void PrepareJobAtExecuteTimeFn_Gen(int index) { /* threadIndex */ }
            public void CleanupJobFn_Gen(void* ptr)
            {
                // If there is no wrapper, ptr will be null.
                // Can also be null for IJobForEach.
                if (ptr == null) return;

                CustomJobData<CustomJob1> jobData = *((CustomJobData<CustomJob1>*)ptr);
                jobData.testArray.Dispose();
            }
#endif
        }

        struct CustomJob2 : ICustomJob
        {
            public NativeArray<int> result;

            public void Execute(ref TestData data)
            {
                result[0] = data.a + 2 * data.b;
            }

#if JOBS_CODEGEN_SAMPLE
            public int PrepareJobAtScheduleTimeFn_Gen() { /* safety handles */ }
            public void PrepareJobAtExecuteTimeFn_Gen(int index) { /* threadIndex */ }
            public void CleanupJobFn_Gen(void*) { /* disposes memory */ }
#endif
        }

        struct CustomJob3<T> : ICustomJob
        {
            public NativeArray<int> result;

            public void Execute(ref TestData data)
            {
                result[0] = data.a + 3 * data.b;
            }
        }

#if UNITY_DOTSPLAYER
        [SetUp]
        public void Init()
        {
            Unity.Burst.DotsRuntimeInitStatics.Init();
        }
#endif

        [Test]
        public void ScheduleCustomJob1()
        {
            NativeArray<int> result = new NativeArray<int>(1, Allocator.TempJob);
            CustomJob1 customJob1 = new CustomJob1()
            {
                result = result,
                jobData = new NativeArray<uint>(20, Allocator.TempJob)
            };
            CustomWorld customWorld = new CustomWorld()
            {
                abData = new TestData()
                {
                    a = 1,
                    b = 2
                }
            };
            var handle = customJob1.Schedule(ref customWorld, new JobHandle());
            handle.Complete();

            Assert.AreEqual(3, result[0]);
            result.Dispose();
        }

        [Test]
        public void ScheduleCustomJob2()
        {
            NativeArray<int> result = new NativeArray<int>(1, Allocator.TempJob);
            CustomJob2 customJob2 = new CustomJob2()
            {
                result = result
            };
            CustomWorld customWorld = new CustomWorld()
            {
                abData = new TestData()
                {
                    a = 1,
                    b = 2
                }
            };
            var handle = customJob2.Schedule(ref customWorld, new JobHandle());
            handle.Complete();

            Assert.AreEqual(5, result[0]);
            result.Dispose();
        }

        [Test]
        public void ScheduleCustomJob3()
        {
            NativeArray<int> result = new NativeArray<int>(1, Allocator.TempJob);
            CustomJob3<int> customJob3 = new CustomJob3<int>()
            {
                result = result
            };
            CustomWorld customWorld = new CustomWorld()
            {
                abData = new TestData()
                {
                    a = 1,
                    b = 2
                }
            };
            var handle = customJob3.Schedule(ref customWorld, new JobHandle());
            handle.Complete();

            Assert.AreEqual(7, result[0]);
            result.Dispose();
        }

    }
}
