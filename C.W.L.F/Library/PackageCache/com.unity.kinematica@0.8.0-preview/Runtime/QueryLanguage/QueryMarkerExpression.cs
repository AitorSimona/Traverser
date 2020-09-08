using System;
using Unity.Collections;

using TypeIndex = Unity.Kinematica.Binary.TypeIndex;

namespace Unity.Kinematica
{
    /// <summary>
    /// Marker expressions represent semantic filtering of animation poses
    /// that are based on marker types.
    /// </summary>
    /// <remarks>
    /// Constraints can be stacked by adding multiple constraints
    /// to the filter expression. An expression always starts with
    /// zero or more filters based on tag traits and ends with zero or
    /// more filters that are based on marker traits. The portion
    /// of a query expression that filters based on marker is a
    /// marker expression and can be introduced at any point in
    /// the expression. It is not possible to switch from a marker
    /// expression back to a tag expression.
    /// </remarks>
    /// <seealso cref="Query"/>
    public struct QueryMarkerExpression : IDisposable
    {
        MemoryRef<Binary> binary;

        enum Type
        {
            At,
            Before,
            After
        }

        struct Constraint
        {
            public Type type;

            public TypeIndex typeIndex;

            public static Constraint Create(TypeIndex typeIndex, Type type)
            {
                return new Constraint
                {
                    typeIndex = typeIndex,
                    type = type
                };
            }
        }

        [DeallocateOnJobCompletion]
        NativeList<Constraint> constraints;

        QueryResult queryResult;

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
        public static implicit operator QueryResult(QueryMarkerExpression expression)
        {
            return expression.Execute();
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
            constraints.Add(
                Constraint.Create(
                    GetTypeIndex<T>(), Type.At));

            return this;
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
            constraints.Add(
                Constraint.Create(
                    GetTypeIndex<T>(), Type.Before));

            return this;
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
            constraints.Add(
                Constraint.Create(
                    GetTypeIndex<T>(), Type.After));

            return this;
        }

        TypeIndex GetTypeIndex<T>() where T : struct
        {
            ref var binary = ref this.binary.Ref;

            var typeIndex = binary.GetTypeIndex<T>();

            if (!typeIndex.IsValid)
            {
                throw new ArgumentException($"Invalid type {typeof(T).Name} used in marker query expression.");
            }

            return typeIndex;
        }

        internal static QueryMarkerExpression Create(ref Binary binary, QueryResult queryResult)
        {
            var constraints = new NativeList<Constraint>(Allocator.Temp);

            return new QueryMarkerExpression()
            {
                binary = MemoryRef<Binary>.Create(ref binary),
                constraints = constraints,
                queryResult = queryResult
            };
        }

        QueryResult Execute()
        {
            ref var binary = ref this.binary.Ref;

            int numSequences = queryResult.length;

            for (int i = 0; i < numSequences; ++i)
            {
                ApplyConstraints(
                    ref queryResult.sequences.At(i));
            }

            constraints.Dispose();

            return queryResult;
        }

        void ApplyConstraints(ref PoseSequence sequence)
        {
            int numConstraints = constraints.Length;

            for (int i = 0; i < numConstraints; ++i)
            {
                var constraint = constraints[i];

                var typeIndex = constraint.typeIndex;

                if (constraint.type == Type.Before)
                {
                    Before(ref sequence, typeIndex);
                }
                else if (constraint.type == Type.After)
                {
                    After(ref sequence, typeIndex);
                }
                else if (constraint.type == Type.At)
                {
                    At(ref sequence, typeIndex);
                }
            }
        }

        void Before(ref PoseSequence sequence, TypeIndex typeIndex)
        {
            ref var binary = ref this.binary.Ref;

            ref var interval =
                ref binary.GetInterval(
                    sequence.intervalIndex);

            var segmentIndex = interval.segmentIndex;

            var numMarkers = binary.numMarkers;

            for (int i = 0; i < numMarkers; ++i)
            {
                ref var marker = ref binary.GetMarker(i);

                if (marker.segmentIndex == segmentIndex)
                {
                    if (interval.Contains(marker.frameIndex))
                    {
                        var markerType =
                            binary.GetTrait(
                                marker.traitIndex).typeIndex;

                        if (markerType == typeIndex)
                        {
                            var onePastLastFrame = sequence.onePastLastFrame;

                            if (marker.frameIndex < onePastLastFrame)
                            {
                                onePastLastFrame = marker.frameIndex;
                            }

                            sequence.numFrames =
                                onePastLastFrame -
                                sequence.firstFrame;
                        }
                    }
                }
            }
        }

        void After(ref PoseSequence sequence, TypeIndex typeIndex)
        {
            ref var binary = ref this.binary.Ref;

            ref var interval =
                ref binary.GetInterval(
                    sequence.intervalIndex);

            var segmentIndex = interval.segmentIndex;

            var numMarkers = binary.numMarkers;

            for (int i = 0; i < numMarkers; ++i)
            {
                ref var marker = ref binary.GetMarker(i);

                if (marker.segmentIndex == segmentIndex)
                {
                    if (interval.Contains(marker.frameIndex))
                    {
                        var markerType =
                            binary.GetTrait(
                                marker.traitIndex).typeIndex;

                        if (markerType == typeIndex)
                        {
                            var onePastLastFrame = sequence.onePastLastFrame;

                            if (sequence.firstFrame < marker.frameIndex)
                            {
                                sequence.firstFrame = marker.frameIndex;
                            }

                            sequence.numFrames =
                                onePastLastFrame -
                                sequence.firstFrame;
                        }
                    }
                }
            }
        }

        void At(ref PoseSequence sequence, TypeIndex typeIndex)
        {
            ref var binary = ref this.binary.Ref;

            ref var interval =
                ref binary.GetInterval(
                    sequence.intervalIndex);

            var segmentIndex = interval.segmentIndex;

            var numMarkers = binary.numMarkers;

            for (int i = 0; i < numMarkers; ++i)
            {
                ref var marker = ref binary.GetMarker(i);

                if (marker.segmentIndex == segmentIndex)
                {
                    if (interval.Contains(marker.frameIndex))
                    {
                        var markerType =
                            binary.GetTrait(
                                marker.traitIndex).typeIndex;

                        if (markerType == typeIndex)
                        {
                            sequence.firstFrame = marker.frameIndex;
                            sequence.numFrames = 1;
                        }
                    }
                }
            }
        }
    }
}
