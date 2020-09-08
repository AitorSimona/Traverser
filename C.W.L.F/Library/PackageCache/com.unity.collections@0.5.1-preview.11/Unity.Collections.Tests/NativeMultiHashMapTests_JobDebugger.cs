using NUnit.Framework;
using System;
using Unity.Jobs;
using Unity.Collections;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
public class NativeMultiHashMapTests_JobDebugger : NativeMultiHashMapTestsFixture
{
    [Test]
    public void NativeMultiHashMap_Read_And_Write_Without_Fences()
    {
        var hashMap = new NativeMultiHashMap<int, int>(hashMapSize, Allocator.TempJob);
        var writeStatus = new NativeArray<int>(hashMapSize, Allocator.TempJob);
        var readValues = new NativeArray<int>(hashMapSize, Allocator.TempJob);

        var writeData = new MultiHashMapWriteParallelForJob()
        {
            hashMap = hashMap.AsParallelWriter(),
            status = writeStatus,
            keyMod = hashMapSize,
        };

        var readData = new MultiHashMapReadParallelForJob()
        {
            hashMap = hashMap,
            values = readValues,
            keyMod = writeData.keyMod,
        };

        var writeJob = writeData.Schedule(hashMapSize, 1);
        Assert.Throws<InvalidOperationException> (() => { readData.Schedule(hashMapSize, 1); } );
        writeJob.Complete();

        hashMap.Dispose();
        writeStatus.Dispose();
        readValues.Dispose();
    }
}
#endif
