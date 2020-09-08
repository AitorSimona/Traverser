using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

public class GenericTests
{
    [Performance]
    public void Default_VersionAttribute_IsSet()
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "1");
    }
    
    [Performance, Version("TEST")]
    public void VersionAttribute_IsSet()
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "TEST");
    }

    [TestCase("1"), TestCase("2")]
    [Performance, Version("TEST")]
    public void VersionAttribute_IsSet_OnTestCase(string name)
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "TEST");
    }

    [Performance]
    public void ZeroSampleGroups_Highlighted_SingleSample()
    {
        var sgd = new SampleGroupDefinition("TEST");
        Measure.Custom(sgd, 0);
    }

    [Performance]
    public void ZeroSampleGroups_Highlighted_MultipleSamples()
    {
        var sgd = new SampleGroupDefinition("TEST");
        Measure.Custom(sgd, 0);
        Measure.Custom(sgd, 0);
        Measure.Custom(sgd, 0);
        Measure.Custom(sgd, 0);
    }

    [UnityTest, Performance]
    public IEnumerator EnterPlaymode_NoFailure()
    {
        yield return new EnterPlayMode();
        yield return new ExitPlayMode();
    }
}

[Version("1")]
public class ClassVersionTests
{
    [Test, Performance]
    public void Default_VersionAttribute_IsSet()
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "1.1");
    }
    
    [Test, Performance, Version("TEST")]
    public void VersionAttribute_IsSet()
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "1.TEST");
    }

    [TestCase("1"), TestCase("2")]
    [Test, Performance, Version("TEST")]
    public void VersionAttribute_IsSet_OnTestCase(string name)
    {
        Assert.AreEqual(PerformanceTest.Active.TestVersion, "1.TEST");
    }
}