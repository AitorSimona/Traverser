using System;
using UnityEngine.Assertions;
using Unity.Collections;

using TraitIndex = Unity.Kinematica.Binary.TraitIndex;
using TagListIndex = Unity.Kinematica.Binary.TagListIndex;

namespace Unity.Kinematica
{
    /// <summary>
    /// Trait expressions represent semantic filtering of animation poses
    /// that are based on tag traits.
    /// </summary>
    /// <remarks>
    /// Constraints can be stacked by adding multiple constraints
    /// to the filter expression. An expression always starts with
    /// zero or more filters based on tag traits and ends with zero or
    /// more filters that are based on marker traits. The portion
    /// of a query expression that filters based on tag traits is a
    /// trait expression and can be introduced at any point in
    /// the expression. It is possible to switch from a trait expression
    /// to a marker expression, but not visa versa.
    /// </remarks>
    /// <seealso cref="Query"/>
    public struct QueryTraitExpression : IDisposable
    {
        MemoryRef<Binary> binary;

        struct Constraint
        {
            public int chainLength;

            public bool include;

            public TraitIndex traitIndex;

            public static Constraint Create(TraitIndex traitIndex, bool include = true)
            {
                return new Constraint
                {
                    traitIndex = traitIndex,
                    include = include
                };
            }
        }

        [DeallocateOnJobCompletion]
        NativeList<Constraint> constraints;

        NativeString64 debugName;

        int chainIndex;

        /// <summary>
        /// Disposes the underlying constructed filter expression.
        /// </summary>
        public void Dispose()
        {
            constraints.Dispose();
        }

        /// <summary>
        /// Implicit conversion from a marker expression to a pose sequence.
        /// </summary>
        public static implicit operator QueryResult(QueryTraitExpression expression)
        {
            return expression.Execute();
        }

        public static implicit operator PoseSet(QueryTraitExpression expression)
        {
            return expression.Execute();
        }

        /// <summary>
        /// Appends a tag trait constraint to the existing tag trait expression.
        /// </summary>
        /// <remarks>
        /// An "and" clause matches all intervals from the motion library
        /// that contain the trait passed as argument and any constraint
        /// that has been previously specified as part of the tag trait expression.
        /// <example>
        /// <code>
        /// synthesizer.Query.Where(
        ///     Locomotion.Default).And(Idle.Default));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryTraitExpression And<T>(T value) where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Insert(Constraint.Create(traitIndex));

            return this;
        }

        /// <summary>
        /// Appends a tag trait constraint to the existing tag trait expression.
        /// </summary>
        /// <remarks>
        /// An "except" clause matches all intervals from the motion library
        /// that does not contain the trait passed as argument.
        /// <example>
        /// <code>
        /// synthesizer.Query.Where(
        ///     Locomotion.Default).Except(Idle.Default));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryTraitExpression Except<T>(T value) where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Insert(Constraint.Create(traitIndex, false));

            return this;
        }

        /// <summary>
        /// Appends a tag trait constraint to the existing tag trait expression.
        /// </summary>
        /// <remarks>
        /// An "or" clause matches all intervals from the motion library
        /// that contain the trait passed as argument or any constraint
        /// that has been previously specified as part of the tag trait expression.
        /// <example>
        /// <code>
        /// synthesizer.Query.Where(
        ///     Locomotion.Default).Or(Locomotion.Crouching));
        /// </code>
        /// </example>
        /// </remarks>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public QueryTraitExpression Or<T>(T value) where T : struct
        {
            var traitIndex = binary.Ref.GetTraitIndex(value);

            Append(Constraint.Create(traitIndex));

            return this;
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
            return marker.At<T>();
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
            return marker.Before<T>();
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
            return marker.After<T>();
        }

        QueryMarkerExpression marker
        {
            get
            {
                return QueryMarkerExpression.Create(ref binary.Ref, Execute());
            }
        }

        internal static QueryTraitExpression Create(ref Binary binary, NativeString64 debugName = default(NativeString64))
        {
            var constraints = new NativeList<Constraint>(Allocator.Temp);

            return new QueryTraitExpression()
            {
                binary = MemoryRef<Binary>.Create(ref binary),
                constraints = constraints,
                debugName = debugName
            };
        }

        ref Binary Binary => ref binary.Ref;

        bool MatchesConstraints(TagListIndex tagListIndex)
        {
            ref Binary binary = ref Binary;

            int numConstraints = constraints.Length;

            int readIndex = 0;

            while (readIndex < numConstraints)
            {
                int chainLength =
                    constraints[readIndex].chainLength;

                int matchIndex = 0;

                while (matchIndex < chainLength)
                {
                    var constraint = constraints[readIndex + matchIndex];

                    var contains =
                        binary.Contains(
                            tagListIndex, constraint.traitIndex);

                    if (constraint.include != contains)
                    {
                        break;
                    }

                    matchIndex++;
                }

                if (matchIndex == chainLength)
                {
                    return true;
                }

                readIndex += chainLength;
            }

            return false;
        }

        QueryResult Execute()
        {
            var queryResult = QueryResult.Create();
            queryResult.debugName = debugName;

            ref Binary binary = ref Binary;

            int numIntervals = binary.numIntervals;

            for (int i = 0; i < numIntervals; ++i)
            {
                ref var interval = ref binary.GetInterval(i);

                if (MatchesConstraints(interval.tagListIndex))
                {
                    queryResult.Add(i,
                        interval.firstFrame, interval.numFrames);
                }
            }

            constraints.Dispose();

            return queryResult;
        }

        void Insert(Constraint constraint)
        {
            if (constraints.Length > 0)
            {
                var link = constraints[chainIndex];

                constraints.Add(link);

                constraint.chainLength = link.chainLength + 1;

                constraints[chainIndex] = constraint;
            }
            else
            {
                constraint.chainLength = 1;

                constraints.Add(constraint);
            }
        }

        void Append(Constraint constraint)
        {
            Assert.IsTrue(constraints.Length > 0);

            constraint.chainLength = 1;

            chainIndex += constraints[chainIndex].chainLength;

            Assert.IsTrue(chainIndex == constraints.Length);

            constraints.Add(constraint);
        }
    }
}
