using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

public class FrametimeOverloadTests
{
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WhenYielding_RecordsFrametimes()
    {
        using (Measure.Frames().Scope())
        {
            yield return null;
            yield return null;
        }
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 2);
        Assert.IsTrue(AllSamplesHigherThan0(test));
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithNoParams_RecordsSampleGroup()
    {
        yield return Measure.Frames().Run();

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.Greater(test.SampleGroups[0].Samples.Count, 0);
        Assert.IsTrue(AllSamplesHigherThan0(test));
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithCustomDefinition_AssignsDefinition()
    {
        yield return Measure.Frames()
            .Definition("TIME", SampleUnit.Microsecond)
            .Run();

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Definition.Name, "TIME");
        Assert.AreEqual(test.SampleGroups[0].Definition.SampleUnit, SampleUnit.Microsecond);
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithRecordingDisabled_RecordsNoSampleGroups()
    {
        yield return Measure.Frames().DontRecordFrametime().Run();
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 0);
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithProfilerMarkers_RecordsMarkers()
    {
        var obj = new GameObject("MeasureFrames_WithRecordingDisabled_RecordsNoSampleGroups");
        obj.AddComponent<CreateMarkerOnUpdate>();
        
        yield return Measure.Frames()
            .ProfilerMarkers(new SampleGroupDefinition("TEST_MARKER", SampleUnit.Microsecond))
            .DontRecordFrametime()
            .Run();
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Definition.Name, "TEST_MARKER");
        Assert.AreEqual(test.SampleGroups[0].Definition.SampleUnit, SampleUnit.Microsecond);
        Assert.IsTrue(AllSamplesHigherThan0(test));
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithProfilerMarkers_string_RecordsMarkers()
    {
        var obj = new GameObject("MeasureFrames_WithRecordingDisabled_RecordsNoSampleGroups");
        obj.AddComponent<CreateMarkerOnUpdate>();
        
        yield return Measure.Frames()
            .ProfilerMarkers("TEST_MARKER")
            .DontRecordFrametime()
            .Run();
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Definition.Name, "TEST_MARKER");
        Assert.AreEqual(test.SampleGroups[0].Definition.SampleUnit, SampleUnit.Millisecond);
        Assert.IsTrue(AllSamplesHigherThan0(test));
    }

    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithWarmupAndExecutions_RecordsSpecifiedAmount()
    {
        yield return Measure.Frames()
            .WarmupCount(10)
            .MeasurementCount(10)
            .Run();
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 10);
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureFrames_WithWarmup_ThrowsException()
    {
        LogAssert.Expect(LogType.Error, new Regex(".+frames measurement"));
        yield return Measure.Frames()
            .WarmupCount(10)
            .Run();
        
        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 0);
    }
    
    private static bool AllSamplesHigherThan0(PerformanceTest test)
    {
        foreach (var sampleGroup in test.SampleGroups)
        {
            foreach (var sample in sampleGroup.Samples)
            {
                if (sample <= 0) return false;
            }
        }

        return true;
    }
    
    public class CreateMarkerOnUpdate : MonoBehaviour
    {
        private CustomSampler m_CustomSampler;

        private void OnEnable()
        {
            m_CustomSampler = CustomSampler.Create("TEST_MARKER");
        }

        private void Update()
        {
            m_CustomSampler.Begin();
            Thread.Sleep(1);
            m_CustomSampler.End();
        }
    }
}
