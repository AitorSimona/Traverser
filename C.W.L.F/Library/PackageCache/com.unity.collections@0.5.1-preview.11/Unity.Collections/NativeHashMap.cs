using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public struct NativeMultiHashMapIterator<TKey>
        where TKey : struct
    {
        internal TKey key;
        internal int NextEntryIndex;
        internal int EntryIndex;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public struct NativeKeyValueArrays<TKey, TValue> : IDisposable
        where TKey : struct
        where TValue : struct
    {
        public NativeArray<TKey> Keys;
        public NativeArray<TValue> Values;

        public NativeKeyValueArrays(int length, Allocator allocator, NativeArrayOptions options)
        {
            Keys = new NativeArray<TKey>(length, allocator, options);
            Values = new NativeArray<TValue>(length, allocator, options);
        }

        public void Dispose()
        {
            Keys.Dispose();
            Values.Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeHashMapDebuggerTypeProxy<,>))]
    public unsafe struct NativeHashMap<TKey, TValue> : IDisposable
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        UnsafeHashMap<TKey, TValue> m_HashMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="allocator"></param>
        public NativeHashMap(int capacity, Allocator allocator)
            : this(capacity, allocator, 2)
        {
        }

        NativeHashMap(int capacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            m_HashMapData = new UnsafeHashMap<TKey, TValue>(capacity, allocator, disposeSentinelStackDepth);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_HashMapData.Length;
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_HashMapData.Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_HashMapData.Capacity = value;
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_HashMapData.Clear();
        }

        /// <summary>
        /// Try adding an element with the specified key and value into the container. If the key already exist, the value won't be updated.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <returns>Returns true if value is added into the container, otherwise returns false.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return m_HashMapData.TryAdd(key, value);
        }

        /// <summary>
        /// Add an element with the specified key and value into the container. If the key already exist an ArgumentException will be thrown.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(TKey key, TValue value)
        {
            var added = m_HashMapData.TryAdd(key, value);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!added)
            {
                throw new ArgumentException("An item with the same key has already been added", nameof(key));
            }
#endif
        }

        /// <summary>
        /// Removes the element with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns true if the key was removed from the container, otherwise returns false indicating key wasn't in the container.</returns>
        public bool Remove(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return m_HashMapData.Remove(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool TryGetValue(TKey key, out TValue item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_HashMapData.TryGetValue(key, out item);
        }

        /// <summary>
        /// Determines whether an key is in the container.
        /// </summary>
        /// <param name="key">The key to locate in the container.</param>
        /// <returns>Returns true if the container contains the key.</returns>
        public bool ContainsKey(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_HashMapData.ContainsKey(key);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (m_HashMapData.TryGetValue(key, out res))
                {
                    return res;
                }
                else
                {
                    throw new ArgumentException($"Key: {key} is not present in the NativeHashMap.");
                }
#else
                m_HashMapData.TryGetValue(key, out res);
                return res;
#endif
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_HashMapData[key] = value;
            }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor.</remarks>
        public bool IsCreated => m_HashMapData.IsCreated;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            m_HashMapData.Dispose();
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            return m_HashMapData.Dispose(inputDeps);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_HashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_HashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_HashMapData.GetKeyValueArrays(allocator);
        }

        /// <summary>
        /// Returns parallel writer instance.
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;
            writer.m_Writer = m_HashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
#endif
            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            internal UnsafeHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Writer.Capacity;
                }
            }

            public bool TryAdd(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                return m_Writer.TryAdd(key, item);
            }
        }
    }

    internal sealed class NativeHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
#if !NET_DOTS
        private NativeHashMap<TKey, TValue> m_Target;

        public NativeHashMapDebuggerTypeProxy(NativeHashMap<TKey, TValue> target)
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
    [NativeContainer]
    [DebuggerTypeProxy(typeof(NativeMultiHashMapDebuggerTypeProxy<,>))]
    public unsafe struct NativeMultiHashMap<TKey, TValue> : IDisposable
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        internal UnsafeMultiHashMap<TKey, TValue> m_MultiHashMapData;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="allocator"></param>
        public NativeMultiHashMap(int capacity, Allocator allocator)
            : this(capacity, allocator, 2)
        {
        }

        NativeMultiHashMap(int capacity, Allocator allocator, int disposeSentinelStackDepth)
        {
            m_MultiHashMapData = new UnsafeMultiHashMap<TKey, TValue>(capacity, allocator, disposeSentinelStackDepth);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, disposeSentinelStackDepth, allocator);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_MultiHashMapData.Length;
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_MultiHashMapData.Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_MultiHashMapData.Capacity = value;
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        /// <remarks>Containers capacity remains unchanged.</remarks>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_MultiHashMapData.Clear();
        }

        /// <summary>
        /// Add an element with the specified key and value into the container. If the key already exist an ArgumentException will be thrown.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        public void Add(TKey key, TValue item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_MultiHashMapData.Add(key, item);
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns number of removed items.</returns>
        public int Remove(TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.Remove(key);
        }

        [BurstDiscard]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckValueEQ<TValueEQ>()
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
            if (typeof(TValueEQ) != typeof(TValue))
            {
                throw new System.ArgumentException($"value is type '{typeof(TValueEQ)}' but must match the HashMap value type '{typeof(TValue)}'.");
            }
        }

        /// <summary>
        /// Removes all elements with the specified key from the container.
        /// </summary>
        /// <typeparam name="TValueEQ"></typeparam>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>Returns number of removed items.</returns>
        public void Remove<TValueEQ>(TKey key, TValueEQ value)
            where TValueEQ : struct, IEquatable<TValueEQ>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            CheckValueEQ<TValueEQ>();
#endif
            m_MultiHashMapData.Remove(key, value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="it"></param>
        public void Remove(NativeMultiHashMapIterator<TKey> it)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            m_MultiHashMapData.Remove(it);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.TryGetFirstValue(key, out item, out it);
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.TryGetNextValue(out item, ref it);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="it"></param>
        /// <returns></returns>
        public bool SetValue(TValue item, NativeMultiHashMapIterator<TKey> it)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.SetValue(item, it);
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor.</remarks>
        public bool IsCreated => m_MultiHashMapData.IsCreated;

        /// <summary>
        /// Disposes of this multi-hashmap and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            m_MultiHashMapData.Dispose();
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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            return m_MultiHashMapData.Dispose(inputDeps);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TKey> GetKeyArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.GetKeyArray(allocator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeArray<TValue> GetValueArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.GetValueArray(allocator);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="allocator"></param>
        /// <returns></returns>
        public NativeKeyValueArrays<TKey, TValue> GetKeyValueArrays(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            return m_MultiHashMapData.GetKeyValueArrays(allocator);
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
            internal NativeMultiHashMap<TKey, TValue> hashmap;
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
            writer.m_Writer = m_MultiHashMapData.AsParallelWriter();
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            writer.m_Safety = m_Safety;
#endif
            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            internal UnsafeMultiHashMap<TKey, TValue>.ParallelWriter m_Writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            public int m_ThreadIndex => m_Writer.m_ThreadIndex;

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
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                    return m_Writer.Capacity;
                }
            }

            /// <summary>
            /// Add an element with the specified key and value into the container. If the key already exist an ArgumentException will be thrown.
            /// </summary>
            /// <param name="key">The key of the element to add.</param>
            /// <param name="value">The value of the element to add.</param>
            public void Add(TKey key, TValue item)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_Writer.Add(key, item);
            }
        }
    }

    internal sealed class NativeMultiHashMapDebuggerTypeProxy<TKey, TValue>
        where TKey : struct, IEquatable<TKey>, IComparable<TKey>
        where TValue : struct
    {
#if !NET_DOTS   
        private NativeMultiHashMap<TKey, TValue> m_Target;

        public NativeMultiHashMapDebuggerTypeProxy(NativeMultiHashMap<TKey, TValue> target)
        {
            m_Target = target;
        }

        public List<ListPair<TKey, List<TValue>>> Items
        {
            get
            {
                var result = new List<ListPair<TKey, List<TValue>>>();
                var keys = m_Target.GetUniqueKeyArray(Allocator.Temp);

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
    [JobProducerType(typeof(JobNativeMultiHashMapUniqueHashExtensions.NativeMultiHashMapUniqueHashJobStruct<>))]
    public interface IJobNativeMultiHashMapMergedSharedKeyIndices
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
    public static class JobNativeMultiHashMapUniqueHashExtensions
    {
        internal struct NativeMultiHashMapUniqueHashJobStruct<TJob>
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            [ReadOnly] public NativeMultiHashMap<int, int> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(NativeMultiHashMapUniqueHashJobStruct<TJob>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            private delegate void ExecuteJobFunction(ref NativeMultiHashMapUniqueHashJobStruct<TJob> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref NativeMultiHashMapUniqueHashJobStruct<TJob> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = fullData.HashMap.m_MultiHashMapData.m_Buffer;
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
        public static unsafe JobHandle Schedule<TJob>(this TJob jobData, NativeMultiHashMap<int, int> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapMergedSharedKeyIndices
        {
            var fullData = new NativeMultiHashMapUniqueHashJobStruct<TJob>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , NativeMultiHashMapUniqueHashJobStruct<TJob>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_MultiHashMapData.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyValue.NativeMultiHashMapVisitKeyValueJobStruct<,,>))]
    public interface IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void ExecuteNext(TKey key, TValue value);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class JobNativeMultiHashMapVisitKeyValue
    {
        internal struct NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            where TJob : struct, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [ReadOnly] public NativeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    UnsafeHashMapData* hashMapData = fullData.HashMap.m_MultiHashMapData.m_Buffer;
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
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapVisitKeyValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var fullData = new NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , NativeMultiHashMapVisitKeyValueJobStruct<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_MultiHashMapData.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }

#if !UNITY_DOTSPLAYER
    /// <summary>
    /// 
    /// </summary>
    public static class NativeHashMapExtensions
    {
        public static int Unique<T>(this NativeArray<T> array)
            where T : struct, IEquatable<T>
        {
            if (array.Length == 0)
            {
                return 0;
            }

            int first = 0;
            int last = array.Length;
            var result = first;
            while (++first != last)
            {
                if (!array[result].Equals(array[first]))
                {
                    array[++result] = array[first];
                }
            }

            return ++result;
        }

        public static Tuple<NativeArray<TKey>, int> GetUniqueKeyArray<TKey, TValue>(this NativeMultiHashMap<TKey, TValue> hashMap, Allocator allocator)
            where TKey : struct, IEquatable<TKey>, IComparable<TKey>
            where TValue : struct
        {
            var withDuplicates = hashMap.GetKeyArray(allocator);
            withDuplicates.Sort();
            int uniques = withDuplicates.Unique();
            return new Tuple<NativeArray<TKey>, int>(withDuplicates, uniques);
        }
    }
#endif

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [JobProducerType(typeof(JobNativeMultiHashMapVisitKeyMutableValue.NativeMultiHashMapVisitKeyMutableValueJobStruct<,,>))]
    public interface IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct
    {
        void ExecuteNext(TKey key, ref TValue value);
    }

    /// <summary>
    /// 
    /// </summary>
    public static class JobNativeMultiHashMapVisitKeyMutableValue
    {
        internal struct NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>
            where TJob : struct, IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            [NativeDisableContainerSafetyRestriction]
            internal NativeMultiHashMap<TKey, TValue> HashMap;
            internal TJob JobData;

            private static IntPtr jobReflectionData;

            internal static IntPtr Initialize()
            {
                if (jobReflectionData == IntPtr.Zero)
                {
                    jobReflectionData = JobsUtility.CreateJobReflectionData(typeof(NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>), typeof(TJob), JobType.ParallelFor, (ExecuteJobFunction)Execute);
                }

                return jobReflectionData;
            }

            internal delegate void ExecuteJobFunction(ref NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue> fullData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;

                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                    {
                        return;
                    }

                    var buckets = (int*)fullData.HashMap.m_MultiHashMapData.m_Buffer->buckets;
                    var nextPtrs = (int*)fullData.HashMap.m_MultiHashMapData.m_Buffer->next;
                    var keys = fullData.HashMap.m_MultiHashMapData.m_Buffer->keys;
                    var values = fullData.HashMap.m_MultiHashMapData.m_Buffer->values;

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
        public static unsafe JobHandle Schedule<TJob, TKey, TValue>(this TJob jobData, NativeMultiHashMap<TKey, TValue> hashMap, int minIndicesPerJobCount, JobHandle dependsOn = new JobHandle())
            where TJob : struct, IJobNativeMultiHashMapVisitKeyMutableValue<TKey, TValue>
            where TKey : struct, IEquatable<TKey>
            where TValue : struct
        {
            var fullData = new NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>
            {
                HashMap = hashMap,
                JobData = jobData
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                  UnsafeUtility.AddressOf(ref fullData)
                , NativeMultiHashMapVisitKeyMutableValueJobStruct<TJob, TKey, TValue>.Initialize()
                , dependsOn
                , ScheduleMode.Batched
                );

            return JobsUtility.ScheduleParallelFor(ref scheduleParams, hashMap.m_MultiHashMapData.m_Buffer->bucketCapacityMask + 1, minIndicesPerJobCount);
        }
    }
}
