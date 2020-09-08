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
    /// that allow writing data types to a buffer.
    /// </summary>
    /// <seealso cref="Buffer"/>
    public static class WriteExtensions
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
                                if (IsWriteExtension(method, extendedType))
                                {
                                    yield return method;
                                }
                            }
                        }
                    }
                }
            }
        }

        static Type ReturnType(MethodInfo method)
        {
            return method.GetParameters()[1].ParameterType;
        }

        static bool IsWriteExtension(MethodInfo method, Type extendedType)
        {
            if (method.ReturnType != typeof(void))
            {
                return false;
            }

            if (method.GetParameters().Length != 2)
            {
                return false;
            }

            if (ReturnType(method) == typeof(object))
            {
                return false;
            }

            if (method.IsGenericMethod)
            {
                return false;
            }

            return method.GetParameters()[0].ParameterType == extendedType;
        }

        static int GetComponentIndex<T>(T component) where T : Component
        {
            if (component != null)
            {
                T[] components = component.gameObject.GetComponents<T>();
                for (int i = 0; i < components.Length; ++i)
                {
                    if (components[i] == component)
                    {
                        return i;
                    }
                }
            }
            return -1;
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

        internal static void WriteType(this Buffer buffer, Type type)
        {
            if (type.Assembly == typeof(Type).Assembly || type.Assembly == typeof(Buffer).Assembly)
            {
                buffer.Write(type.FullName);
            }
            else
            {
                buffer.Write(type.AssemblyQualifiedName);
            }
        }

        internal static void TryWriteObject(this Buffer buffer, object value)
        {
            Type type = value.GetType();

            buffer.WriteType(type);

            if (typeof(Serializable).IsAssignableFrom(type) == true)
            {
                (value as Serializable).WriteToStream(buffer);
            }
            else
            {
                Assert.IsTrue(extensionMethods != null);
                foreach (var method in  extensionMethods)
                {
                    if (ReturnType(method) == type)
                    {
                        method.Invoke(null, new object[] { buffer, value });
                        return;
                    }
                }

                throw new NotSupportedException(
                    string.Format("Couldn't find a default serializer for type '{0}'. Try implementing 'Serializable' to ensure the type can be serialized correctly", type));
            }
        }

        /// <summary>
        /// Writes a single byte (8-bit value) to the buffer.
        /// </summary>
        /// <param name="value">The byte that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, byte value)
        {
            buffer.Append(value);
        }

        /// <summary>
        /// Writes an array of bytes to the buffer.
        /// </summary>
        /// <param name="bytes">The array of bytes that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                buffer.Append(bytes[i]);
            }
        }

        /// <summary>
        /// Writes an array of bytes to the buffer.
        /// </summary>
        /// <param name="bytes">The array of bytes that should be written to the buffer.</param>
        /// <param name="offset">Read offset in bytes where the bytes should be read from.</param>
        /// <param name="length">The number of bytes to be read from the input buffer.</param>
        public static void Write(this Buffer buffer, byte[] bytes, int offset, int length)
        {
            for (int i = offset; i < length; i++)
            {
                buffer.Append(bytes[i]);
            }
        }

        /// <summary>
        /// Writes a short (16-bit value) to the buffer.
        /// </summary>
        /// <param name="value">The short that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, short value)
        {
            buffer.WriteBlittable(value);
        }

        /// <summary>
        /// Writes an integer (32-bit value) to the buffer.
        /// </summary>
        /// <param name="value">The integer that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, int value)
        {
            buffer.WriteBlittable(value);
        }

        /// <summary>
        /// Writes a floating point value to the buffer.
        /// </summary>
        /// <param name="value">The floating point value that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, float value)
        {
            buffer.WriteBlittable(value);
        }

        /// <summary>
        /// Writes a boolean to the buffer.
        /// </summary>
        /// <param name="value">The boolean that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, bool value)
        {
            buffer.WriteBlittable(value);
        }

        /// <summary>
        /// Writes a string to the buffer.
        /// </summary>
        /// <param name="value">The string that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, string value)
        {
#if UNITY_WINRT && !UNITY_EDITOR
            byte[] bytes = Encoding.UTF8.GetBytes(value);
#else
            byte[] bytes = Encoding.Default.GetBytes(value);
#endif

            buffer.Write((short)bytes.Length);
            buffer.Write(bytes);
        }

        /// <summary>
        /// Writes a buffer passed as argument to the buffer.
        /// </summary>
        /// <param name="value">The input buffer that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Buffer other)
        {
            buffer.Copy(other);
        }

        /// <summary>
        /// Writes a vector2 to the buffer.
        /// </summary>
        /// <param name="value">The vector2 that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Vector2 value)
        {
            buffer.Write(value.x);
            buffer.Write(value.y);
        }

        /// <summary>
        /// Writes a vector3 to the buffer.
        /// </summary>
        /// <param name="value">The vector3 that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Vector3 value)
        {
            buffer.Write(value.x);
            buffer.Write(value.y);
            buffer.Write(value.z);
        }

        /// <summary>
        /// Writes a vector4 to the buffer.
        /// </summary>
        /// <param name="value">The vector4 that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Vector4 value)
        {
            buffer.Write(value.x);
            buffer.Write(value.y);
            buffer.Write(value.z);
            buffer.Write(value.w);
        }

        /// <summary>
        /// Writes a quaternion to the buffer.
        /// </summary>
        /// <param name="value">The quaternion that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Quaternion value)
        {
            buffer.Write(value.x);
            buffer.Write(value.y);
            buffer.Write(value.z);
            buffer.Write(value.w);
        }

        /// <summary>
        /// Writes a color value to the buffer.
        /// </summary>
        /// <param name="value">The color value that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Color value)
        {
            buffer.Write(value.r);
            buffer.Write(value.g);
            buffer.Write(value.b);
            buffer.Write(value.a);
        }

        /// <summary>
        /// Writes a color32 value to the buffer.
        /// </summary>
        /// <param name="value">The color32 value that should be written to the buffer.</param>
        public static void Write(this Buffer buffer, Color32 value)
        {
            buffer.Write(value.r);
            buffer.Write(value.g);
            buffer.Write(value.b);
            buffer.Write(value.a);
        }

        public static void Write(this Buffer buffer, NativeString64 value)
        {
            buffer.WriteBlittable(value.LengthInBytes);
            buffer.WriteBlittable(value.buffer);
        }

        /// <summary>
        /// Writes a floating point value to the buffer.
        /// </summary>
        /// <remarks>
        /// The floating point value will be stored in a quantized format.
        /// </remarks>
        /// <param name="value">The floating point value that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, float value)
        {
            buffer.Write((short)(value * 256));
        }

        /// <summary>
        /// Writes a vector2 value to the buffer.
        /// </summary>
        /// <remarks>
        /// The vector2 value will be stored in a quantized format.
        /// </remarks>
        /// <param name="value">The vector2 value that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, Vector2 value)
        {
            buffer.WriteQuantized(value.x);
            buffer.WriteQuantized(value.y);
        }

        /// <summary>
        /// Writes a vector3 value to the buffer.
        /// </summary>
        /// <remarks>
        /// The vector3 value will be stored in a quantized format.
        /// </remarks>
        /// <param name="value">The vector3 value that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, Vector3 value)
        {
            buffer.WriteQuantized(value.x);
            buffer.WriteQuantized(value.y);
            buffer.WriteQuantized(value.z);
        }

        /// <summary>
        /// Writes a vector4 value to the buffer.
        /// </summary>
        /// <remarks>
        /// The vector4 value will be stored in a quantized format.
        /// </remarks>
        /// <param name="value">The vector4 value that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, Vector4 value)
        {
            buffer.WriteQuantized(value.x);
            buffer.WriteQuantized(value.y);
            buffer.WriteQuantized(value.z);
            buffer.WriteQuantized(value.w);
        }

        /// <summary>
        /// Writes a quaternion to the buffer.
        /// </summary>
        /// <remarks>
        /// The quaternion will be stored in a quantized format.
        /// </remarks>
        /// <param name="value">The quaternion that should be written to the buffer.</param>
        public static void WriteQuantized(this Buffer buffer, Quaternion value)
        {
            buffer.WriteQuantized(value.x);
            buffer.WriteQuantized(value.y);
            buffer.WriteQuantized(value.z);
            buffer.WriteQuantized(value.w);
        }

        /// <summary>
        /// Writes a transform to the buffer.
        /// </summary>
        /// <param name="transform">The transform that should be written to the buffer.</param>
        public static void WriteTransform(this Buffer buffer, Transform transform)
        {
            if (transform != null)
            {
                string path = transform.name;
                while (transform.parent != null)
                {
                    transform = transform.parent;
                    path = transform.name + "/" + path;
                }

                buffer.Write(path);
            }
            else
            {
                buffer.Write(string.Empty);
            }
        }

        /// <summary>
        /// Writes a component to the buffer.
        /// </summary>
        /// <param name="component">The component that should be written to the buffer.</param>
        public static void WriteComponent<T>(this Buffer buffer, T component) where T : Component
        {
            int index = GetComponentIndex(component);
            buffer.Write(index);
            if (index >= 0)
            {
                buffer.WriteTransform(component.gameObject.transform);
            }
        }

        public static void WriteNativeArray<T>(this Buffer buffer, NativeArray<T> array, Allocator allocator) where T : struct
        {
            buffer.Write((int)allocator);

            if (allocator == Allocator.Invalid)
            {
                return;
            }

            buffer.Write(array.Length);

            int elemSize;
            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            NativeArray<byte> bytes = array.Reinterpret<byte>(elemSize);
            buffer.Append(bytes);
        }

        public static void WriteNativeList<T>(this Buffer buffer, NativeList<T> list, Allocator allocator) where T : struct
        {
            buffer.Write((int)allocator);

            if (allocator == Allocator.Invalid)
            {
                return;
            }

            buffer.Write(list.Length);

            int elemSize;
            unsafe
            {
                elemSize = UnsafeUtility.SizeOf<T>();
            }

            NativeArray<byte> bytes = list.AsArray().Reinterpret<byte>(elemSize);
            buffer.Append(bytes);
        }

        /// <summary>
        /// Writes a native list to the buffer.
        /// </summary>
        public static unsafe void WriteToStream<T>(this NativeList<T> nativeList, Buffer buffer) where T : struct
        {
            int numElements = nativeList.Length;
            buffer.Write(numElements);
            if (numElements > 0)
            {
                var numBytes = numElements * UnsafeUtility.SizeOf<T>();
                var byteArray = new byte[numBytes];
                fixed(byte* dst = &byteArray[0])
                {
                    UnsafeUtility.MemCpy(dst,
                        NativeListUnsafeUtility.GetUnsafePtr(nativeList),
                        byteArray.Length);
                }
                buffer.Write(byteArray);
            }
        }

        /// <summary>
        /// Writes a native array to the buffer.
        /// </summary>
        public static unsafe void WriteToStream<T>(this NativeArray<T> nativeArray, Buffer buffer) where T : struct
        {
            var numBytes = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            var byteArray = new byte[numBytes];
            fixed(byte* dst = &byteArray[0])
            {
                UnsafeUtility.MemCpy(dst,
                    NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray),
                    byteArray.Length);
            }
            buffer.Write(byteArray);
        }

        public static unsafe void WriteToStream<T>(this NativeSlice<T> nativeSlice, Buffer buffer) where T : struct
        {
            for (int i = 0; i < nativeSlice.Length; ++i)
            {
                buffer.WriteBlittable(nativeSlice[i]);
            }
        }
    }
}
