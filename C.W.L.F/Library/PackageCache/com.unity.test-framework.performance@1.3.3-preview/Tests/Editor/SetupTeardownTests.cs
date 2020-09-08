using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSetupTeardown
{
    private static int s_Counter;

    [SetUp]
    public void Setup()
    {
        Debug.Log("Setup");
        s_Counter = 1;
    }

    [Test, Performance]
    public void Test()
    {
        Assert.AreEqual(s_Counter, 1);
        s_Counter = 2;
    }

    [TearDown]
    public void Teardown()
    {
        Debug.Log("Teardown");
        Assert.AreEqual(s_Counter, 2);
    }
}

public class UnityTestSetupTeardown
{
    private static int s_Counter;

    [SetUp]
    public void Setup()
    {
        Debug.Log("Setup");
        s_Counter = 1;
    }

    [UnityTest, Performance]
    public IEnumerator Test()
    {
        Assert.AreEqual(s_Counter, 1);
        s_Counter = 2;
        yield return null;
    }

    [TearDown]
    public void Teardown()
    {
        Debug.Log("Teardown");
        Assert.AreEqual(s_Counter, 2);
    }
}