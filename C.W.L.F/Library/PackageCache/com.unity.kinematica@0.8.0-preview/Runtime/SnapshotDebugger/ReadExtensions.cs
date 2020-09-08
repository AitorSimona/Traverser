using System;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.SnapshotDebugger
{
    /// <summary>
    /// Static class that contains various extension methods
    /// that allow reading data types from a buffer.
    /// </summary>
    /// <seealso cref="Buffer"/>
    public static class ReadExtensions
    {
        static IEnumerable<MethodInfo> GetExtensionMethods(Type extendedType)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in SnapshotDebugger.ReflectionUtility.GetTypesFromAssembly(assembly))
                {
                    if (type.IsSealed && !type.IsGenericType && !type.IsNested)
                    {
                        foreach (var method in type.GetMethods(BindingFlags.Static |
                            BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (method.IsDefined(typeof(ExtensionAttribute), false))
                            {
                                if (IsReadExtension(method, extendedType))
                                {
                                    yield return method;
                                }
                            }
                        }
                    }
                }
            }
        }

        static bool IsReadExtension(MethodInfo method, Type extendedType)
        {
            if (method.GetParameters().Length > 1)
            {
                return false;
            }

            if (method.ReturnType == typeof(void))
            {
                return false;
            }

            if (method.ReturnType == typeof(object))
            {
                return false;
            }

            return method.GetParameters()[0].ParameterType == extendedType;
        }

        static T GetComponentByIndex<T>(Transform transform, int index) where T : Component
        {
            return transform.gameObject.GetComponents<T>()[index];
        }

        static MethodInfo[] extensionMethods;

        internal static void InitializeExtensionMethods()
        {
            var methods = new List<MethodInfo>();

            foreach (var method in GetExtensionMethods(typeof(Buffer)))
            {
                methods.Add(method);
            }

            extensionMethods = methods.ToArray();
        }

        internal static Type ReadType(this Buffer buffer)
        {
            return Type.GetType(buffer.ReadString());
        }

        internal static object TryReadObject(this Buffer buffer, Type type)
        {
            Assert.IsTrue(extensionMethods != null);

            foreach (var method in extensionMethods)
            {
                if (method.ReturnType == type)
                {
                    return method.Invoke(null, new object[] { buffer });
                }
            }

            throw new NotSupportedException(
                string.Format("Couldn't find a default deserializer for type '{0}'. Try implementing 'Serializable' to ensure the type can be deserialized correctly", type));
        }

        /// <summary>
        /// Reads a single byte (8-bit value) from the buffer.
        /// </summary>
        /// <returns>The byte that has been read from the buffer.</returns>
        public static byte ReadByte(this Buffer buffer)
        {
            return buffer.ReadByte();
        }

        /// <summary>
        /// Reads a series of bytes from the buffer.
        /// </summary>
        /// <param name="amount">Number of bytes to be read from the buffer.</param>
        /// <returns>The array of bytes that has been read from the buffer.</returns>
        public static byte[] ReadBytes(this Buffer buffer, int amount)
        {
            byte[] bytes = new byte[amount];

            for (int i = 0; i < amount; i++)
            {
                bytes[i] = buffer.ReadByte();
            }

            return bytes;
        }

        /// <summary>
        /// Reads a series of bytes from the buffer.
        /// </summary>
        /// <param name="rhs">Destination array that the bytes should be written to.</param>
        /// <param name="offset">Offset into the destination array where the bytes should be written to.</param>
        /// <param name="amount">Number of bytes to be read from the buffer.</param>
        public static void ReadBytes(this Buffer buffer, byte[] rhs, int offset, int amount)
        {
            for (int i = offset; i < amount; i++)
            {
                rhs[i] = buffer.ReadByte();
            }
        }

        /// <summary>
        /// Reads a short (16-bit value) from the buffer.
        /// </summary>
        /// <returns>The short that has been read from the buffer.</returns>
        public static short Read16(this Buffer buffer)
        {
            return buffer.ReadBlittable<short>();
        }

        /// <summary>
        /// Reads an integer (32-bit value) from the buffer.
        /// </summary>
        /// <returns>The integer that has been read from the buffer.</returns>
        public static int Read32(this Buffer buffer)
        {
            return buffer.ReadBlittable<int>();
        }

        /// <summary>
        /// Reads a 32-bit floating point value from the buffer.
        /// </summary>
        /// <returns>The floating point value that has been read from the buffer.</returns>
        public static float ReadSingle(this Buffer buffer)
        {
            return buffer.ReadBlittable<float>();
        }

        /// <summary>
        /// Reads a boolean from the buffer.
        /// </summary>
        /// <returns>The boolean value that has been read from the buffer.</returns>
        public static bool ReadBoolean(this Buffer buffer)
        {
            return buffer.ReadBlittable<bool>();
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        /// <returns>The string that has been read from the buffer.</returns>
        public static string ReadString(this Buffer buffer)
        {
            short size = buffer.Read16();

            byte[] bytes = buffer.ReadBytes(size);

#if UNITY_WINRT && !UNITY_EDITOR
            return Encoding.UTF8.GetString(bytes);
#else
            return Encoding.Default.GetString(bytes);
#endif
        }

        /// <summary>
        /// Reads a vector2 from the buffer.
        /// </summary>
        /// <returns>The vector that has been read from the buffer.</returns>
        public static Vector2 ReadVector2(this Buffer buffer)
        {
            float x = buffer.ReadSingle();
            float y = buffer.ReadSingle();

            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads a vector3 from the buffer.
        /// </summary>
        /// <returns>The vector that has been read from the buffer.</returns>
        public static Vector3 ReadVector3(this Buffer buffer)
        {
            float x = buffer.ReadSingle();
            float y = buffer.ReadSingle();
            float z = buffer.ReadSingle();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a vector4 from the buffer.
        /// </summary>
        /// <returns>The vector that has been read from the buffer.</returns>
        public static Vector4 ReadVector4(this Buffer buffer)
        {
            float x = buffer.ReadSingle();
            float y = buffer.ReadSingle();
            float z = buffer.ReadSingle();
            float w = buffer.ReadSingle();

            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Reads a quaternion from the buffer.
        /// </summary>
        /// <returns>The quaternion that has been read from the buffer.</returns>
        public static Quaternion ReadQuaternion(this Buffer buffer)
        {
            float x = buffer.ReadSingle();
            float y = buffer.ReadSingle();
            float z = buffer.ReadSingle();
            float w = buffer.ReadSingle();

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a color from the buffer.
        /// </summary>
        /// <remarks>
        /// The individual color components are expected to be stored as floating point values.
        /// </remarks>
        /// <returns>The color that has been read from the buffer.</returns>
        public static Color ReadColor(this Buffer buffer)
        {
            float r = buffer.ReadSingle();
            float g = buffer.ReadSingle();
            float b = buffer.ReadSingle();
            float a = buffer.ReadSingle();

            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Reads a color from the buffer.
        /// </summary>
        /// <remarks>
        /// The individual color components are expected to be stored as byte (8-bit) values.
        /// </remarks>
        /// <returns>The color that has been read from the buffer.</returns>
        public static Color32 ReadColor32(this Buffer buffer)
        {
            byte r = buffer.ReadByte();
            byte g = buffer.ReadByte();
            byte b = buffer.ReadByte();
            byte a = buffer.ReadByte();

            return new Color32(r, g, b, a);
        }

        public static NativeString64 ReadNativeString64(this Buffer buffer)
        {
            NativeString64 str = new NativeString64();
            str.LengthInBytes = buffer.ReadBlittable<ushort>();
            str.buffer = buffer.ReadBlittable<Bytes62>();

            return str;
        }

        /// <summary>
        /// Reads a quantized floating point value from the buffer.
        /// </summary>
        /// <returns>The original floating point value.</returns>
        public static float ReadSingleQuantized(this Buffer buffer)
        {
            return buffer.Read16() / 256f;
        }

        /// <summary>
        /// Reads a quantized vector2 from the buffer.
        /// </summary>
        /// <returns>The original vector2 value.</returns>
        public static Vector2 ReadVector2Quantized(this Buffer buffer)
        {
            float x = buffer.ReadSingleQuantized();
            float y = buffer.ReadSingleQuantized();

            return new Vector2(x, y);
        }

        /// <summary>
        /// Reads a quantized vector3 from the buffer.
        /// </summary>
        /// <returns>The original vector3 value.</returns>
        public static Vector3 ReadVector3Quantized(this Buffer buffer)
        {
            float x = buffer.ReadSingleQuantized();
            float y = buffer.ReadSingleQuantized();
            float z = buffer.ReadSingleQuantized();

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Reads a quantized vector4 from the buffer.
        /// </summary>
        /// <returns>The original vector4 value.</returns>
        public static Vector4 ReadVector4Quantized(this Buffer buffer)
        {
            float x = buffer.ReadSingleQuantized();
            float y = buffer.ReadSingleQuantized();
            float z = buffer.ReadSingleQuantized();
            float w = buffer.ReadSingleQuantized();

            return new Vector4(x, y, z, w);
        }

        /// <summary>
        /// Reads a quantized quaternion from the buffer.
        /// </summary>
        /// <returns>The original quaternion value.</returns>
        public static Quaternion ReadQuaternionQuantized(this Buffer buffer)
        {
            float x = buffer.ReadSingleQuantized();
            float y = buffer.ReadSingleQuantized();
            float z = buffer.ReadSingleQuantized();
            float w = buffer.ReadSingleQuantized();

            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        /// Reads a transform reference from the buffer.
        /// </summary>
        /// <remarks>
        /// Transform references are stored as absolute paths to the transform.
        /// If the serialized Transform has been reparented or renamed, this
        /// method will have undefined results.
        /// </remarks>
        /// <returns>A reference to the transform that has been read from the buffer.</returns>
        public static Transform ReadTransform(this Buffer buffer)
        {
            string path = buffer.ReadString();
            if (!string.IsNullOrEmpty(path))
            {
                GameObject gameObject = GameObject.Find(path);
                if (gameObject != null)
                {
                    return gameObject.transform;
                }
            }
            return null;
        }

        /// <summary>
        /// Reads a component reference from the buffer.
        /// </summary>
        /// <remarks>
        /// Component references are stored as an index relative to a GameObject.
        /// If components are added to or removed from the GameObject, this method
        /// will have undefined results.
        /// </remarks>
        /// <returns>The component reference that has been read from the buffer.</returns>
        public static T ReadComponent<T>(this Buffer buffer) where T : Component
        {
            int index = buffer.Read32();
            if (index >= 0)
            {
                Transform transform = ReadTransform(buffer);
                if (transform != null)
                {
                    return GetComponentByIndex<T>(transform, index);
                }
            }
            return null;
        }

        public static NativeArray<T> ReadNativeArray<T>(this Buffer buffer, out Allocator allocator) where T : struct
        {
            allocator = (Allocator)buffer.Read32();
            if (allocator == Allocator.Invalid)
            {
                return default(NativeArray<T>);
            }

            int length = buffer.Read32();

            NativeArray<T> array = new NativeArray<T>(length, allocator);

            int elemSize;
            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            NativeSlice<T> slice = buffer.ReadSlice(array.Length * elemSize).SliceConvert<T>();
            slice.CopyTo(array);

            return array;
        }

        public static NativeList<T> ReadNativeList<T>(this Buffer buffer, out Allocator allocator) where T : struct
        {
            allocator = (Allocator)buffer.Read32();
            if (allocator == Allocator.Invalid)
            {
                return default(NativeList<T>);
            }

            int length = buffer.Read32();

            NativeList<T> list = new NativeList<T>(length, allocator);
            list.ResizeUninitialized(length);

            int elemSize;
            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            NativeSlice<T> slice = buffer.ReadSlice(list.Length * elemSize).SliceConvert<T>();
            slice.CopyTo(list.AsArray());

            return list;
        }

        /// <summary>
        /// Reads a native list from the buffer.
        /// </summary>
        public static unsafe void ReadFromStream<T>(this NativeList<T> nativeList, Buffer buffer) where T : struct
        {
            int numElements = buffer.Read32();
            nativeList.ResizeUninitialized(numElements);
            if (numElements > 0)
            {
                var numBytes = numElements * UnsafeUtility.SizeOf<T>();
                var byteArray = buffer.ReadBytes(numBytes);
                fixed(byte* src = &byteArray[0])
                {
                    UnsafeUtility.MemCpy(
                        NativeListUnsafeUtility.GetUnsafePtr(nativeList),
                        src, byteArray.Length);
                }
            }
        }

        /// <summary>
        /// Reads a native array from the buffer.
        /// </summary>
        public static unsafe void ReadFromStream<T>(this NativeArray<T> nativeArray, Buffer buffer) where T : struct
        {
            var numBytes = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            var byteArray = buffer.ReadBytes(numBytes);
            fixed(byte* src = &byteArray[0])
            {
                UnsafeUtility.MemCpy(
                    NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray),
                    src, byteArray.Length);
            }
        }

        public static unsafe void ReadFromStream<T>(this NativeSlice<T> nativeSlice, Buffer buffer) where T : struct
        {
            for (int i = 0; i < nativeSlice.Length; ++i)
            {
                nativeSlice[i] = buffer.ReadBlittable<T>();
            }
        }
    }
}
