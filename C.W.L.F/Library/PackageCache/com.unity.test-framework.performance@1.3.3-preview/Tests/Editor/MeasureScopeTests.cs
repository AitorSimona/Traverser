using NUnit.Framework;
using System.Threading;
using Unity.PerformanceTesting;

public class MeasureScope
{
    readonly SampleGroupDefinition sgd = new SampleGroupDefinition("TEST", SampleUnit.Microsecond, AggregationType.Average, 0.2D, true);

    [Test, Performance]
    public void MeasureScope_WithoutDefinition_MeasuresDefaultSample()
    {
        using (Measure.Scope())
        {
            Thread.Sleep(1);
        }

        var result = PerformanceTest.Active;
        var definition = result.SampleGroups[0].Definition;
        Assert.That(result.SampleGroups.Count, Is.EqualTo(1));
        Assert.That(result.SampleGroups[0].Samples[0], Is.GreaterThan(0.0f));
        AssertDefinition(definition, "Time", SampleUnit.Millisecond, AggregationType.Median, 0.0D, 0.15D, false);
    }

    [Test, Performance]
    public void MeasureScope_WithDefinition_MeasuresSample()
    {
        using (Measure.Scope(sgd))
        {
            Thread.Sleep(1);
        }

        var result = PerformanceTest.Active;
        var definition = result.SampleGroups[0].Definition;
        Assert.That(result.SampleGroups.Count, Is.EqualTo(1));
        Assert.That(result.SampleGroups[0].Samples[0], Is.GreaterThan(0.0f));
        AssertDefinition(definition, "TEST", SampleUnit.Microsecond, AggregationType.Average, 0.0D, 0.2D, true);
    }
    
    [Test, Performance]
    public void MeasureScope_WithDifferentDefinitions_IsUnaffected()
    {
        using (Measure.Scope(new SampleGroupDefinition("TEST")))
        {
            Thread.Sleep(1);
        }

        using (Measure.Scope(new SampleGroupDefinition("TEST", SampleUnit.Second)))
        {
            Thread.Sleep(1);
        }
        
        var result = PerformanceTest.Active;
        var definition = result.SampleGroups[0].Definition;
        Assert.That(result.SampleGroups.Count, Is.EqualTo(1));
        Assert.That(result.SampleGroups[0].Samples.Count, Is.EqualTo(2));
        AssertDefinition(definition, "TEST", SampleUnit.Millisecond, AggregationType.Median, 0.0D, 0.15D, false);
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