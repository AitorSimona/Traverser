using System;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    /// <summary>
    /// A memory reference allows a ref value to be stored in
    /// memory and later to be converted back into its ref value.
    /// </summary>
    public unsafe struct MemoryRef<T> where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        internal void* ptr;

        /// <summary>
        /// Constructs a new memory reference from a ref value.
        /// </summary>
        /// <param name="target">The ref value that this memory reference should be constructed for.</param>
        public MemoryRef(ref T target)
        {
            ptr = UnsafeUtility.AddressOf(ref target);
        }

        /// <summary>
        /// Constructs a new memory reference from a ref value.
        /// </summary>
        /// <param name="target">The ref value that this memory reference should be constructed for.</param>
        public static MemoryRef<T> Create(ref T target)
        {
            return new MemoryRef<T>(ref target);
        }

        /// <summary>
        /// Retrieves the original ref value from the memory reference.
        /// </summary>
        public ref T Ref
        {
            get { return ref UnsafeUtilityEx.AsRef<T>((byte*)ptr); }
        }

        /// <summary>
        /// Determines if the given memory reference is valid or not.
        /// </summary>
        /// <returns>True if the memory reference is valid; false otherwise.</returns>
        public bool IsValid
        {
            get { return ptr != null; }
        }

        public bool Equals(ref T other)
        {
            return ptr == UnsafeUtility.AddressOf(ref other);
        }

        /// <summary>
        /// Invalid memory reference.
        /// </summary>
        public static MemoryRef<T> Null => new MemoryRef<T>();
    }
}
