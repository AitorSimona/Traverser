using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct UnsafeHashMapData
    {
        [FieldOffset(0)]
        internal byte* values;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        internal byte* keys;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(16)]
        internal byte* next;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(24)]
        internal byte* buckets;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(32)]
        internal int keyCapacity;

        [FieldOffset(36)]
        internal int bucketCapacityMask; // = bucket capacity - 1

        [FieldOffset(40)]
        internal int allocatedIndexLength;

        [FieldOffset(JobsUtility.CacheLineSize < 64 ? 64 : JobsUtility.CacheLineSize)]
        internal fixed int firstFreeTLS[JobsUtility.MaxJobThreadCount * IntsPerCacheLine];

        // 64 is the cache line size on x86, arm usually has 32 - so it is possible to save some memory there
        internal const int IntsPerCacheLine = JobsUtility.CacheLineSize / sizeof(int);

        internal static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        internal static int GrowCapacity(int capacity)
        {
            if (capacity == 0)
            {
                return 1;
            }

            return capacity * 2;
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void IsBlittableAndThrow<TKey, TValue>()
            where TKey : struct
            where TValue : struct
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();
        }

        internal static void AllocateHashMap<TKey, TValue>(int length, int bucketLength, Allocator label,
            out UnsafeHashMapData* outBuf)
            where TKey : struct
            where TValue : struct
        {
            IsBlittableAndThrow<TKey, TValue>();

            UnsafeHashMapData* data = (UnsafeHashMapData*)UnsafeUtility.Malloc(sizeof(UnsafeHashMapData), UnsafeUtility.AlignOf<UnsafeHashMapData>(), label);

            bucketLength = math.ceilpow2(bucketLength);

            data->keyCapacity = length;
            data->bucketCapacityMask = bucketLength - 1;

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(length, bucketLength, out keyOffset, out nextOffset, out bucketOffset);

            data->values = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            data->keys = data->values + keyOffset;
            data->next = data->values + nextOffset;
            data->buckets = data->values + bucketOffset;

            outBuf = data;
        }

        internal static void ReallocateHashMap<TKey, TValue>(UnsafeHashMapData* data, int newCapacity, int newBucketCapacity, Allocator label)
            where TKey : struct
            where TValue : struct
        {
            newBucketCapacity = math.ceilpow2(newBucketCapacity);

            if (data->keyCapacity == newCapacity && (data->bucketCapacityMask + 1) == newBucketCapacity)
            {
                return;
            }

            if (data->keyCapacity > newCapacity)
            {
                throw new Exception("Shrinking a hash map is not supported");
            }

            int keyOffset, nextOffset, bucketOffset;
            int totalSize = CalculateDataSize<TKey, TValue>(newCapacity, newBucketCapacity, out keyOffset, out nextOffset, out bucketOffset);

            byte* newData = (byte*)UnsafeUtility.Malloc(totalSize, JobsUtility.CacheLineSize, label);
            byte* newKeys = newData + keyOffset;
            byte* newNext = newData + nextOffset;
            byte* newBuckets = newData + bucketOffset;

            // The items are taken from a free-list and might not be tightly packed, copy all of the old capcity
            UnsafeUtility.MemCpy(newData, data->values, data->keyCapacity * UnsafeUtility.SizeOf<TValue>());
            UnsafeUtility.MemCpy(newKeys, data->keys, data->keyCapacity * UnsafeUtility.SizeOf<TKey>());
            UnsafeUtility.MemCpy(newNext, data->next, data->keyCapacity * UnsafeUtility.SizeOf<int>());

            for (int emptyNext = data->keyCapacity; emptyNext < newCapacity; ++emptyNext)
            {
                ((int*)newNext)[emptyNext] = -1;
            }

            // re-hash the buckets, first clear the new bucket list, then insert all values from the old list
            for (int bucket = 0; bucket < newBucketCapacity; ++bucket)
            {
                ((int*)newBuckets)[bucket] = -1;
            }

            for (int bucket = 0; bucket <= data->bucketCapacityMask; ++bucket)
            {
                int* buckets = (int*)data->buckets;
                int* nextPtrs = (int*)newNext;
                while (buckets[bucket] >= 0)
                {
                    int curEntry = buckets[bucket];
                    buckets[bucket] = nextPtrs[curEntry];
                    int newBucket = UnsafeUtility.ReadArrayElement<TKey>(data->keys, curEntry).GetHashCode() & (newBucketCapacity - 1);
                    nextPtrs[curEntry] = ((int*)newBuckets)[newBucket];
                    ((int*)newBuckets)[newBucket] = curEntry;
                }
            }

            UnsafeUtility.Free(data->values, label);
            if (data->allocatedIndexLength > data->keyCapacity)
            {
                data->allocatedIndexLength = data->keyCapacity;
            }

            data->values = newData;
            data->keys = newKeys;
            data->next = newNext;
            data->buckets = newBuckets;
            data->keyCapacity = newCapacity;
            data->bucketCapacityMask = newBucketCapacity - 1;
        }

        internal static void DeallocateHashMap(UnsafeHashMapData* data, Allocator allocation)
        {
            UnsafeUtility.Free(data->values, allocation);
            UnsafeUtility.Free(data, allocation);
        }

        internal static int CalculateDataSize<TKey, TValue>(int length, int bucketLength, out int keyOffset, out int nextOffset, out int bucketOffset)
            where TKey : struct
            where TValue : struct
        {
            int elementSize = UnsafeUtility.SizeOf<TValue>();
            int keySize = UnsafeUtility.SizeOf<TKey>();

            // Offset is rounded up to be an even cacheLineSize
            keyOffset = (elementSize * length + JobsUtility.CacheLineSize - 1);
            keyOffset -= keyOffset % JobsUtility.CacheLineSize;

            nextOffset = (keyOffset + keySize * length + JobsUtility.CacheLineSize - 1);
            nextOffset -= nextOffset % JobsUtility.CacheLineSize;

            bucketOffset = (nextOffset + UnsafeUtility.SizeOf<int>() * length + JobsUtility.CacheLineSize - 1);
            bucketOffset -= bucketOffset % JobsUtility.CacheLineSize;

            int totalSize = bucketOffset + UnsafeUtility.SizeOf<int>() * bucketLength;
            return totalSize;
        }

        internal static void GetKeyArray<TKey>(UnsafeHashMapData* data, NativeArray<TKey> result)
            where TKey : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result[o++] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, b);
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Length, o);
        }

        internal static void GetValueArray<TValue>(UnsafeHashMapData* data, NativeArray<TValue> result)
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result[o++] = UnsafeUtility.ReadArrayElement<TValue>(data->values, b);
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Length, o);
        }

        internal static void GetKeyValueArrays<TKey, TValue>(UnsafeHashMapData* data, NativeKeyValueArrays<TKey, TValue> result)
            where TKey : struct
            where TValue : struct
        {
            var bucketArray = (int*)data->buckets;
            var bucketNext = (int*)data->next;

            int o = 0;
            for (int i = 0; i <= data->bucketCapacityMask; ++i)
            {
                int b = bucketArray[i];

                while (b != -1)
                {
                    result.Keys[o] = UnsafeUtility.ReadArrayElement<TKey>(data->keys, b);
                    result.Values[o] = UnsafeUtility.ReadArrayElement<TValue>(data->values, b);
                    o++;
                    b = bucketNext[b];
                }
            }

            Assert.AreEqual(result.Keys.Length, o);
            Assert.AreEqual(result.Values.Length, o);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct UnsafeHashMapBase<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal static unsafe void Clear(UnsafeHashMapData* data)
        {
            UnsafeUtility.MemSet(data->buckets, 0xff, (data->bucketCapacityMask + 1) * 4);
            UnsafeUtility.MemSet(data->next, 0xff, (data->keyCapacity) * 4);

            for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
            {
                data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] = -1;
            }

            data->allocatedIndexLength = 0;
        }

        internal static unsafe int AllocEntry(UnsafeHashMapData* data, int threadIndex)
        {
            int idx;
            int* nextPtrs = (int*)data->next;

            do
            {
                idx = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine];

                if (idx < 0)
                {
                    // Try to refill local cache
                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -2);

                    // If it failed try to get one from the never-allocated array
                    if (data->allocatedIndexLength < data->keyCapacity)
                    {
                        idx = Interlocked.Add(ref data->allocatedIndexLength, 16) - 16;

                        if (idx < data->keyCapacity - 1)
                        {
                            int count = Math.Min(16, data->keyCapacity - idx);

                            for (int i = 1; i < count; ++i)
                            {
                                nextPtrs[idx + i] = idx + i + 1;
                            }

                            nextPtrs[idx + count - 1] = -1;
                            nextPtrs[idx] = -1;
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], idx + 1);

                            return idx;
                        }

                        if (idx == data->keyCapacity - 1)
                        {
                            Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -1);

                            return idx;
                        }
                    }

                    Interlocked.Exchange(ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine], -1);

                    // Failed to get any, try to get one from another free list
                    bool again = true;
                    while (again)
                    {
                        again = false;
                        for (int other = (threadIndex + 1) % JobsUtility.MaxJobThreadCount
                            ; other != threadIndex
                            ; other = (other + 1) % JobsUtility.MaxJobThreadCount
                            )
                        {
                            do
                            {
                                idx = data->firstFreeTLS[other * UnsafeHashMapData.IntsPerCacheLine];

                                if (idx < 0)
                                {
                                    break;
                                }

                            }
                            while (Interlocked.CompareExchange(
                                      ref data->firstFreeTLS[other * UnsafeHashMapData.IntsPerCacheLine]
                                    , nextPtrs[idx]
                                    , idx
                                    ) != idx
                                    );

                            if (idx == -2)
                            {
                                again = true;
                            }
                            else if (idx >= 0)
                            {
                                nextPtrs[idx] = -1;
                                return idx;
                            }
                        }
                    }
                    throw new InvalidOperationException("HashMap is full");
                }

                if (idx >= data->keyCapacity)
                {
                    throw new InvalidOperationException(string.Format("nextPtr idx {0} beyond capacity {1}", idx, data->keyCapacity));
                }
            }
            while (Interlocked.CompareExchange(
                      ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine]
                    , nextPtrs[idx]
                    , idx
                    ) != idx
                    );

            nextPtrs[idx] = -1;
            return idx;
        }

        internal static unsafe bool TryAddAtomic(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            TValue tempItem;
            NativeMultiHashMapIterator<TKey> tempIt;
            if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                return false;
            }

            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            if (Interlocked.CompareExchange(ref buckets[bucket], idx, -1) != -1)
            {
                int* nextPtrs = (int*)data->next;

                do
                {
                    nextPtrs[idx] = buckets[bucket];

                    if (TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
                    {
                        // Put back the entry in the free list if someone else added it while trying to add
                        do
                        {
                            nextPtrs[idx] = data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine];
                        }
                        while (Interlocked.CompareExchange(
                                  ref data->firstFreeTLS[threadIndex * UnsafeHashMapData.IntsPerCacheLine]
                                , idx
                                , nextPtrs[idx]
                                ) != nextPtrs[idx]
                                );

                        return false;
                    }
                }
                while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtrs[idx]) != nextPtrs[idx]);
            }

            return true;
        }

        internal static unsafe void AddAtomicMulti(UnsafeHashMapData* data, TKey key, TValue item, int threadIndex)
        {
            // Allocate an entry from the free list
            int idx = AllocEntry(data, threadIndex);

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;

            int nextPtr;
            int* nextPtrs = (int*)data->next;
            do
            {
                nextPtr = buckets[bucket];
                nextPtrs[idx] = nextPtr;
            }
            while (Interlocked.CompareExchange(ref buckets[bucket], idx, nextPtr) != nextPtr);
        }

        internal static unsafe bool TryAdd(UnsafeHashMapData* data, TKey key, TValue item, bool isMultiHashMap, Allocator allocation)
        {
            TValue tempItem;
            NativeMultiHashMapIterator<TKey> tempIt;
            if (!isMultiHashMap && TryGetFirstValueAtomic(data, key, out tempItem, out tempIt))
            {
                return false;
            }

            // Allocate an entry from the free list
            int idx;
            int* nextPtrs;

            if (data->allocatedIndexLength >= data->keyCapacity && data->firstFreeTLS[0] < 0)
            {
                for (int tls = 1; tls < JobsUtility.MaxJobThreadCount; ++tls)
                {
                    if (data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] >= 0)
                    {
                        idx = data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine];
                        nextPtrs = (int*)data->next;
                        data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine] = nextPtrs[idx];
                        nextPtrs[idx] = -1;
                        data->firstFreeTLS[0] = idx;
                        break;
                    }
                }

                if (data->firstFreeTLS[0] < 0)
                {
                    int newCap = UnsafeHashMapData.GrowCapacity(data->keyCapacity);
                    UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, newCap, UnsafeHashMapData.GetBucketSize(newCap), allocation);
                }
            }

            idx = data->firstFreeTLS[0];

            if (idx >= 0)
            {
                data->firstFreeTLS[0] = ((int*)data->next)[idx];
            }
            else
            {
                idx = data->allocatedIndexLength++;
            }

            if (idx < 0 || idx >= data->keyCapacity)
            {
                throw new InvalidOperationException("Internal HashMap error");
            }

            // Write the new value to the entry
            UnsafeUtility.WriteArrayElement(data->keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->values, idx, item);

            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            // Add the index to the hash-map
            int* buckets = (int*)data->buckets;
            nextPtrs = (int*)data->next;

            nextPtrs[idx] = buckets[bucket];
            buckets[bucket] = idx;

            return true;
        }

        internal static unsafe int Remove(UnsafeHashMapData* data, TKey key, bool isMultiHashMap)
        {
            var removed = 0;

            // First find the slot based on the hash
            var buckets = (int*)data->buckets;
            var nextPtrs = (int*)data->next;
            var bucket = key.GetHashCode() & data->bucketCapacityMask;
            var prevEntry = -1;
            var entryIdx = buckets[bucket];

            while (entryIdx >= 0 && entryIdx < data->keyCapacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(key))
                {
                    ++removed;

                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        buckets[bucket] = nextPtrs[entryIdx];
                    }
                    else
                    {
                        nextPtrs[prevEntry] = nextPtrs[entryIdx];
                    }

                    // And free the index
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = data->firstFreeTLS[0];
                    data->firstFreeTLS[0] = entryIdx;
                    entryIdx = nextIdx;

                    // Can only be one hit in regular hashmaps, so return
                    if (!isMultiHashMap)
                    {
                        break;
                    }
                }
                else
                {
                    prevEntry = entryIdx;
                    entryIdx = nextPtrs[entryIdx];
                }
            }

            return removed;
        }

        internal static unsafe void Remove(UnsafeHashMapData* data, NativeMultiHashMapIterator<TKey> it)
        {
            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int* nextPtrs = (int*)data->next;
            int bucket = it.key.GetHashCode() & data->bucketCapacityMask;

            int entryIdx = buckets[bucket];

            if (entryIdx == it.EntryIndex)
            {
                buckets[bucket] = nextPtrs[entryIdx];
            }
            else
            {
                while (entryIdx >= 0 && nextPtrs[entryIdx] != it.EntryIndex)
                {
                    entryIdx = nextPtrs[entryIdx];
                }

                if (entryIdx < 0)
                {
                    throw new InvalidOperationException("Invalid iterator passed to HashMap remove");
                }

                nextPtrs[entryIdx] = nextPtrs[it.EntryIndex];
            }

            // And free the index
            nextPtrs[it.EntryIndex] = data->firstFreeTLS[0];
            data->firstFreeTLS[0] = it.EntryIndex;
        }

        internal static unsafe void RemoveKeyValue<TValueEQ>(UnsafeHashMapData* data, TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            var buckets = (int*)data->buckets;
            var keyCapacity = (uint)data->keyCapacity;
            var prevNextPtr = buckets + (key.GetHashCode() & data->bucketCapacityMask);
            var entryIdx = *prevNextPtr;

            if ((uint)entryIdx >= keyCapacity)
            {
                return;
            }

            var nextPtrs = (int*)data->next;
            var keys = data->keys;
            var values = data->values;
            var firstFreeTLS = data->firstFreeTLS;

            do
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(keys, entryIdx).Equals(key)
                && UnsafeUtility.ReadArrayElement<TValueEQ>(values, entryIdx).Equals(value))
                {
                    int nextIdx = nextPtrs[entryIdx];
                    nextPtrs[entryIdx] = firstFreeTLS[0];
                    firstFreeTLS[0] = entryIdx;
                    *prevNextPtr = entryIdx = nextIdx;
                }
                else
                {
                    prevNextPtr = nextPtrs + entryIdx;
                    entryIdx = *prevNextPtr;
                }
            }
            while ((uint)entryIdx < keyCapacity);
        }

        internal static unsafe bool TryGetFirstValueAtomic(UnsafeHashMapData* data, TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            it.key = key;

            if (data->allocatedIndexLength <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                item = default(TValue);
                return false;
            }

            // First find the slot based on the hash
            int* buckets = (int*)data->buckets;
            int bucket = key.GetHashCode() & data->bucketCapacityMask;
            it.EntryIndex = it.NextEntryIndex = buckets[bucket];
            return TryGetNextValueAtomic(data, out item, ref it);
        }

        internal static unsafe bool TryGetNextValueAtomic(UnsafeHashMapData* data, out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            int entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;
            item = default(TValue);
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            int* nextPtrs = (int*)data->next;
            while (!UnsafeUtility.ReadArrayElement<TKey>(data->keys, entryIdx).Equals(it.key))
            {
                entryIdx = nextPtrs[entryIdx];
                if (entryIdx < 0 || entryIdx >= data->keyCapacity)
                {
                    return false;
                }
            }

            it.NextEntryIndex = nextPtrs[entryIdx];
            it.EntryIndex = entryIdx;

            // Read the value
            item = UnsafeUtility.ReadArrayElement<TValue>(data->values, entryIdx);

            return true;
        }

        internal static unsafe bool SetValue(UnsafeHashMapData* data, ref NativeMultiHashMapIterator<TKey> it, ref TValue item)
        {
            int entryIdx = it.EntryIndex;
            if (entryIdx < 0 || entryIdx >= data->keyCapacity)
            {
                return false;
            }

            UnsafeUtility.WriteArrayElement(data->values, entryIdx, item);
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeHashMapDebuggerTypeProxy<,>))]
    public unsafe struct UnsafeHashMap<TKey, TValue> : IDisposable
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        UnsafeHashMapData* m_Buffer;
        Allocator m_AllocatorLabel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="allocator"></param>
        public UnsafeHashMap(int capacity, Allocator allocator)
            : this(capacity, allocator, 2)
        {
        }

        internal UnsafeHashMap(int capacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            CollectionHelper.CheckIsUnmanaged<TKey>();
            CollectionHelper.CheckIsUnmanaged<TValue>();

            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);

            Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Length
        {
            get
            {
                UnsafeHashMapData* data = m_Buffer;
                int* nextPtrs = (int*)data->next;
                int freeListSize = 0;

                for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
                {
                    for (int freeIdx = data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine]
                        ; freeIdx >= 0
                        ; freeIdx = nextPtrs[freeIdx]
                        )
                    {
                        ++freeListSize;
                    }
                }

                return Math.Min(data->keyCapacity, data->allocatedIndexLength) - freeListSize;
            }
        }

        /// <summary>
        /// The number of items that can fit in the container.
        /// </summary>
        /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory.</remarks>
        public int Capacity
        {
            get
            {
                UnsafeHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeHashMapData* data = m_Buffer;
                UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
            UnsafeHashMapBase<TKey, TValue>.Clear(m_Buffer);
        }

        /// <summary>
        /// Try adding an element with the specified key and value into the container. If the key already exist, the value won't be updated.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>Returns true if value is added into the container, otherwise returns false.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, value, false, m_AllocatorLabel);
        }

        /// <summary>
        /// Add an element with the specified key and value into the container.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            TryAdd(key, item);
        }

        /// <summary>
        /// Removes the element with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns true if the key was removed from the container, otherwise returns false indicating key wasn't in the container.</returns>
        public bool Remove(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, key, false) != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
            NativeMultiHashMapIterator<TKey> tempIt;
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out tempIt);
        }

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var tempValue, out var tempIt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                TValue res;
                TryGetValue(key, out res);
                return res;
            }

            set
            {
                if (UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out var item, out var iterator))
                {
                    UnsafeHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref iterator, ref value);
                }
                else
                {
                    UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, value, false, m_AllocatorLabel);
                }
            }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor.</remarks>
        public bool IsCreated => m_Buffer != null;

        void Deallocate()
        {
            UnsafeHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            Deallocate();
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="jobHandle">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        [BurstCompile]
        struct DisposeJob : IJob
        {
            public UnsafeHashMap<TKey, TValue> Container;

            public void Execute()
            {
                Container.Deallocate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
            var result = new NativeArray<TKey>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
            var result = new NativeArray<TValue>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
#if UNITY_DOTSPLAYER
            writer.m_ThreadIndex = -1;   // aggressively check that code-gen has patched the ThreadIndex
#else
            writer.m_ThreadIndex = 0;    //
#endif
            writer.m_Buffer = m_Buffer;
            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// The number of items that can fit in the container.
            /// </summary>
            /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
            /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
            /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
            /// old array to the new one, and then deallocates the original array memory.</remarks>
            public int Capacity
            {
                get
                {
                    UnsafeHashMapData* data = m_Buffer;
                    return data->keyCapacity;
                }
            }

            /// <summary>
            /// Try adding an element with the specified key and value into the container. If the key already exist, the value won't be updated.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="value">The value of the element to add.</param>
            /// <returns>Returns true if value is added into the container, otherwise returns false.</returns>
            public bool TryAdd(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                return UnsafeHashMapBase<TKey, TValue>.TryAddAtomic(m_Buffer, key, item, m_ThreadIndex);
            }
        }
    }

    sealed internal class UnsafeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        private UnsafeHashMap<TKey, TValue> m_Target;

        public UnsafeHashMapDebuggerTypeProxy(UnsafeHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<Pair<TKey, TValue>> Items
        {
            get
            {
                var result = new List<Pair<TKey, TValue>>();
                using (var keys = m_Target.GetKeyArray(Allocator.Temp))
                {
                    for (var k = 0; k < keys.Length; ++k)
                    {
                        if (m_Target.TryGetValue(keys[k], out var value))
                        {
                            result.Add(new Pair<TKey, TValue>(keys[k], value));
                        }
                    }
                }
                return result;
            }
        }
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct UnsafeMultiHashMap<TKey, TValue> : IDisposable
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal UnsafeHashMapData* m_Buffer;
        Allocator m_AllocatorLabel;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="allocator"></param>
        public UnsafeMultiHashMap(int capacity, Allocator allocator)
            : this(capacity, allocator, 2)
        {
        }

        internal UnsafeMultiHashMap(int capacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            m_AllocatorLabel = allocator;
            // Bucket size if bigger to reduce collisions
            UnsafeHashMapData.AllocateHashMap<TKey, TValue>(capacity, capacity * 2, allocator, out m_Buffer);
            Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public int Length
        {
            get
            {
                UnsafeHashMapData* data = m_Buffer;
                int* nextPtrs = (int*)data->next;
                int freeListSize = 0;

                for (int tls = 0; tls < JobsUtility.MaxJobThreadCount; ++tls)
                {
                    for (int freeIdx = data->firstFreeTLS[tls * UnsafeHashMapData.IntsPerCacheLine]
                        ; freeIdx >= 0
                        ; freeIdx = nextPtrs[freeIdx]
                        )
                    {
                        ++freeListSize;
                    }
                }

                return Math.Min(data->keyCapacity, data->allocatedIndexLength) - freeListSize;
            }
        }

        /// <summary>
        /// The number of items that can fit in the container.
        /// </summary>
        /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
        /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
        /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
        /// old array to the new one, and then deallocates the original array memory.</remarks>
        public int Capacity
        {
            get
            {
                UnsafeHashMapData* data = m_Buffer;
                return data->keyCapacity;
            }

            set
            {
                UnsafeHashMapData* data = m_Buffer;
                UnsafeHashMapData.ReallocateHashMap<TKey, TValue>(data, value, UnsafeHashMapData.GetBucketSize(value), m_AllocatorLabel);
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
            UnsafeHashMapBase<TKey, TValue>.Clear(m_Buffer);
        }

        /// <summary>
        /// Add an element with the specified key and value into the container.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
            UnsafeHashMapBase<TKey, TValue>.TryAdd(m_Buffer, key, item, true, m_AllocatorLabel);
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns number of removed items.</returns>
        public int Remove(TKey key)
        {
            return UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, key, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TValueEQ"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Remove<TValueEQ>(TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            UnsafeHashMapBase<TKey, TValueEQ>.RemoveKeyValue(m_Buffer, key, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        public void Remove(NativeMultiHashMapIterator<TKey> it)
        {
            UnsafeHashMapBase<TKey, TValue>.Remove(m_Buffer, it);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <param name="it"></param>
        /// <returns></returns>
        public bool TryGetFirstValue(TKey key, out TValue item, out NativeMultiHashMapIterator<TKey> it)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryGetFirstValueAtomic(m_Buffer, key, out item, out it);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            return TryGetFirstValue(key, out var temp0, out var temp1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int CountValuesForKey(TKey key)
        {
            if (!TryGetFirstValue(key, out var value, out var iterator))
            {
                return 0;
            }

            var count = 1;
            while (TryGetNextValue(out value, ref iterator))
            {
                count++;
            }

            return count;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="it"></param>
        /// <returns></returns>
        public bool TryGetNextValue(out TValue item, ref NativeMultiHashMapIterator<TKey> it)
        {
            return UnsafeHashMapBase<TKey, TValue>.TryGetNextValueAtomic(m_Buffer, out item, ref it);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="it"></param>
        /// <returns></returns>
        public bool SetValue(TValue item, NativeMultiHashMapIterator<TKey> it)
        {
            return UnsafeHashMapBase<TKey, TValue>.SetValue(m_Buffer, ref it, ref item);
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor.</remarks>
        public bool IsCreated => m_Buffer != null;

        void Deallocate()
        {
            UnsafeHashMapData.DeallocateHashMap(m_Buffer, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Disposes of this multi-hashmap and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
            Deallocate();
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="jobHandle">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);
            m_Buffer = null;
            return jobHandle;
        }

        [BurstCompile]
        struct DisposeJob : IJob
        {
            public UnsafeMultiHashMap<TKey, TValue> Container;

            public void Execute()
            {
                Container.Deallocate();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
            var result = new NativeArray<TKey>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
            var result = new NativeArray<TValue>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetValueArray(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
            var result = new NativeKeyValueArrays<TKey, TValue>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeHashMapData.GetKeyValueArrays(m_Buffer, result);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Enumerator GetValuesForKey(TKey key)
        {
            return new Enumerator { hashmap = this, key = key, isFirst = true };
        }

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator<TValue>
        {
            internal UnsafeMultiHashMap<TKey, TValue> hashmap;
            internal TKey key;
            internal bool isFirst;

            TValue value;
            NativeMultiHashMapIterator<TKey> iterator;

            /// <summary>
            /// 
            /// </summary>
            public void Dispose() { }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                //Avoids going beyond the end of the collection.
                if (isFirst)
                {
                    isFirst = false;
                    return hashmap.TryGetFirstValue(key, out value, out iterator);
                }

                return hashmap.TryGetNextValue(out value, ref iterator);
            }

            /// <summary>
            /// 
            /// </summary>
            public void Reset() => isFirst = true;

            /// <summary>
            /// 
            /// </summary>
            public TValue Current => value;

            object IEnumerator.Current => throw new InvalidOperationException("Use IEnumerator<T> to avoid boxing");

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public Enumerator GetEnumerator()
            {
                return this;
            }
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

#if UNITY_DOTSPLAYER
            writer.m_ThreadIndex = -1;    // aggressively check that code-gen has patched the ThreadIndex
#else
            writer.m_ThreadIndex = 0;
#endif
            writer.m_Buffer = m_Buffer;

            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal UnsafeHashMapData* m_Buffer;

            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// The number of items that can fit in the container.
            /// </summary>
            /// <value>The number of items that the container can hold before it resizes its internal storage.</value>
            /// <remarks>Capacity specifies the number of items the container can currently hold. You can change Capacity
            /// to fit more or fewer items. Changing Capacity creates a new array of the specified size, copies the
            /// old array to the new one, and then deallocates the original array memory.</remarks>
            public int Capacity
            {
                get
                {
                    return m_Buffer->keyCapacity;
                }
            }

            /// <summary>
            /// Add an element with the specified key and value into the container.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="value">The value of the element to add.</param>
            public void Add(TKey key, TValue item)
            {
                Assert.IsTrue(m_ThreadIndex >= 0);
                UnsafeHashMapBase<TKey, TValue>.AddAtomicMulti(m_Buffer, key, item, m_ThreadIndex);
            }
        }
    }

    internal sealed class UnsafeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>, IComparable<TKey>
        where TValue : struct
    {
#if !NET_DOTS   
        private UnsafeMultiHashMap<TKey, TValue> m_Target;

        public UnsafeMultiHashMapDebuggerTypeProxy(UnsafeMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public static Tuple<NativeArray<TKey>, int> GetUniqueKeyArray(ref UnsafeMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
        {
            var withDuplicates = hashMap.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return new Tuple<NativeArray<TKey>, int>(withDuplicates, uniques);
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = GetUniqueKeyArray(ref m_Target, Allocator.Temp);

                using (keys.Item1)
                {
                    for (var k = 0; k < keys.Item2; ++k)
                    {
                        var values = new List<TValue>();
                        if (m_Target.TryGetFirstValue(keys.Item1[k], out var value, out var iterator))
                        {
                            do
                            {
                                values.Add(value);
                            }
                            while (m_Target.TryGetNextValue(out value, ref iterator));
                        }

                        result.Add(new ListPair<TKey, List<TValue>>(keys.Item1[k], values));
                    }
                }

                return result;
            }
        }
#endif
    }
}

namespace Unity.Collections
{
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// 
    /// </summary>
    // IJobNativeMultiHashMapMergedSharedKeyIndices: custom job type, following its own defined custom safety rules:
    // A) because we know how hashmap safety works, B) we can iterate safely in parallel
    // Notable Features:
    // 1) The hash map must be a NativeMultiHashMap<int,int>, where the key is a hash of some data, and the index is
    // a unique index (generally to the relevant data in some other collection).
    // 2) Each bucket is processed concurrently with other buckets.
    // 3) All key/value pairs in each bucket are processed individually (in sequential order) by a single thread.
    [JobProducerType(typeof(JobUnsafeMultiHashMapUniqueHashExtensions.UnsafeMultiHashMapUniqueHashJobStruct<>))]
    public interface IJobUnsafeMultiHashMapMergedSharedKeyIndices
    {
        // The first time each key (=hash) is encountered, ExecuteFirst() is invoked with corresponding value (=index).
        void ExecuteFirst(int index);

        // For each subsequent instance of the same key in the bucket, ExecuteNext() is invoked with the corresponding
        // value (=index) for that key, as well as the value passed to ExecuteFirst() the first time this key
        // was encountered (=firstIndex).
        void ExecuteNext(int firstIndex, int index);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class JobUnsafeMultiHashMapUniqueHashExtensions
    {
        internal struct UnsafeMultiHashMapUniqueHashJobStruct<TJob>
            where TJob : struct, IJobUnsafeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public UnsafeMultiHashMap<int, int> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(UnsafeMultiHashMapUniqueHashJobStruct<TJob>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            private delegate void ExecuteJobFunction(ref UnsafeMultiHashMapUniqueHashJobStruct<TJob> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref UnsafeMultiHashMapUniqueHashJobStruct<TJob> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = fullData.HashMap.m_Buffer;
                    var buckets = (int*)hashMapData->buckets;
                    var nextPtrs = (int*)hashMapData->next;
                    var keys = hashMapData->keys;
                    var values = hashMapData->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<int>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<int>(values, entryIndex);
                            int firstValue;

                            NativeMultiHashMapIterator<int> it;
                            fullData.HashMap.TryGetFirstValue(key, out firstValue, out it);

                            // [macton] Didn't expect a usecase for this with multiple same values
                            // (since it's intended use was for unique indices.)
                            // https://forum.unity.com/threads/ijobnativemultihashmapmergedsharedkeyindices-unexpected-behavior.569107/#post-3788170
                            if (entryIndex == it.EntryIndex)
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref fullData), value, 1);
#endif
                                fullData.JobData.ExecuteFirst(value);
                            }
                            else
                            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                                var startIndex = Math.Min(firstValue, value);
                                var lastIndex = Math.Max(firstValue, value);
                                var rangeLength = (lastIndex - startIndex) + 1;

                                JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref fullData), startIndex, rangeLength);
#endif
                                fullData.JobData.ExecuteNext(firstValue, value);
                            }

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, UnsafeMultiHashMap<int, int> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapMergedSharedKeyIndices
        {
            var fullData = new UnsafeMultiHashMapUniqueHashJobStruct<TJob>
            {
                HashMap = hashMap,
                JobData = jobData,
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , UnsafeMultiHashMapUniqueHashJobStruct<TJob>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [JobProducerType(typeof(JobUnsafeMultiHashMapVisitKeyValue.UnsafeMultiHashMapVisitKeyValueJobStruct<,,>))]
    public interface IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class JobUnsafeMultiHashMapVisitKeyValue
    {
        internal struct UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [ReadOnly] public UnsafeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = fullData.HashMap.m_Buffer;
                    var buckets = (int*)hashMapData->buckets;
                    var nextPtrs = (int*)hashMapData->next;
                    var keys = hashMapData->keys;
                    var values = hashMapData->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);
                            var value = UnsafeUtility.ReadArrayElement<TValue>(values, entryIndex);

                            fullData.JobData.ExecuteNext(key, value);

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, UnsafeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var fullData = new UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , UnsafeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [JobProducerType(typeof(JobUnsafeMultiHashMapVisitKeyMutableValue.UnsafeMultiHashMapVisitKeyMutableValueJobStruct<,,>))]
    public interface IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void ExecuteNext(TKey key, ref TValue value);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class JobUnsafeMultiHashMapVisitKeyMutableValue
    {
        internal struct UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [NativeDisableContainerSafetyRestriction]
            internal UnsafeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            internal static unsafe void Execute(ref UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var buckets = (int*)fullData.HashMap.m_Buffer->buckets;
                    var nextPtrs = (int*)fullData.HashMap.m_Buffer->next;
                    var keys = fullData.HashMap.m_Buffer->keys;
                    var values = fullData.HashMap.m_Buffer->values;

                    for (int i = begin; i < end; i++)
                    {
                        int entryIndex = buckets[i];

                        while (entryIndex != -1)
                        {
                            var key = UnsafeUtility.ReadArrayElement<TKey>(keys, entryIndex);

                            fullData.JobData.ExecuteNext(key, ref UnsafeUtilityEx.ArrayElementAsRef<TValue>(values, entryIndex));

                            entryIndex = nextPtrs[entryIndex];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TJob"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="jobData"></param>
        /// <param name="hashMap"></param>
        /// <param name="minIndicesPerJobCount"></param>
        /// <param name="dependsOn"></param>
        /// <returns></returns>
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, UnsafeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobUnsafeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var fullData = new UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , UnsafeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }
}
