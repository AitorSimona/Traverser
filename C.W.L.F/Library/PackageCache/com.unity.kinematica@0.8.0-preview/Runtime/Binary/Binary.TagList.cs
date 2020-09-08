using System;
using System.Diagnostics;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    public partial struct Binary
    {
        /// <summary>
        /// Tag lists are created during the runtime asset build process based
        /// on the annotations that have been made at authoring time. Each unique
        /// combination of tags that is used for annotation purposes generates
        /// a unique tag list which is accessible via an index. This scheme is
        /// used extensively to speed up the execution of queries.
        /// </summary>
        /// <seealso cref="Segment"/>
        /// <seealso cref="Interval"/>
        /// <seealso cref="Query"/>
        public struct TagList
        {
            /// <summary>
            /// Denotes the starting index relative to the global list of tag indices.
            /// </summary>
            public int tagIndicesIndex;

            /// <summary>
            /// Denotes the size of the tax index list.
            /// </summary>
            public int numIndices;
        }

        /// <summary>
        /// Denotes an index into the list of tag lists contained in the runtime asset.
        /// </summary>
        public struct TagListIndex
        {
            internal int value;

            /// <summary>
            /// Determines whether two tag list indices are equal.
            /// </summary>
            /// <param name="tagListIndex">The index to compare against the current index.</param>
            /// <returns>True if the specified index is equal to the current index; otherwise, false.</returns>
            public bool Equals(TagListIndex tagListIndex)
            {
                return value == tagListIndex.value;
            }

            /// <summary>
            /// Implicit conversion from a tag list index to an integer value.
            /// </summary>
            public static implicit operator int(TagListIndex tagListIndex)
            {
                return tagListIndex.value;
            }

            /// <summary>
            /// Implicit conversion from an integer value to a tag list index.
            /// </summary>
            public static implicit operator TagListIndex(int tagListIndex)
            {
                return Create(tagListIndex);
            }

            internal static TagListIndex Create(int tagListIndex)
            {
                return new TagListIndex
                {
                    value = tagListIndex
                };
            }

            /// <summary>
            /// Empty tag list index.
            /// </summary>
            public static TagListIndex Empty => - 1;
        }

        /// <summary>
        /// Returns the number of tag lists stored in the runtime asset.
        /// </summary>
        /// <seealso cref="TagList"/>
        public int numTagLists => tagLists.Length;

        /// <summary>
        /// Retrieves a tag list given a tag list index from the runtime asset.
        /// </summary>
        /// <param name="tagListIndex">The tag list index.</param>
        /// <returns>Reference to the corresponding tag list for the index passed as argument.</returns>
        public ref TagList GetTagList(TagListIndex tagListIndex)
        {
            Assert.IsTrue(tagListIndex < numTagLists);
            return ref tagLists[tagListIndex];
        }

        /// <summary>
        /// Returns the tag index from the tag list at given index
        /// </summary>
        /// <param name="tagList">List of tag indices</param>
        /// <param name="index">Index of tag index in the list</param>
        /// <returns></returns>
        public TagIndex GetTagIndex(ref TagList tagList, int index)
        {
            return tagIndices[tagList.tagIndicesIndex + index];
        }

        /// <summary>
        /// Checks if a given trait belongs to a given trait list.
        /// </summary>
        /// <param name="tagList">The tag list to check against.</param>
        /// <param name="traitIndex">The trait to check for.</param>
        /// <returns>True if the given trait belongs to the trait list, false otherwise.</returns>
        public bool Contains(ref TagList tagList, TraitIndex traitIndex)
        {
            for (int i = 0; i < tagList.numIndices; ++i)
            {
                var tagIndex = tagIndices[tagList.tagIndicesIndex + i];

                if (GetTag(tagIndex).traitIndex == traitIndex)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a given trait belongs to a given trait list.
        /// </summary>
        /// <param name="tagListIndex">The tag list to check against.</param>
        /// <param name="traitIndex">The trait to check for.</param>
        /// <returns>True if the given trait belongs to the trait list, false otherwise.</returns>
        public bool Contains(TagListIndex tagListIndex, TraitIndex traitIndex)
        {
            return Contains(ref GetTagList(tagListIndex), traitIndex);
        }
    }
}
