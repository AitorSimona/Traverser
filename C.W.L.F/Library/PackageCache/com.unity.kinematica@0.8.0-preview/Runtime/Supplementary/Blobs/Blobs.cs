using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    unsafe internal struct BlobAllocator : IDisposable
    {
        byte* m_RootPtr;
        byte* m_Ptr;

        long m_Size;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;
        DisposeSentinel m_DisposeSentinel;
#endif

        //@TODO: handle alignment correctly in the allocator
        public BlobAllocator(int sizeHint)
        {
            //@TODO: Use virtual alloc to make it unnecessary to know the size ahead of time...
            int size = 1024 * 1024 * 256;
            m_RootPtr = m_Ptr = (byte*)UnsafeUtility.Malloc(size, 16, Allocator.Persistent);
            m_Size = size;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, Allocator.Persistent);
#endif
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
            UnsafeUtility.Free(m_RootPtr, Allocator.Persistent);
#endif
        }

        public ref T ConstructRoot<T>() where T : struct
        {
            byte* returnPtr = m_Ptr;
            m_Ptr += UnsafeUtility.SizeOf<T>();
            return ref UnsafeUtilityEx.AsRef<T>(returnPtr);
        }

        int Allocate(long size, void* ptrAddr)
        {
            long offset = (byte*)ptrAddr - m_RootPtr;
            if (m_Ptr - m_RootPtr > m_Size)
                throw new ArgumentException("BlobAllocator.preallocated size not large enough");

            if (offset < 0 || offset + size > m_Size)
                throw new ArgumentException("Ptr must be part of root compound");

            byte* returnPtr = m_Ptr;
            m_Ptr += size;

            long relativeOffset = returnPtr - (byte*)ptrAddr;
            if (relativeOffset > int.MaxValue || relativeOffset < int.MinValue)
                throw new ArgumentException("BlobPtr uses 32 bit offsets, and this offset exceeds it.");

            return (int)relativeOffset;
        }

        public void Allocate<T>(int length, ref BlobArray<T> ptr) where T : struct
        {
            ptr.m_OffsetPtr = Allocate(UnsafeUtility.SizeOf<T>() * length, UnsafeUtility.AddressOf(ref ptr));
            ptr.m_Length = length;
        }

        public void Allocate<T>(ref BlobPtr<T> ptr) where T : struct
        {
            ptr.m_OffsetPtr = Allocate(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AddressOf(ref ptr));
        }

        public BlobAssetReference<T> CreateBlobAssetReference<T>(Allocator allocator) where T : struct
        {
            Assert.AreEqual(16, sizeof(BlobAssetHeader));

            long dataSize = (m_Ptr - m_RootPtr);
            byte* buffer = (byte*)UnsafeUtility.Malloc(sizeof(BlobAssetHeader) + dataSize, 16, allocator);
            UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader), m_RootPtr, dataSize);

            BlobAssetHeader* header = (BlobAssetHeader*)buffer;
            *header = new BlobAssetHeader();
            header->Refcount = 1;
            header->Allocator = allocator;

            BlobAssetReference<T> assetReference;
            assetReference.m_Ptr = buffer + sizeof(BlobAssetHeader);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            assetReference.m_Safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(assetReference.m_Safety, false);
            AtomicSafetyHandle.UseSecondaryVersion(ref assetReference.m_Safety);
#endif

            return assetReference;
        }

        public long DataSize
        {
            get
            {
                return (m_Ptr - m_RootPtr);
            }
        }
    }

    struct BlobAssetHeader
    {
        uint _padding0;
        uint _padding1;

        public int Refcount;
        public Allocator Allocator;
    }

    [NativeContainer]
    internal unsafe struct BlobAssetReference<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte* m_Ptr;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public bool Exists
        {
            get { return m_Ptr != null; }
        }

        public void* GetUnsafePtr()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            return m_Ptr;
        }

        public void Retain()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            var header = (BlobAssetHeader*)m_Ptr;
            header -= 1;
            Interlocked.Increment(ref header->Refcount);
        }

        public void SafeRelease()
        {
            if (m_Ptr != null)
                Release();
        }

        public void Release()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif

            var header = (BlobAssetHeader*)m_Ptr;
            header -= 1;

            if (Interlocked.Decrement(ref header->Refcount) == 0)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.Release(m_Safety);
#endif

                UnsafeUtility.Free(header, header->Allocator);
                m_Ptr = null;
            }
        }

        public ref T Value
        {
            get
            {
                return ref UnsafeUtilityEx.AsRef<T>(m_Ptr);
            }
        }

        unsafe public static BlobAssetReference<T> Create(byte[] data)
        {
            byte* buffer = (byte*)UnsafeUtility.Malloc(sizeof(BlobAssetHeader) + data.Length, 16, Allocator.Persistent);
            fixed(byte* ptr = &data[0])
            {
                UnsafeUtility.MemCpy(buffer + sizeof(BlobAssetHeader), ptr, data.Length);
            }

            BlobAssetHeader* header = (BlobAssetHeader*)buffer;
            *header = new BlobAssetHeader();
            header->Refcount = 1;
            header->Allocator = Allocator.Persistent;

            BlobAssetReference<T> assetReference;
            assetReference.m_Ptr = buffer + sizeof(BlobAssetHeader);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            assetReference.m_Safety = AtomicSafetyHandle.Create();
            AtomicSafetyHandle.SetAllowSecondaryVersionWriting(assetReference.m_Safety, false);
            AtomicSafetyHandle.UseSecondaryVersion(ref assetReference.m_Safety);
#endif

            return assetReference;
        }
    }

    unsafe internal struct BlobPtr<T> where T : struct
    {
        internal int m_OffsetPtr;

        public ref T Value
        {
            get
            {
                fixed(int* thisPtr = &m_OffsetPtr)
                {
                    return ref UnsafeUtilityEx.AsRef<T>((byte*)thisPtr + m_OffsetPtr);
                }
            }
        }

        public void* GetUnsafePtr()
        {
            fixed(int* thisPtr = &m_OffsetPtr)
            {
                return (byte*)thisPtr + m_OffsetPtr;
            }
        }
    }

    unsafe internal struct BlobArray<T> where T : struct
    {
        internal int m_OffsetPtr;
        internal int m_Length;

        public int Length { get { return m_Length; } }

        public void* GetUnsafePtr()
        {
            fixed(int* thisPtr = &m_OffsetPtr)
            {
                return (byte*)thisPtr + m_OffsetPtr;
            }
        }

        public ref T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= (uint)m_Length)
                    throw new IndexOutOfRangeException(string.Format("Index {0} is out of range Length {1}", index, m_Length));
#endif

                fixed(int* thisPtr = &m_OffsetPtr)
                {
                    return ref UnsafeUtilityEx.ArrayElementAsRef<T>((byte*)thisPtr + m_OffsetPtr, index);
                }
            }
        }
    }
}
