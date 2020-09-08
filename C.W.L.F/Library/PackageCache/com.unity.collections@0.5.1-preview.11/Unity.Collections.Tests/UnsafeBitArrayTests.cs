using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public class UnsafeBitArrayTests
{
    [Test]
    public void UnsafeBitArray_Get_Set()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Assert.False(test.IsSet(123));
        test.Set(123, true);
        Assert.True(test.IsSet(123));

        Assert.False(test.TestAll(0, numBits));
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAny(0, numBits));
        Assert.AreEqual(1, test.CountBits(0, numBits));

        Assert.False(test.TestAll(0, 122));
        Assert.True(test.TestNone(0, 122));
        Assert.False(test.TestAny(0, 122));

        test.Clear();
        Assert.False(test.IsSet(123));
        Assert.AreEqual(0, test.CountBits(0, numBits));

        test.SetBits(40, true, 4);
        Assert.AreEqual(4, test.CountBits(0, numBits));

        test.SetBits(0, true, numBits);
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAll(0, numBits));

        test.SetBits(0, false, numBits);
        Assert.True(test.TestNone(0, numBits));
        Assert.False(test.TestAll(0, numBits));

        test.SetBits(123, true, 7);
        Assert.True(test.TestAll(123, 7));

        test.Dispose();
    }

    [Test]
    public unsafe void UnsafeBitArray_Throws()
    {
        var numBits = 256;

        using (var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory))
        {
            Assert.DoesNotThrow(() => { test.TestAll(0, numBits); });
            Assert.Throws<ArgumentException>(() => { test.IsSet(-1); });
            Assert.Throws<ArgumentException>(() => { test.IsSet(numBits); });

            // GetBits numBits must be 1-64.
            Assert.Throws<ArgumentException>(() => { test.GetBits(0, 0); });
            Assert.Throws<ArgumentException>(() => { test.GetBits(0, 65); });
            Assert.DoesNotThrow(() => { test.GetBits(63, 2); });

            Assert.Throws<ArgumentException>(() => { new UnsafeBitArray(null, 7); /* check sizeInBytes must be multiple of 8-bytes. */ });
        }
    }

    void GetBitsTest(ref UnsafeBitArray test, int pos, int numBits)
    {
        test.SetBits(pos, true, numBits);
        Assert.AreEqual(numBits, test.CountBits(0, test.Length));
        Assert.AreEqual(0xfffffffffffffffful >> (64 - numBits), test.GetBits(pos, numBits));
        test.Clear();
    }

    [Test]
    public void UnsafeBitArray_GetBits()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        GetBitsTest(ref test, 0, 5);
        GetBitsTest(ref test, 1, 3);
        GetBitsTest(ref test, 1, 64);
        GetBitsTest(ref test, 62, 5);
        GetBitsTest(ref test, 127, 3);

        test.Dispose();
    }

    void SetBitsTest(ref UnsafeBitArray test, int pos, ulong value, int numBits)
    {
        test.SetBits(pos, value, numBits);
        Assert.AreEqual(value, test.GetBits(pos, numBits));
        test.Clear();
    }

    [Test]
    public void UnsafeBitArray_SetBits()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        SetBitsTest(ref test, 0, 16, 5);
        SetBitsTest(ref test, 1, 7, 3);
        SetBitsTest(ref test, 1, 32, 64);
        SetBitsTest(ref test, 62, 6, 5);
        SetBitsTest(ref test, 127, 1, 3);
        SetBitsTest(ref test, 60, 0xaa, 8);

        test.Dispose();
    }
}