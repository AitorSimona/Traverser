using System.IO;

using UnityEngine.Assertions;

using Unity.Collections.LowLevel.Unsafe;
using Unity.SnapshotDebugger;
using Unity.Collections;

namespace Unity.Kinematica
{
    /// <summary>
    /// A memory array is very similar in nature to a NativeArray.
    /// It stores a reference to an array of data elements and allows
    /// for retrieval of inidividual elements. This is a workaround
    /// for the inability to nest NativeArrays.
    /// </summary>
    internal unsafe struct MemoryArray<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* ptr;
        internal int length;

        public MemoryArray(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                ptr = null;
                length = 0;
                return;
            }

            ptr = UnsafeUtility.AddressOf<T>(ref array[0]);
            length = array.Length;
        }

        public MemoryArray(NativeArray<T> array, int startIndex = 0, int subLength = -1)
        {
            System.Int64 address = (System.Int64)array.GetUnsafePtr();
            address += UnsafeUtility.SizeOf<T>() * startIndex;
            ptr = (void*)address;
            length = subLength >= 0 ? subLength : array.Length;
        }

        public MemoryArray(MemoryArray<T> array, int startIndex = 0, int subLength = -1)
        {
            System.Int64 address = (System.Int64)array.ptr;
            address += UnsafeUtility.SizeOf<T>() * startIndex;
            ptr = (void*)address;
            length = subLength >= 0 ? subLength : array.Length;
        }

        public MemoryArray(NativeSlice<T> array)
        {
            ptr = array.GetUnsafePtr();
            length = array.Length;
        }

        public MemoryArray<U> Reinterpret<U>() where U : struct
        {
            int sizeT = UnsafeUtility.SizeOf<T>();
            int sizeU = UnsafeUtility.SizeOf<U>();
            int bufferSize = sizeT * Length;
            Assert.IsTrue(sizeU <= bufferSize && (bufferSize % sizeU == 0));

            return new MemoryArray<U>()
            {
                ptr = ptr,
                length = bufferSize / sizeU
            };
        }

        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            NativeArray<T> array = new NativeArray<T>(Length, allocator);

            for (int i = 0; i < Length; ++i)
            {
                array[i] = this[i];
            }

            return array;
        }

        /// <summary>
        /// Determines the amount of elements stored in the memory array.
        /// </summary>
        public int Length => length;

        /// <summary>
        /// Allows to retrieve an individual array element.
        /// The element is returned a ref value.
        /// </summary>
        public ref T this[int index]
        {
            get
            {
                return ref UnsafeUtilityEx.ArrayElementAsRef<T>(ptr, index);
            }
        }

        /// <summary>
        /// Determines if the given memory array is valid or not.
        /// </summary>
        /// <returns>True if the memory array is valid; false otherwise.</returns>
        public bool IsValid
        {
            get { return ptr != null; }
        }

        /// <summary>
        /// Invalid memory array.
        /// </summary>
        public static MemoryArray<T> Null => new MemoryArray<T>();

        internal void WriteToStream(BinaryWriter writer)
        {
            var numBytes = Length * UnsafeUtility.SizeOf<T>();
            var buffer = new byte[numBytes];
            fixed(byte* dst = &buffer[0])
            {
                UnsafeUtility.MemCpy(
                    dst, ptr, buffer.Length);
            }
            writer.Write(buffer);
        }

        internal void ReadFromStream(BinaryReader reader)
        {
            var numBytes = Length * UnsafeUtility.SizeOf<T>();
            var buffer = reader.ReadBytes(numBytes);
            fixed(byte* src = &buffer[0])
            {
                UnsafeUtility.MemCpy(
                    ptr, src, buffer.Length);
            }
        }

        /// <summary>
        /// Reads a memory array from the buffer passed as argument.
        /// </summary>
        /// <param name="buffer">The buffer that the memory array should be read from.</param>
        public void ReadFromStream(Buffer buffer)
        {
            var numBytes = Length * UnsafeUtility.SizeOf<T>();
            var byteArray = buffer.ReadBytes(numBytes);
            fixed(byte* src = &byteArray[0])
            {
                UnsafeUtility.MemCpy(
                    ptr, src, byteArray.Length);
            }
        }

        /// <summary>
        /// Stores a memory array in the buffer passed as argument.
        /// </summary>
        /// <param name="buffer">The buffer that the memory array should be written to.</param>
        public void WriteToStream(Buffer buffer)
        {
            var numBytes = Length * UnsafeUtility.SizeOf<T>();
            var byteArray = new byte[numBytes];
            fixed(byte* dst = &byteArray[0])
            {
                UnsafeUtility.MemCpy(
                    dst, ptr, byteArray.Length);
            }
            buffer.Write(byteArray);
        }

        /// <summary>
        /// Copies the memory array to the target memory array passed as argument.
        /// </summary>
        /// <param name="target">The target memory array that this memory array should be copied to.</param>
        public void CopyTo(ref MemoryArray<T> target)
        {
            Assert.IsTrue(target.Length == length);

            UnsafeUtility.MemCpy(
                target.ptr, ptr, Length * UnsafeUtility.SizeOf<T>());
        }

        public static implicit operator NativeSlice<T>(MemoryArray<T> array)
        {
            int elemSize;

            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            NativeSlice<T> slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(array.ptr, elemSize, array.length);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif

            return slice;
        }
    }
}
