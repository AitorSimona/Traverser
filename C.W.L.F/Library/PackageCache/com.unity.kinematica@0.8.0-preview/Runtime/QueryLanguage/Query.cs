using Unity.Collections;

namespace Unity.Kinematica
{
    // (has_a && has_b && !has_d) or (has_a && has_c)

    //          or
    //         /  \
    //        /    \
    //       /      \
    //      and     and
    //     / \      / \
    //    /   \    /   \
    //   a     b  c     d

    // We convert each tag/payload combination into a unique identifier.
    // We convert the timeline into a sequence of intervals where each
    // interval is associated with a list of unique identifiers.
    // A query iterates this list and finds matching intervals.

    /// <summary>
    /// Entry point for semantic filtering based query language.
    /// </summary>
    /// <remarks>
    /// Methods in this struct serve as an entry point for query
    /// language based constructs. A query performs semantic filtering
    /// of the motion library. The result of this filtering process
    /// is always a pose sequence, which represents the poses that
    /// match the criterias used as constraints.
    /// <para>
    /// Constraints can be stacked by adding multiple constraints
    /// to the filter expression. An expression always starts with
    /// zero or more filters based on tag traits and ends with zero or
    /// more filters that are based on marker traits.
    /// </para>
    /// <para>
    /// Task creation methods offered by the motion synthesizer
    /// typically consume pose sequences directly and subsequently
    /// select a single pose out of the input sequence based on
    /// certain criterias, like matching a given reference pose
    /// and/or matching a given input trajectory.
    /// </para>
    /// <example>
    /// <code>
    /// var result = synthesizer.Query.Where(
    ///     Locomotion.Default).And(Idle.Default));
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="MotionSynthesizer.Query"/>
    /// <seealso cref="MotionSynthesizer.Push"/>
    /// <seealso cref="Binary.Interval"/>
    public struct Query
    {
        MemoryRef<Binary> binary;

        internal static Query Create(ref Binary binary)
        {
            return new Query()
            {
                binary = MemoryRef<Binary>.Create(ref binary)
            };
        }

        /// <summary>
        /// Introduces a tag trait expression that selects based on a "where" clause.
        /// </summary>
        /// <remarks>
        /// A "where" clause matches all intervals from the motion library
        /// that contain the trait passed as argument.
        /// <example>
        /// <code>
        /// synthesizer.Query.Where(Climbing.Create(Climbing.Type.Wall));
        /// </code>
        /// </example>
        /// </remarks>
        /// <param name="value">The trait values to be used as constraint.</param>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryTraitExpression Where<T>(T value) where T : struct
        {
            return
                QueryTraitExpression.Create(
                ref binary.Ref).And(value);
        }

        public QueryTraitExpression Where<T>(NativeString64 debugName, T value) where T : struct
        {
            return
                QueryTraitExpression.Create(
                ref binary.Ref, debugName).And(value);
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on an "at" clause.
        /// </summary>
        /// <remarks>
        /// An "at" clause matches all single poses from the motion library
        /// that contain the marker type passed as argument.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression At<T>() where T : struct
        {
            return
                QueryMarkerExpression.Create(
                ref binary.Ref, QueryResult.Empty).At<T>();
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on a "before" clause.
        /// </summary>
        /// <remarks>
        /// A "before" clause matches all poses from the motion library
        /// that come "before" the specified marker type. The result is most
        /// likely a partial interval, assuming that the marker has been
        /// not been placed at the very beginning or end of an interval.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression Before<T>() where T : struct
        {
            return
                QueryMarkerExpression.Create(
                ref binary.Ref, QueryResult.Empty).Before<T>();
        }

        /// <summary>
        /// Introduces a marker trait expression that selects based on an "after" clause.
        /// </summary>
        /// <remarks>
        /// An "after" clause matches all poses from the motion library
        /// that come "after" the specified marker type. The result is most
        /// likely a partial interval, assuming that the marker has been
        /// not been placed at the very beginning or end of an interval.
        /// <example>
        /// <code>
        /// synthesizer.Query.At<Loop>();
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryMarkerExpression After<T>() where T : struct
        {
            return
                QueryMarkerExpression.Create(
                ref binary.Ref, QueryResult.Empty).After<T>();
        }
    }
}
