using UnityEngine.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        /// <summary>
        /// Traits are user-defined characteristics that can be associated to tags or markers.
        /// </summary>
        /// <remarks>
        /// Users can define own custom data by using C# structs. These structs
        /// will then show up in the Kinematica builder tool and allow tags or markers
        /// to be created carrying specific instances of the corresponding traits.
        /// A trait itself wraps the actual payload (the instance of the user-defined struct).
        /// <example>
        /// <code>
        /// [Trait]
        /// public struct Anchor
        /// {
        ///     public AffineTransform transform;
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Tag"/>
        /// <seealso cref="Marker"/>
        /// <seealso cref="Query"/>
        public struct Trait
        {
            /// <summary>
            /// Denotes the type index of this trait.
            /// </summary>
            public TypeIndex typeIndex;

            // Allows access to the associated payload;
            // The associated payload is guaranteed to be
            // of type 'type', see field 'TypeIndex'.
            internal int payload;

            /// <summary>
            /// Determines if the trait is equal to a type passed as argument.
            /// </summary>
            /// <param name="typeIndex">The type to compare the trait to.</param>
            /// <returns>True if the trait is equal to the type passed as argument; otherwise, false.</returns>
            public bool IsType(TypeIndex typeIndex)
            {
                return this.typeIndex.Equals(typeIndex);
            }
        }

        /// <summary>
        /// Denotes an index into the list of traits contained in the runtime asset.
        /// </summary>
        public struct TraitIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two trait indices are equal.
            /// </summary>
            /// <param name="traitIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TraitIndex traitIndex)
            {
                return value == traitIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a trait index to an integer value.
            /// </summary>
            public static implicit operator int(TraitIndex traitIndex)
            {
                return traitIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a trait index.
            /// </summary>
            public static implicit operator TraitIndex(int traitIndex)
            {
                return Create(traitIndex);
            }

            internal static TraitIndex Create(int traitIndex)
            {
                return new TraitIndex
                {
                    value = traitIndex
                };
            }

            /// <summary>
            /// Empty codebook index.
            /// </summary>
            public static TraitIndex Empty => - 1;
        }

        /// <summary>
        /// Returns the number of traits stored in the runtime asset.
        /// </summary>
        /// <seealso cref="Trait"/>
        public int numTraits => traits.Length;

        /// <summary>
        /// Retrieves a reference to a trait stored in the runtime asset.
        /// </summary>
        /// <param name="traitIndex">The trait index to retrieve the reference for.</param>
        /// <returns>Trait reference that corresponds to the index passed as argument.</returns>
        public ref Trait GetTrait(TraitIndex traitIndex)
        {
            Assert.IsTrue(traitIndex < numTraits);
            return ref traits[traitIndex];
        }

        /// <summary>
        /// Converts a value to a trait index.
        /// </summary>
        /// <remarks>
        /// <example>
        /// <code>
        /// var traitIndex =
        ///     binary.GetTraitIndex(
        ///         Climbing.Create(Climbing.Type.Wall));
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="value">The value to be converted to a trait index.</param>
        /// <returns>The trait index that corresponds to the value passed as argument.</returns>
        public TraitIndex GetTraitIndex<T>(T value) where T : struct
        {
            var typeIndex = GetTypeIndex<T>();

            if (typeIndex != TypeIndex.Invalid)
            {
                int numTraits = this.numTraits;

                for (int i = 0; i < numTraits; ++i)
                {
                    if (traits[i].IsType(typeIndex))
                    {
                        if (IsPayload(ref value, traits[i].payload))
                        {
                            return i;
                        }
                    }
                }
            }

            return TraitIndex.Empty;
        }

        internal unsafe byte[] GetTraitPayload(TraitIndex traitIndex)
        {
            var trait = GetTrait(traitIndex);

            int numBytes = GetType(trait.typeIndex).numBytes;

            byte* ptr = (byte*)payloads.GetUnsafePtr() + trait.payload;

            var result = new byte[numBytes];

            for (int i = 0; i < numBytes; ++i)
            {
                result[i] = ptr[i];
            }

            return result;
        }

        /// <summary>
        /// Retrieves the associated payload (i.e. the instance of the user-defined value) for a given trait index.
        /// </summary>
        /// <param name="traitIndex">The trait index for which the corresponding payload is to be retrieved.</param>
        /// <returns>Reference to the user-defined value that corresponds to the trait index passed as argument.</returns>
        public unsafe ref T GetPayload<T>(TraitIndex traitIndex) where T : struct
        {
            var trait = GetTrait(traitIndex);

            byte* ptr = (byte*)payloads.GetUnsafePtr() + trait.payload;

            return ref UnsafeUtilityEx.AsRef<T>(ptr);
        }
    }
}
