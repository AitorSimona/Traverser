using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// CacheIndex is used in AnimationJobCache to recover the index to the cached data.
    /// </summary>
    public struct CacheIndex
    {
        internal int idx;
    }

    /// <summary>
    /// AnimationJobCache can be used in animation jobs to store values that
    /// can be updated through the AnimationJobCache during the Update loop without
    /// rebuilding the job.
    /// </summary>
    public struct AnimationJobCache : System.IDisposable
    {
        NativeArray<float> m_Data;

        internal AnimationJobCache(float[] data)
        {
            m_Data = new NativeArray<float>(data, Allocator.Persistent);
        }

        /// <summary>
        /// Dispose of the AnimationJobCache memory.
        /// </summary>
        public void Dispose()
        {
            m_Data.Dispose();
        }

        /// <summary>
        /// Gets raw float data at specified index.
        /// </summary>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        /// <returns>The raw float data.</returns>
        public float GetRaw(CacheIndex index, int offset = 0)
        {
            return m_Data[index.idx + offset];
        }

        /// <summary>
        /// Sets raw float data at specified index.
        /// </summary>
        /// <param name="val">Raw float data.</param>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        public void SetRaw(float val, CacheIndex index, int offset = 0)
        {
            m_Data[index.idx + offset] = val;
        }

        /// <summary>
        /// Gets value at specified index.
        /// </summary>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        /// <typeparam name="T">The value type.</typeparam>
        /// <returns></returns>
        unsafe public T Get<T>(CacheIndex index, int offset = 0) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            int stride = size / UnsafeUtility.SizeOf<float>();

            T val = default(T);
            UnsafeUtility.MemCpy(&val, (float*)m_Data.GetUnsafeReadOnlyPtr() + index.idx + offset * stride, size);
            return val;
        }

        /// <summary>
        /// Sets value at specified index.
        /// </summary>
        /// <param name="val">Value.</param>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        /// <typeparam name="T"></typeparam>
        unsafe public void Set<T>(T val, CacheIndex index, int offset = 0) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            int stride = size / UnsafeUtility.SizeOf<float>();

            UnsafeUtility.MemCpy((float*)m_Data.GetUnsafePtr() + index.idx + offset * stride, &val, size);
        }

        /// <summary>
        /// Sets an array of values at specified index.
        /// </summary>
        /// <param name="v">Array of values.</param>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        /// <typeparam name="T"></typeparam>
        unsafe public void SetArray<T>(T[] v, CacheIndex index, int offset = 0) where T : unmanaged
        {
            int size = UnsafeUtility.SizeOf<T>();
            int stride = size / UnsafeUtility.SizeOf<float>();

            fixed (void* ptr = v)
            {
                UnsafeUtility.MemCpy((float*)m_Data.GetUnsafePtr() + index.idx + offset * stride, ptr, size * v.Length);
            }
        }
    }

    /// <summary>
    /// AnimationJobCacheBuilder can be used to create a new AnimationJobCache object.
    /// </summary>
    public class AnimationJobCacheBuilder
    {
        List<float> m_Data;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AnimationJobCacheBuilder()
        {
            m_Data = new List<float>();
        }

        /// <summary>
        /// Adds a float value to the AnimationJobCache.
        /// </summary>
        /// <param name="v">Float value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(float v)
        {
            m_Data.Add(v);
            return new CacheIndex { idx = m_Data.Count - 1 };
        }

        /// <summary>
        /// Adds a Vector2 value to the AnimationJobCache.
        /// </summary>
        /// <param name="v">Vector2 value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(Vector2 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            return new CacheIndex { idx = m_Data.Count - 2 };
        }

        /// <summary>
        /// Adds a Vector3 value to the AnimationJobCache.
        /// </summary>
        /// <param name="v">Vector3 value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(Vector3 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            m_Data.Add(v.z);
            return new CacheIndex { idx = m_Data.Count - 3 };
        }

        /// <summary>
        /// Adds a Vector4 value to the AnimationJobCache.
        /// </summary>
        /// <param name="v">Vector4 value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(Vector4 v)
        {
            m_Data.Add(v.x);
            m_Data.Add(v.y);
            m_Data.Add(v.z);
            m_Data.Add(v.w);
            return new CacheIndex { idx = m_Data.Count - 4 };
        }

        /// <summary>
        /// Adds a Quaternion value to the AnimationJobCache.
        /// </summary>
        /// <param name="v">Quaternion value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(Quaternion v)
        {
            return Add(new Vector4(v.x, v.y, v.z, v.w));
        }

        /// <summary>
        /// Adds a AffineTransform value to the AnimationJobCache.
        /// </summary>
        /// <param name="tx">AffineTransform value.</param>
        /// <returns>CacheIndex that refers to the new value.</returns>
        public CacheIndex Add(AffineTransform tx)
        {
            Add(tx.translation);
            Add(tx.rotation);
            return new CacheIndex { idx = m_Data.Count - 7 };
        }

        /// <summary>
        /// Allocates uninitialized chunk of specified size in the AnimationJobCacheBuilder.
        /// </summary>
        /// <param name="size">Size of chunk to allocate.</param>
        /// <returns>CacheIndex that refers to the allocated chunk.</returns>
        public CacheIndex AllocateChunk(int size)
        {
            m_Data.AddRange(new float[size]);
            return new CacheIndex { idx = m_Data.Count - size };
        }

        /// <summary>
        /// Sets value in AnimationJobCacheBuilder at specified index.
        /// </summary>
        /// <param name="index">CacheIndex value.</param>
        /// <param name="offset">Offset to the CacheIndex.</param>
        /// <param name="value">float data.</param>
        public void SetValue(CacheIndex index, int offset, float value)
        {
            if (index.idx + offset < m_Data.Count)
                m_Data[index.idx + offset] = value;
        }

        /// <summary>
        /// Creates a new AnimationJobCache.
        /// </summary>
        /// <returns>AnimationJobCache object with newly set values.</returns>
        public AnimationJobCache Build() => new AnimationJobCache(m_Data.ToArray());
    }
}
