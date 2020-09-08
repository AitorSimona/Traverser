using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.TestTools;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Threading;

[TestFixture]
public class EditModeTest
{
    private const int MaxIterations = 500;

//    [UnityTest]
//    [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor)]
    public IEnumerator CheckBurstJobEnabledDisabled() {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
#if UNITY_2019_3_OR_NEWER
        foreach(var item in CheckBurstJobDisabled()) yield return item;
        foreach(var item in CheckBurstJobEnabled()) yield return item;
#else
        foreach(var item in CheckBurstJobEnabled()) yield return item;
        foreach(var item in CheckBurstJobDisabled()) yield return item;
#endif
        BurstCompiler.Options.EnableBurstCompilation = true;
    }

    private IEnumerable CheckBurstJobEnabled()
    {
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        using (var jobTester = new BurstJobTester())
        {
            var result = jobTester.Calculate();
            Assert.AreNotEqual(0.0f, result);
        }
    }

    private IEnumerable CheckBurstJobDisabled()
    {
        BurstCompiler.Options.EnableBurstCompilation = false;

        yield return null;

        using (var jobTester = new BurstJobTester())
        {
            var result = jobTester.Calculate();
            Assert.AreEqual(0.0f, result);
        }
    }

#if UNITY_2019_3_OR_NEWER
    [UnityTest]
    [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor)]
    public IEnumerator CheckJobWithNativeArray()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        var job = new BurstJobTester.MyJobCreatingAndDisposingNativeArray()
        {
            Length = 128,
            Result = new NativeArray<int>(16, Allocator.TempJob)
        };
        var handle = job.Schedule();
        handle.Complete();
        try
        {
            Assert.AreEqual(job.Length, job.Result[0]);
        }
        finally
        {
            job.Result.Dispose();
        }
    }
#endif


#if UNITY_BURST_BUG_FUNCTION_POINTER_FIXED
    [UnityTest]
    public IEnumerator CheckBurstFunctionPointerException()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = true;
        BurstCompiler.Options.EnableBurstCompilation = true;

        yield return null;

        using (var jobTester = new BurstJobTester())
        {
            var exception = Assert.Throws<InvalidOperationException>(() => jobTester.CheckFunctionPointer());
            StringAssert.Contains("Exception was thrown from a function compiled with Burst", exception.Message);
        }
    }
#endif

    [UnityTest]
    [UnityPlatform(RuntimePlatform.OSXEditor, RuntimePlatform.WindowsEditor)]
    public IEnumerator CheckBurstAsyncJob()
    {
        BurstCompiler.Options.EnableBurstCompileSynchronously = false;
        BurstCompiler.Options.EnableBurstCompilation = true;

        var iteration = 0;
        var result = 0.0f;
        var array = new NativeArray<float>(10, Allocator.Persistent);

        while (result == 0.0f && iteration < MaxIterations)
        {
            array[0] = 0.0f;
            var job = new BurstJobTester.MyJobAsync { Result = array };
            job.Schedule().Complete();
            result = job.Result[0];
            iteration++;
            yield return null;
        }
        array.Dispose();
        Assert.AreNotEqual(0.0f, result);
    }
}
