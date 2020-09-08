using NUnit.Framework;
using Unity.PerformanceTesting;
using Unity.PerformanceTesting.Exceptions;
using UnityEngine.TestTools;

public class MeasureCustomTests
{
    private readonly SampleGroupDefinition m_RegularDefinition =
        new SampleGroupDefinition("REGULAR", SampleUnit.Byte, AggregationType.Min, 10.0D, true);

    private readonly SampleGroupDefinition m_PercentileDefinition =
        new SampleGroupDefinition("PERCENTILE", SampleUnit.Second, AggregationType.Max, 0.5D, 20.0D, true);

    [Test, Performance]
    public void MeasureCustom_SampleGroup_CorrectValues()
    {
        Measure.Custom(m_RegularDefinition, 10D);

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples[0], 10D, 0.001D);
        var testDefinition = test.SampleGroups[0].Definition;
        AssertDefinition(testDefinition, "REGULAR", SampleUnit.Byte, AggregationType.Min, 0.0D, 10.0D, true);
    }

    [Test, Performance]
    public void MeasureCustom_SampleGroupWithSamples_CorrectValues()
    {
        Measure.Custom(m_RegularDefinition, 10D);
        Measure.Custom(m_RegularDefinition, 20D);

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 2);
        Assert.AreEqual(test.SampleGroups[0].Samples[0], 10D, 0.001D);
        Assert.AreEqual(test.SampleGroups[0].Samples[1], 20D, 0.001D);
        var testDefinition = test.SampleGroups[0].Definition;
        AssertDefinition(testDefinition, "REGULAR", SampleUnit.Byte, AggregationType.Min, 0.0D, 10.0D, true);
    }

    [Test, Performance]
    public void MeasureCustom_PercentileSample_CorrectValues()
    {
        Measure.Custom(m_PercentileDefinition, 10D);

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples[0], 10D, 0.001D);
        var testDefinition = test.SampleGroups[0].Definition;
        AssertDefinition(testDefinition, "PERCENTILE", SampleUnit.Second, AggregationType.Max, 0.5D, 20.0D, true);
    }

    [Test, Performance]
    public void MeasureCustom_PercentileSamples_CorrectValues()
    {
        Measure.Custom(m_PercentileDefinition, 10D);
        Measure.Custom(m_PercentileDefinition, 20D);

        var test = PerformanceTest.Active;
        Assert.AreEqual(test.SampleGroups.Count, 1);
        Assert.AreEqual(test.SampleGroups[0].Samples.Count, 2);
        Assert.AreEqual(test.SampleGroups[0].Samples[0], 10D, 0.001D);
        Assert.AreEqual(test.SampleGroups[0].Samples[1], 20D, 0.001D);
        var testDefinition = test.SampleGroups[0].Definition;
        AssertDefinition(testDefinition, "PERCENTILE", SampleUnit.Second, AggregationType.Max, 0.5D, 20.0D, true);
    }

    [Test, Performance]
    public void MeasureCustom_MultipleSampleGroups()
    {
        Measure.Custom(m_RegularDefinition, 20D);
        Measure.Custom(m_PercentileDefinition, 10D);

        var test = PerformanceTest.Active;
        var regularDefinition = test.SampleGroups[0].Definition;
        var percentileDefinition = test.SampleGroups[1].Definition;
        Assert.AreEqual(test.SampleGroups.Count, 2);
        AssertDefinition(regularDefinition, "REGULAR", SampleUnit.Byte, AggregationType.Min, 0.0D, 10.0D, true);
        AssertDefinition(percentileDefinition, "PERCENTILE", SampleUnit.Second, AggregationType.Max, 0.5D, 20.0D, true);
    }

    [Test, Performance]
    public void MeasureCustom_InvalidPercentile_ThrowsException()
    {
        Assert.Throws<PerformanceTestException>(() =>
        {
            new SampleGroupDefinition("PERCENTILE", SampleUnit.Byte, AggregationType.Average, 2.0D, 0.1D);
        });
    }

    [Test, Performance]
    public void MeasureCustom_ValidPercentile_ThrowsNoException()
    {
        LogAssert.NoUnexpectedReceived();
        new SampleGroupDefinition("PERCENTILE", SampleUnit.Byte, AggregationType.Average, 0.5D, 0.1D);
    }

    private static void AssertDefinition(SampleGroupDefinition definition, string name, SampleUnit sampleUnit,
        AggregationType aggregationType, double percentile, double threshhold, bool increaseIsBetter)
    {
        Assert.AreEqual(definition.Name, name);
        Assert.AreEqual(definition.SampleUnit, sampleUnit);
        Assert.AreEqual(definition.AggregationType, aggregationType);
        Assert.AreEqual(definition.Percentile, percentile);
        Assert.AreEqual(definition.Threshold, threshhold, 0.001D);
        Assert.AreEqual(definition.IncreaseIsBetter, increaseIsBetter);
    }
}