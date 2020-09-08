using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;
using System.Reflection;

namespace Unity.SnapshotDebugger
{
    public struct Buffer : System.IDisposable
    {
        private NativeList<byte> bytes;

        public int Length => bytes.Length;

        public bool CanRead
        {
            get { return Size > 0; }
        }

        public bool EndRead
        {
            get { return bytes.Length >= Size; }
        }

        public int Size
        {
            get { return bytes.Capacity; }
        }

        public int SetCursor(int cursor)
        {
            Assert.IsTrue(cursor <= bytes.Capacity);

            int previousCursor = bytes.Length;
            bytes.Resize(cursor, NativeArrayOptions.UninitializedMemory);

            return previousCursor;
        }

        public static Buffer Create(Allocator allocator)
        {
            return new Buffer()
            {
                bytes = new NativeList<byte>(allocator)
            };
        }

        public static Buffer Create(int capacity, Allocator allocator)
        {
            return new Buffer()
            {
                bytes = new NativeList<byte>(capacity, allocator)
            };
        }

        public static Buffer Create(byte[] data, Allocator allocator)
        {
            var buffer = Create(allocator);

            NativeArray<byte> elements = new NativeArray<byte>(data, Allocator.Temp);
            buffer.bytes.AddRange(elements);
            elements.Dispose();

            return buffer;
        }

        public void Dispose()
        {
            bytes.Dispose();
        }

        public void PrepareForRead()
        {
            SetCursor(0);
        }

        public void Clear()
        {
            bytes.Resize(0, NativeArrayOptions.ClearMemory);
        }

        public void Append(byte value)
        {
            bytes.Add(value);
        }

        public void Append(NativeArray<byte> elements)
        {
            bytes.AddRange(elements);
        }

        public void Copy(Buffer from)
        {
            bytes.AddRange(from.bytes.AsArray());
        }

        public byte ReadByte()
        {
            if (EndRead)
            {
                throw new InvalidOperationException("End of buffer reached");
            }

            bytes.Resize(bytes.Length + 1, NativeArrayOptions.UninitializedMemory);
            return bytes[bytes.Length - 1];
        }

        public NativeSlice<byte> ReadSlice(int size)
        {
            if (bytes.Length + size > Size)
            {
                throw new InvalidOperationException("End of buffer reached");
            }

            int startOffset = bytes.Length;
            bytes.Resize(startOffset + size, NativeArrayOptions.UninitializedMemory);

            return new NativeSlice<byte>(bytes.AsArray(), startOffset, size);
        }

        public T ReadBlittable<T>() where T : struct
        {
            T val;

            unsafe
            {
                int size = UnsafeUtility.SizeOf<T>();
                NativeSlice<byte> bytes = ReadSlice(size);

                val = UnsafeUtilityEx.AsRef<T>((void*)((byte*)bytes.GetUnsafePtr()));
            }

            return val;
        }

        public object ReadBlittableGeneric(Type type)
        {
            MethodInfo readBlittableMethod = GetType().GetMethod(nameof(ReadBlittable));
            MethodInfo readMethod = readBlittableMethod.MakeGenericMethod(new Type[] { type });
            return readMethod.Invoke(this, null);
        }

        public void WriteBlittable<T>(T val) where T : struct
        {
            unsafe
            {
                int size = UnsafeUtility.SizeOf<T>();
                byte* elems = (byte*)UnsafeUtility.AddressOf<T>(ref val);

                for (int i = 0; i < size; ++i)
                {
                    Append(elems[i]);
                }
            }
        }

        public void WriteBlittableGeneric(object val)
        {
            MethodInfo writeBlittableMethod = GetType().GetMethod(nameof(WriteBlittable));
            MethodInfo writeMethod = writeBlittableMethod.MakeGenericMethod(new Type[] { val.GetType() });
            writeMethod.Invoke(this, new object[] { val });
        }
    }
}
