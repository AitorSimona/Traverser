using Unity.Collections;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Tag related methods
        //

        /// <summary>
        /// Tags are used to associate a trait to a sequence of animation frames.
        /// </summary>
        /// <remarks>
        /// Tags can be used in a variety of scenarios:
        /// * Narrow the result of a query
        /// * Control behavior of a task
        /// <para>
        /// Tags are a general purpose tool that allows a custom trait to be
        /// associated with a sequence of animation frames. Queries support tags
        /// directly by allowing to include or exclude animation frames based on
        /// the associated tags.
        /// </para>
        /// <para>
        /// Tags can also be accessed directly given a reference to the runtime
        /// asset and therefore tasks can perform any kind of custom processing
        /// based on them.
        /// </para>
        /// </remarks>
        /// <seealso cref="Marker"/>
        /// <seealso cref="Trait"/>
        /// <seealso cref="Query"/>
        public struct Tag
        {
            /// <summary>
            /// Denotes the segment this tag belongs to.
            /// </summary>
            public SegmentIndex segmentIndex;

            /// <summary>
            /// Denotes the associated trait.
            /// </summary>
            public TraitIndex traitIndex;

            /// <summary>
            /// Denotes the first frame this tag refers to.
            /// </summary>
            public int firstFrame;

            /// <summary>
            /// Denotes the number of frames this tag refers to.
            /// </summary>
            public int numFrames;

            /// <summary>
            /// Denotes the first interval index for this tag.
            /// </summary>
            /// <seealso cref="Interval"/>
            public IntervalIndex intervalIndex;

            /// <summary>
            /// Denotes the number of intervals this tag belongs to.
            /// </summary>
            public int numIntervals;

            /// <summary>
            /// Denotes the first frame this tag refers to.
            /// </summary>
            public int FirstFrame => firstFrame;

            /// <summary>
            /// Denotes the number of frames this tag refers to.
            /// </summary>
            public int NumFrames => numFrames;

            /// <summary>
            /// Denotes the one-past-last frame this tag refer to.
            /// </summary>
            public int OnePastLastFrame => FirstFrame + NumFrames;
        }

        /// <summary>
        /// Denotes an index into the list of tags contained in the runtime asset.
        /// </summary>
        public struct TagIndex
        {
            internal int value;

            /// <summary>
            /// Determines if the given tag index is valid or not.
            /// </summary>
            /// <returns>True if the tag index is valid; false otherwise.</returns>
            public bool IsValid => value != Invalid;

            /// <summary>
            /// Determines whether two tag indices are equal.
            /// </summary>
            /// <param name="tagIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TagIndex tagIndex)
            {
                return value == tagIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a tag index to an integer value.
            /// </summary>
            public static implicit operator int(TagIndex tagIndex)
            {
                return tagIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a tag index.
            /// </summary>
            public static implicit operator TagIndex(int tagIndex)
            {
                return Create(tagIndex);
            }

            internal static TagIndex Create(int tagIndex)
            {
                return new TagIndex
                {
                    value = tagIndex
                };
            }

            /// <summary>
            /// Invalid tag index.
            /// </summary>
            public static TagIndex Invalid => - 1;
        }

        /// <summary>
        /// Returns the number of tags stored in the runtime asset.
        /// </summary>
        /// <seealso cref="Tag"/>
        public int numTags => tags.Length;

        /// <summary>
        /// Retrieves a reference to a tag stored in the runtime asset.
        /// </summary>
        /// <param name="tagIndex">The tag index to retrieve the reference for.</param>
        /// <returns>Tag reference that corresponds to the index passed as argument.</returns>
        public ref Tag GetTag(TagIndex tagIndex)
        {
            Assert.IsTrue(tagIndex < numTags);
            return ref tags[tagIndex];
        }
    }
}
