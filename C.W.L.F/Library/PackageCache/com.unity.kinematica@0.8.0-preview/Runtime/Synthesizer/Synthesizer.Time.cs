using Unity.Mathematics;

using UnityEngine.Assertions;

using ArgumentException = System.ArgumentException;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        /// <summary>
        /// Switches the pose generation stream to read
        /// from the time index passed as argument.
        /// </summary>
        /// <remarks>
        /// Animation poses are always generated continuously
        /// based on a sampling time. This method allows to switch
        /// to a new starting time. The motion synthesizer automatically
        /// handles any discrepancies between the old and the new pose.
        /// </remarks>
        /// <param name="timeIndex">New starting time index for the pose generation.</param>
        public void PlayAtTime(TimeIndex timeIndex)
        {
            PlayAtTime(
                SamplingTime.Create(
                    timeIndex, Time.theta));
        }

        /// <summary>
        /// Switches the pose generation stream to read
        /// from the sampling time passed as argument.
        /// </summary>
        /// <remarks>
        /// Animation poses are always generated continuously
        /// based on a time index. This method allows to switch
        /// to a new starting time. The motion synthesizer automatically
        /// handles any discrepancies between the old and the new pose.
        /// </remarks>
        /// <param name="samplingTime">New sampling time for the pose generation.</param>
        public void PlayAtTime(SamplingTime samplingTime)
        {
            DebugWriteBlittableObject(ref samplingTime);
            PlayAtTimeDebug playAtTimeDebug = new PlayAtTimeDebug()
            {
                playTime = samplingTime.debugIdentifier
            };
            DebugWriteBlittableObject(ref playAtTimeDebug);

            samplingTime.debugIdentifier = DebugIdentifier.Invalid;

            if (updateInProgress)
            {
                delayedPushTime = samplingTime.timeIndex;
            }
            else
            {
                ref Binary binary = ref Binary;

                if (!binary.IsValid(samplingTime))
                {
                    throw new ArgumentException("Invalid sampling time passed as argument");
                }

                if (!Time.timeIndex.Equals(samplingTime.timeIndex))
                {
                    this.samplingTime = samplingTime;

                    poseGenerator.TriggerTransition();
                }

                Assert.IsTrue(binary.IsValid(Time));

                lastSamplingTime = TimeIndex.Invalid;
            }
        }

        DeltaSamplingTime UpdateTime(float deltaTime)
        {
            if (samplingTime.IsValid)
            {
                updateInProgress = true;

                ref var binary = ref Binary;

                var advance = binary.Advance(samplingTime, deltaTime);

                if (advance.crossedBoundary)
                {
                    var segmentIndex =
                        samplingTime.segmentIndex;

                    ref var segment =
                        ref binary.GetSegment(
                            segmentIndex);

                    var remainder =
                        TimeIndex.Create(segmentIndex,
                            segment.destination.NumFrames);

                    TriggerMarkers(
                        samplingTime.timeIndex, remainder);

                    PlayAtTime(advance.samplingTime);

                    lastSamplingTime = TimeIndex.Invalid;

                    var timeIndex =
                        TimeIndex.Create(
                            advance.samplingTime.segmentIndex);

                    TriggerMarkers(
                        timeIndex,
                        advance.samplingTime.timeIndex);
                }
                else
                {
                    TriggerMarkers(
                        samplingTime.timeIndex,
                        advance.samplingTime.timeIndex);
                }

                //
                // Advance current sampling time
                //

                samplingTime = advance.samplingTime;

                updateInProgress = false;

                if (delayedPushTime.IsValid)
                {
                    PlayAtTime(delayedPushTime);

                    poseGenerator.TriggerTransition();

                    delayedPushTime = TimeIndex.Invalid;
                }

                return advance;
            }

            return DeltaSamplingTime.Invalid;
        }

        /// <summary>
        /// Retrieves the time index at the beginning of the interval
        /// that the time index passed as argument belongs to.
        /// </summary>
        /// <remarks>
        /// This method can be used to "loop" time indices.
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
        /// <param name="timeIndex">Time index for which the corresponding interval start time index should be calculated.</param>
        public TimeIndex Rewind(TimeIndex timeIndex)
        {
            ref var binary = ref Binary;

            ref var segment =
                ref binary.GetSegment(
                    timeIndex.segmentIndex);

            var numIntervals = segment.numIntervals;

            for (int i = 0; i < numIntervals; ++i)
            {
                ref var interval =
                    ref binary.GetInterval(
                        segment.intervalIndex + i);

                Assert.IsTrue(
                    interval.segmentIndex ==
                    timeIndex.segmentIndex);

                if (interval.Contains(timeIndex.frameIndex))
                {
                    return interval.timeIndex;
                }
            }

            return TimeIndex.Invalid;
        }

        /// <summary>
        /// Retrieves the time index at the beginning of the interval
        /// that the sampling time passed as argument belongs to.
        /// </summary>
        /// <remarks>
        /// This method can be used to "loop" time indices.
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
        /// <param name="samplingTime">Sampling time for which the corresponding interval start time index should be calculated.</param>
        public TimeIndex Rewind(SamplingTime samplingTime)
        {
            return Rewind(samplingTime.timeIndex);
        }

        /// <summary>
        /// Denotes the current sampling time of the motion synthesizer.
        /// </summary>
        public SamplingTime Time => samplingTime;
    }
}
