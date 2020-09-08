using System.Collections;
using System.Threading;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;
using UnityEngine.TestTools;

public class MeasureProfilerSamplesTests
{
    [Test, Performance]
    public void MeasureProfilerSamples_WithNoSamples_DoesNotRecordSampleGroups()
    {
        LogAssert.NoUnexpectedReceived();
        using (Measure.ProfilerMarkers(new SampleGroupDefinition[0]))
        {
        }

        var result = PerformanceTest.Active;
        Assert.AreEqual(result.SampleGroups.Count, 0);
    }

    [Test, Performance]
    public void MeasureProfilerSamples_SingleFrame_RecordsSample()
    {
        using (Measure.ProfilerMarkers(new []{new SampleGroupDefinition("TestMarker")}))
        {
            CreatePerformanceMarker("TestMarker", 1);
        }
        
        var result = PerformanceTest.Active;
        Assert.AreEqual(result.SampleGroups.Count, 1);
        Assert.AreEqual(result.SampleGroups[0].Samples.Count, 1);
        Assert.Greater(result.SampleGroups[0].Samples[0], 0);
    }
    
    [Test, Performance]
    public void MeasureProfilerSamples_strings_SingleFrame_RecordsSample()
    {
        using (Measure.ProfilerMarkers("TestMarker"))
        {
            CreatePerformanceMarker("TestMarker", 1);
        }
        
        var result = PerformanceTest.Active;
        Assert.AreEqual(result.SampleGroups.Count, 1);
        Assert.AreEqual(result.SampleGroups[0].Samples.Count, 1);
        Assert.Greater(result.SampleGroups[0].Samples[0], 0);
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureProfilerSamples_ManyFrames_RecordsSamples()
    {
        using (Measure.ProfilerMarkers(new []{new SampleGroupDefinition("TestMarker")}))
        {
            yield return null;
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;            
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;            
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;
            CreatePerformanceMarker("TestMarker", 1);
        }
        
        var result = PerformanceTest.Active;
        Assert.AreEqual(result.SampleGroups.Count, 1);
        Assert.AreEqual(result.SampleGroups[0].Samples.Count, 4);
        Assert.Greater(result.SampleGroups[0].Samples[0], 0);
        Assert.Greater(result.SampleGroups[0].Samples[1], 0);
        Assert.Greater(result.SampleGroups[0].Samples[2], 0);
        Assert.Greater(result.SampleGroups[0].Samples[3], 0);
    }
    
    [UnityTest, Performance]
    public IEnumerator MeasureProfilerSamples_string_ManyFrames_RecordsSamples()
    {
        using (Measure.ProfilerMarkers("TestMarker"))
        {
            yield return null;
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;            
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;            
            CreatePerformanceMarker("TestMarker", 1);
            yield return null;
            CreatePerformanceMarker("TestMarker", 1);
        }
        
        var result = PerformanceTest.Active;
        Assert.AreEqual(result.SampleGroups.Count, 1);
        Assert.AreEqual(result.SampleGroups[0].Samples.Count, 4);
        Assert.Greater(result.SampleGroups[0].Samples[0], 0);
        Assert.Greater(result.SampleGroups[0].Samples[1], 0);
        Assert.Greater(result.SampleGroups[0].Samples[2], 0);
        Assert.Greater(result.SampleGroups[0].Samples[3], 0);
    }

    private static void CreatePerformanceMarker(string name, int sleep)
    {
        var marker = CustomSampler.Create(name);
        marker.Begin();
        Thread.Sleep(sleep);
        marker.End();
    }
}
