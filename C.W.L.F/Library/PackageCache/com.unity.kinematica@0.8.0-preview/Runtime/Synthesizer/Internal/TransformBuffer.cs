using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Unity.SnapshotDebugger;
using Buffer = Unity.SnapshotDebugger.Buffer;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace Unity.Kinematica
{
    internal struct TransformBuffer
    {
        public struct Memory : IDisposable
        {
            ArrayMemory memory;
            TransformBuffer buffer;

            public ref TransformBuffer Ref => ref (new MemoryRef<TransformBuffer>(ref buffer)).Ref;

            Memory(int numTransforms, Allocator allocator)
            {
                int transformSize = 0;

                unsafe
                {
                    transformSize = UnsafeUtility.SizeOf<AffineTransform>();
                }

                memory = ArrayMemory.Create();
                memory.Reserve<AffineTransform>(numTransforms);
                memory.Allocate(allocator);

                buffer = Create(ref memory, numTransforms);
            }

            public static Memory Allocate(int numTransforms, Allocator allocator)
            {
                return new Memory(numTransforms, allocator);
            }

            public void Dispose()
            {
                memory.Dispose();
            }
        }

        public static TransformBuffer Create(ref ArrayMemory memory, int numTransforms)
        {
            return new TransformBuffer()
            {
                transforms = memory.CreateSlice<AffineTransform>(numTransforms)
            };
        }

        internal void CopyFrom(ref TransformBuffer transformBuffer)
        {
            transforms.CopyFrom(transformBuffer.transforms);
        }

        public int Length => transforms.Length;

        public AffineTransform this[int index]
        {
            get { return transforms[index]; }
            set { transforms[index] = value; }
        }

        public void WriteToStream(Buffer buffer)
        {
            transforms.WriteToStream(buffer);
        }

        public void ReadFromStream(Buffer buffer)
        {
            transforms.ReadFromStream(buffer);
        }

        public NativeSlice<AffineTransform> transforms;
    }
}
