using System;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        /// <summary>
        /// Markers are used to associate a trait to a single animation frame.
        /// </summary>
        /// <remarks>
        /// Markers can be used in a variety of scenarios:
        /// * Narrow the result of a query
        /// * Control behavior of a task
        /// * Execute custom code
        /// <para>
        /// Markers are a general purpose tool that allows a custom trait to be
        /// associated with a single animation frame. Queries support markers
        /// directly by allowing to narrow searches to all frames that come before,
        /// after or at a marker of a certain type.
        /// </para>
        /// <para>
        /// Markers can also be accessed directly given a reference to the runtime
        /// asset and therefore tasks can perform any kind of custom processing.
        /// </para>
        /// <para>
        /// Markers can also be accessed directly given a reference to the runtime
        /// asset and therefore tasks can perform any kind of custom processing.
        /// </para>
        /// <para>
        /// Markers allow for code execution in case a marker has been placed
        /// on a frame that the syntheszier samples during playback.
        /// </para>
        /// <example>
        /// <code>
        /// [Trait, BurstCompile]
        /// public struct Loop : Trait
        /// {
        ///     public void Execute(ref MotionSynthesizer synthesizer)
        ///     {
        ///         synthesizer.Push(synthesizer.Rewind(synthesizer.Time));
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="Tag"/>
        /// <seealso cref="Trait"/>
        /// <seealso cref="Query"/>
        public struct Marker
        {
            /// <summary>
            /// Denotes the segment this marker has been associated with.
            /// </summary>
            public SegmentIndex segmentIndex;

            /// <summary>
            /// Denotes the associated trait.
            /// </summary>
            public TraitIndex traitIndex;

            /// <summary>
            /// Frame index relative to the segment start frame at which the marker has been placed.
            /// </summary>
            public int frameIndex;
        }

        /// <summary>
        /// Denotes an index into the list of markers contained in the runtime asset.
        /// </summary>
        public struct MarkerIndex
        {
            internal int value;

            /// <summary>
            /// Determines if the given marker index is valid or not.
            /// </summary>
            /// <returns>True if the marker index is valid; false otherwise.</returns>
            public bool IsValid => value != Invalid;

            /// <summary>
            /// Determines whether two marker indices are equal.
            /// </summary>
            /// <param name="markerIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(MarkerIndex markerIndex)
            {
                return value == markerIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a marker index to an integer value.
            /// </summary>
            public static implicit operator int(MarkerIndex markerIndex)
            {
                return markerIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a marker index.
            /// </summary>
            public static implicit operator MarkerIndex(int markerIndex)
            {
                return Create(markerIndex);
            }

            internal static MarkerIndex Create(int markerIndex)
            {
                return new MarkerIndex
                {
                    value = markerIndex
                };
            }

            /// <summary>
            /// Invalid marker index.
            /// </summary>
            public static MarkerIndex Invalid => - 1;
        }

        /// <summary>
        /// Returns the number of markers stored in the runtime asset.
        /// </summary>
        /// <seealso cref="Marker"/>
        public int numMarkers => markers.Length;

        /// <summary>
        /// Retrieves a reference to a marker stored in the runtime asset.
        /// </summary>
        /// <param name="index">The marker index to retrieve the reference for.</param>
        /// <returns>Marker reference that corresponds to the index passed as argument.</returns>
        public ref Marker GetMarker(MarkerIndex index)
        {
            Assert.IsTrue(index < numMarkers);
            return ref markers[index];
        }

        /// <summary>
        /// Determines whether the specified marker is an instance of the type passed as argument.
        /// </summary>
        /// <param name="markerIndex">The marker to compare with the type.</param>
        /// <param name="typeIndex">The type to compare the marker for.</param>
        /// <returns>True if the marker is of the specified type; false otherwise.</returns>
        public bool IsType(MarkerIndex markerIndex, TypeIndex typeIndex)
        {
            ref var marker = ref GetMarker(markerIndex);

            ref var trait = ref GetTrait(marker.traitIndex);

            return trait.typeIndex == typeIndex;
        }
    }
}
