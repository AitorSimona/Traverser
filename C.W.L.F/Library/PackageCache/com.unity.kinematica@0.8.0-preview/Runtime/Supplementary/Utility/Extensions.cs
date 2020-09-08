using System.IO;

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    /// <summary>
    /// Static class that contains various extension methods.
    /// </summary>
    unsafe public static class Extensions
    {
        /// <summary>
        /// Retrieves a reference to an element of a native array.
        /// </summary>
        /// <param name="index">Index of the native array element.</param>
        /// <returns>Reference to the element at the index passed as argument.</returns>
        public static ref T At<T>(this NativeArray<T> nativeArray, int index) where T : struct
        {
            return ref UnsafeUtilityEx.ArrayElementAsRef<T>(
                NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray), index);
        }

        /// <summary>
        /// Retrieves a reference to an element of a native list.
        /// </summary>
        /// <param name="index">Index of the native list element.</param>
        /// <returns>Reference to the element at the index passed as argument.</returns>
        public static ref T At<T>(this NativeList<T> nativeList, int index) where T : struct
        {
            return ref UnsafeUtilityEx.ArrayElementAsRef<T>(
                NativeListUnsafeUtility.GetUnsafePtr(nativeList), index);
        }

        /// <summary>
        /// Retrieves a reference to an element of a native slice.
        /// </summary>
        /// <param name="index">Index of the native slice element.</param>
        /// <returns>Reference to the element at the index passed as argument.</returns>
        public static ref T At<T>(this NativeSlice<T> nativeSlice, int index) where T : struct
        {
            return ref UnsafeUtilityEx.ArrayElementAsRef<T>(
                NativeSliceUnsafeUtility.GetUnsafePtr(nativeSlice), index);
        }

        /// <summary>
        /// Writes a native array to the binary writer.
        /// </summary>
        internal static unsafe void WriteToStream<T>(this NativeArray<T> nativeArray, BinaryWriter writer) where T : struct
        {
            var numBytes = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            var buffer = new byte[numBytes];
            fixed(byte* dst = &buffer[0])
            {
                UnsafeUtility.MemCpy(dst,
                    NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray),
                    buffer.Length);
            }
            writer.Write(buffer);
        }

        /// <summary>
        /// Reads a native array from the binary reader.
        /// </summary>
        internal static unsafe void ReadFromStream<T>(this NativeArray<T> nativeArray, BinaryReader reader) where T : struct
        {
            var numBytes = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            var buffer = reader.ReadBytes(numBytes);
            fixed(byte* src = &buffer[0])
            {
                UnsafeUtility.MemCpy(
                    NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray),
                    src, buffer.Length);
            }
        }

        /// <summary>
        /// Writes a native list to the binary writer.
        /// </summary>
        public static unsafe void WriteToStream<T>(this NativeList<T> nativeList, BinaryWriter writer) where T : struct
        {
            int numElements = nativeList.Length;
            writer.Write(numElements);
            if (numElements > 0)
            {
                var numBytes = numElements * UnsafeUtility.SizeOf<T>();
                var buffer = new byte[numBytes];
                fixed(byte* dst = &buffer[0])
                {
                    UnsafeUtility.MemCpy(dst,
                        NativeListUnsafeUtility.GetUnsafePtr(nativeList),
                        buffer.Length);
                }
                writer.Write(buffer);
            }
        }

        /// <summary>
        /// Reads a native list from the binary reader.
        /// </summary>
        public static unsafe void ReadFromStream<T>(this NativeList<T> nativeList, BinaryReader reader) where T : struct
        {
            int numElements = reader.ReadInt32();
            nativeList.ResizeUninitialized(numElements);
            if (numElements > 0)
            {
                var numBytes = nativeList.Length * UnsafeUtility.SizeOf<T>();
                var buffer = reader.ReadBytes(numBytes);
                fixed(byte* src = &buffer[0])
                {
                    UnsafeUtility.MemCpy(
                        NativeListUnsafeUtility.GetUnsafePtr(nativeList),
                        src, buffer.Length);
                }
            }
        }

        /// <summary>
        /// Writes a vector3 to the binary writer.
        /// </summary>
        /// <param name="value">The vector3 that should be written.</param>
        public static void Write(this BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        /// <summary>
        /// Writes a quaternion to the binary writer.
        /// </summary>
        /// <param name="value">The quaternion that should be written.</param>
        public static void Write(this BinaryWriter writer, Quaternion value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        /// <summary>
        /// Writes a transform to the binary writer.
        /// </summary>
        /// <param name="transform">The transform that should be written.</param>
        public static void Write(this BinaryWriter writer, AffineTransform transform)
        {
            writer.Write(transform.t);
            writer.Write(transform.q);
        }

        /// <summary>
        /// Writes a transform to the binary writer.
        /// </summary>
        /// <param name="transform">The transform that should be written.</param>
        public static void WriteTransform(this BinaryWriter writer, Transform transform)
        {
            if (transform != null)
            {
                string path = transform.name;
                while (transform.parent != null)
                {
                    transform = transform.parent;
                    path = transform.name + "/" + path;
                }

                writer.Write(path);
            }
            else
            {
                writer.Write(string.Empty);
            }
        }

        internal static int GetComponentIndex<T>(T component) where T : Component
        {
            if (component != null)
            {
                T[] components = component.gameObject.GetComponents<T>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] == component)
                        return i;
                }
            }
            return -1;
        }

        internal static T GetComponentByIndex<T>(Transform transform, int index) where T : Component
        {
            return transform.gameObject.GetComponents<T>()[index];
        }

        /// <summary>
        /// Writes a component path to the binary writer.
        /// </summary>
        /// <param name="component">The component that should be written.</param>
        public static void WriteComponent<T>(this BinaryWriter writer, T component) where T : Component
        {
            int index = GetComponentIndex(component);
            writer.Write(index);
            if (index >= 0)
            {
                writer.WriteTransform(component.gameObject.transform);
            }
        }

        /// <summary>
        /// Reads a vector3 from the binary reader.
        /// </summary>
        /// <returns>The vector that has been read.</returns>
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a quaternion from the binary reader.
        /// </summary>
        /// <returns>The quaternion that has been read.</returns>
        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float w = reader.ReadSingle();

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a transform from the binary reader.
        /// </summary>
        /// <returns>The transform that has been read.</returns>
        public static AffineTransform ReadAffineTransform(this BinaryReader reader)
        {
            float3 t = reader.ReadVector3();
            quaternion q = reader.ReadQuaternion();

            return new AffineTransform(t, q);
        }

        /// <summary>
        /// Reads a transform from the binary reader.
        /// </summary>
        /// <returns>The transform that has been read.</returns>
        public static Transform ReadTransform(this BinaryReader reader)
        {
            string path = reader.ReadString();
            if (!string.IsNullOrEmpty(path))
            {
                GameObject gameObject = GameObject.Find(path);
                Assert.IsTrue(gameObject != null);
                return gameObject.transform;
            }
            return null;
        }

        /// <summary>
        /// Reads a component from the binary reader.
        /// </summary>
        /// <returns>The component that has been read.</returns>
        public static T ReadComponent<T>(this BinaryReader reader) where T : Component
        {
            int index = reader.ReadInt32();
            if (index >= 0)
            {
                Transform transform = ReadTransform(reader);
                if (transform != null)
                {
                    return GetComponentByIndex<T>(transform, index);
                }
            }
            return null;
        }
    }
}
