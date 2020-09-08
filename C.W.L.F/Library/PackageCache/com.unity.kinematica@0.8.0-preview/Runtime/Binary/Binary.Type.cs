using System;

using Unity.Burst;

using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        internal struct Type
        {
            public int nameIndex;
            public int hashCode;
            public int numBytes;
            public int fieldIndex;
            public int numFields;
        }

        internal struct Field
        {
            public int typeIndex;
        }

        /// <summary>
        /// Denotes an index into the list of types contained in the runtime asset.
        /// </summary>
        [Serializable]
        public struct TypeIndex
        {
            internal int value;

            /// <summary>
            /// Determines if the given type index is valid or not.
            /// </summary>
            /// <returns>True if the type index is valid; false otherwise.</returns>
            public bool IsValid => value != Invalid;

            /// <summary>
            /// Determines whether two type indices are equal.
            /// </summary>
            /// <param name="typeIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TypeIndex typeIndex)
            {
                return value == typeIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a type index to an integer value.
            /// </summary>
            public static implicit operator int(TypeIndex typeIndex)
            {
                return typeIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a type index.
            /// </summary>
            public static implicit operator TypeIndex(int typeIndex)
            {
                return Create(typeIndex);
            }

            internal static TypeIndex Create(int typeIndex)
            {
                return new TypeIndex
                {
                    value = typeIndex
                };
            }

            /// <summary>
            /// Invalid type index.
            /// </summary>
            public static TypeIndex Invalid => - 1;
        }

        internal TypeIndex GetTypeIndex(int hashCode)
        {
            int numTypes = this.numTypes;

            for (int i = 0; i < numTypes; ++i)
            {
                if (GetType(i).hashCode == hashCode)
                {
                    return TypeIndex.Create(i);
                }
            }

            return TypeIndex.Invalid;
        }

        /// <summary>
        /// Converts the generic type into a type index.
        /// </summary>
        /// <remarks>
        /// Given a type passed as generic argument, this method performs
        /// a lookup in the types stored in the runtime asset and returns
        /// the corresponding type index. This index can then be used to
        /// extract reflection information for the given type. The returned
        /// type index will be invalid if the generic type passed as argument
        /// can not be found in the runtime asset.
        /// </remarks>
        /// <returns>The type index that corresponds to the generic type.</returns>
        /// <seealso cref="GetType"/>
        public TypeIndex GetTypeIndex<T>() where T : struct
        {
            return GetTypeIndex(
                BurstRuntime.GetHashCode32<T>());
        }

        internal int numTypes => types.Length;

        internal ref Type GetType(int index)
        {
            Assert.IsTrue(index < numTypes);
            return ref types[index];
        }

        internal string GetTypeName(Type type)
        {
            return GetString(type.nameIndex);
        }

        internal int numFields => fields.Length;

        internal Field GetField(int index)
        {
            Assert.IsTrue(index < numFields);
            return fields[index];
        }
    }
}
