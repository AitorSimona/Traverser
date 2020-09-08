using Unity.SnapshotDebugger;
using UnityEngine;

namespace Unity.Kinematica
{
    /// <summary>
    /// A sampling time is used to uniquely identfy a sub-sampled animation frame.
    /// </summary>
    /// <remarks>
    /// A sampling time is a construct that is similar in nature to a time index.
    /// It serves exactly the same purpose but carries additional information that
    /// enables sub-sampling.
    /// </remarks>
    /// <seealso cref="TimeIndex"/>
    [Data("Sampling Time", DataFlags.SelfInputOutput)]
    public struct SamplingTime : IDebugObject, IDebugDrawable
    {
        /// <summary>
        /// Denotes a blend weight between 0 and 1 that control sub-frame sampling.
        /// </summary>
        /// <remarks>
        /// The blend weight 'theta" is used for sub-frame sampling. A value of 0
        /// conceptionally refers to the animation frame that the time index refers
        /// to. A value of 1 refers to its immediate next neighboring frame.
        /// </remarks>
        public float theta;

        /// <summary>
        /// Denotes the time index this sampling time refers to.
        /// </summary>
        public TimeIndex timeIndex;


        public DebugIdentifier debugIdentifier { get; set; }

        /// <summary>
        /// Determines if the given sampling time is valid or not.
        /// </summary>
        /// <returns>True if the sampling time is valid; false otherwise.</returns>
        public bool IsValid
        {
            get => timeIndex.IsValid;
        }

        /// <summary>
        /// Denotes the segment this sampling time refers to.
        /// </summary>
        public int segmentIndex
        {
            get => timeIndex.segmentIndex;
        }

        /// <summary>
        /// Denotes the frame index relative to the beginning
        /// of the segment that this sampling time refers to.
        /// </summary>
        public int frameIndex
        {
            get => timeIndex.frameIndex;
        }

        /// <summary>
        /// Create a sampling time from a time index and a theta value.
        /// </summary>
        /// <param name="timeIndex">The time index that the sampling time should refer to.</param>
        /// <param name="theta">The theta value that the sampling time should refer to.</param>
        /// <seealso cref="TimeIndex"/>
        public static SamplingTime Create(TimeIndex timeIndex, float theta = 0.0f)
        {
            return new SamplingTime
            {
                timeIndex = timeIndex,
                theta = theta,
                debugIdentifier = DebugIdentifier.Invalid
            };
        }

        /// <summary>
        /// Invalid sampling time.
        /// </summary>
        public static SamplingTime Invalid
        {
            get => new SamplingTime { timeIndex = TimeIndex.Invalid };
        }

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            if (!timeIndex.IsValid)
            {
                return;
            }

            string text = synthesizer.Binary.GetFragmentDebugText(SamplingTime.Create(timeIndex), "Sampling Clip", options.timeOffset);

            DebugDraw.SetMovableTextTitle(options.textWindowIdentifier, "Sampling Time");
            DebugDraw.AddMovableTextLine(options.textWindowIdentifier, text, options.inputOutputFragTextColor);

            DebugDraw.DrawFragment(ref synthesizer, this, options.inputOutputFragmentColor, options.timeOffset, synthesizer.WorldRootTransform);
        }
    }
}
