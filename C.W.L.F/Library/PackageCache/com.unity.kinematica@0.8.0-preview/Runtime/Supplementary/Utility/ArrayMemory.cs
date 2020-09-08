using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    internal struct ArrayMemory : IDisposable
    {
        NativeArray<byte> bytes;

        int requiredSize;
        int usedSize;

        public static ArrayMemory Create()
        {
            return new ArrayMemory()
            {
                requiredSize = 0,
                usedSize = -1
            };
        }

        public void Dispose()
        {
            bytes.Dispose();
        }

        internal void Reserve<T>(int numElements) where T : struct
        {
            int elemSize = 0;

            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            requiredSize += numElements * elemSize;
        }

        internal void Allocate(Allocator allocator)
        {
            bytes = new NativeArray<byte>(requiredSize, allocator);
            usedSize = 0;
        }

        internal NativeSlice<T> CreateSlice<T>(int length) where T : struct
        {
            if (usedSize < 0)
            {
                throw new Exception("ArraMemory must be allocated before it can create native slices");
            }

            int elemSize = 0;

            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            int sliceSize = length * elemSize;
            int availableSize = bytes.Length - usedSize;

            if (sliceSize > availableSize)
            {
                throw new Exception($"Memory overflow : trying to create a slice of {sliceSize} bytes whereas there are only {availableSize} bytes available");
            }

            var slice = (new NativeSlice<byte>(bytes, usedSize, sliceSize)).SliceConvert<T>();
            usedSize += sliceSize;

            return slice;
        }
    }
}
