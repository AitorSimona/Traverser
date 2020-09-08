using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Time related methods
        //

        /// <summary>
        /// Converts a time in seconds to an index.
        /// </summary>
        /// <param name="timeInSeconds">Time in seconds to be converted into an index.</param>
        /// <returns>Frame index that corresponds to the time in seconds passed as argument.</returns>
        public int TimeInSecondsToIndex(float timeInSeconds)
        {
            Assert.IsTrue(timeInSeconds >= 0.0f);
            return Missing.truncToInt(timeInSeconds * SampleRate);
        }

        /// <summary>
        /// Converts a frame index to the corresponding time in seconds.
        /// </summary>
        /// <param name="index">Frame index to be converted into a time in seconds.</param>
        /// <returns>Time in seconds that corresponds to the frame index passed as argument.</returns>
        public float IndexToTimeInSeconds(int index)
        {
            return (float)index / SampleRate;
        }

        /// <summary>
        /// Advances a sampling time by a certain amount of time.
        /// </summary>
        /// <remarks>
        /// This method advances a starting time by a certain amount of time.
        /// <para>
        /// It is valid to pass a negative delta time in order to step
        /// backward in time.
        /// </para>
        /// <para>
        /// The resulting time can belong to a different segment if boundary
        /// clips have been configured for the segment that the initial time
        /// belongs to.
        /// </para>
        /// <para>
        /// The resulting new sampling time will be clamped at segment boundaries
        /// if no boundary clips have been defined for the segment that the initial time belongs to.
        /// </para>
        /// </remarks>
        /// <param name="samplingTime">The reference time to advance from.</param>
        /// <param name="deltaTime">The time increment to advance by in seconds.</param>
        /// <returns>The resulting sampling time.</returns>
        public DeltaSamplingTime Advance(SamplingTime samplingTime, float deltaTime)
        {
            ref var segment = ref GetSegment(samplingTime.segmentIndex);

            var inverseSampleRate = math.rcp(SampleRate);

            float theta = samplingTime.theta + deltaTime / inverseSampleRate;

            float truncated = math.floor(theta);

            samplingTime.theta = math.saturate(theta - truncated);

            int frameIndex =
                samplingTime.timeIndex.frameIndex +
                Missing.truncToInt(truncated);

            int numFramesMinusOne =
                segment.destination.NumFrames - 1;

            var deltaTransform = AffineTransform.identity;

            var crossedBoundary = false;

            if (frameIndex < 0)
            {
                if (segment.previousSegment.IsValid)
                {
                    crossedBoundary = true;

                    ref var previousSegment = ref GetSegment(segment.previousSegment);

                    deltaTransform =
                        GetTrajectoryTransformGap(
                            samplingTime.segmentIndex,
                            segment.previousSegment).inverse();

                    numFramesMinusOne =
                        previousSegment.destination.NumFrames - 1;

                    samplingTime.timeIndex =
                        TimeIndex.Create(segment.previousSegment);

                    frameIndex += previousSegment.destination.numFrames;

                    if (frameIndex < 0)
                    {
                        frameIndex = 0;

                        samplingTime.theta = 0.0f;
                    }
                }
                else
                {
                    frameIndex = 0;

                    samplingTime.theta = 0.0f;
                }
            }
            else if (frameIndex >= numFramesMinusOne)
            {
                if (segment.nextSegment.IsValid)
                {
                    crossedBoundary = true;

                    frameIndex -= numFramesMinusOne;

                    ref var nextSegment = ref GetSegment(segment.nextSegment);

                    deltaTransform =
                        GetTrajectoryTransformGap(
                            segment.nextSegment,
                            samplingTime.segmentIndex);

                    numFramesMinusOne =
                        nextSegment.destination.NumFrames - 1;

                    samplingTime.timeIndex =
                        TimeIndex.Create(segment.nextSegment);

                    if (frameIndex >= numFramesMinusOne)
                    {
                        frameIndex = numFramesMinusOne;

                        samplingTime.theta = 0.0f;
                    }
                }
                else
                {
                    frameIndex = numFramesMinusOne;

                    samplingTime.theta = 0.0f;
                }
            }

            samplingTime.timeIndex.frameIndex = (short)frameIndex;

            Assert.IsTrue(samplingTime.timeIndex.frameIndex >= 0);
            Assert.IsTrue(samplingTime.timeIndex.frameIndex <= numFramesMinusOne);

            return DeltaSamplingTime.Create(samplingTime, deltaTransform, crossedBoundary);
        }

        /// <summary>
        /// Determines if a given time index is valid.
        /// </summary>
        /// <param name="timeIndex">The time index to be checked.</param>
        /// <returns>True if the specified time index is valid; otherwise, false.</returns>
        public bool IsValid(TimeIndex timeIndex)
        {
            bool contains(int value, int min, int max)
            {
                return (value >= min) && (value < max);
            }

            if (!contains(timeIndex.segmentIndex, 0, numSegments))
            {
                return false;
            }

            int numFrames = GetSegment(
                timeIndex.segmentIndex).destination.NumFrames;

            return contains(timeIndex.frameIndex, 0, numFrames);
        }

        /// <summary>
        /// Determines if a given sampling time is valid.
        /// </summary>
        /// <param name="samplingTime">The sampling time to be checked.</param>
        /// <returns>True if the specified sampling time is valid; otherwise, false.</returns>
        public bool IsValid(SamplingTime samplingTime)
        {
            bool contains(float value, float min, float max)
            {
                return (value >= min) && (value <= max);
            }

            if (!IsValid(samplingTime.timeIndex))
            {
                return false;
            }

            return contains(samplingTime.theta, 0.0f, 1.0f);
        }

        internal AnimationSampleTimeIndex GetAnimationSampleTimeIndex(TimeIndex timeIndex)
        {
            if (timeIndex.IsValid)
            {
                ref var segment =
                    ref GetSegment(
                        timeIndex.segmentIndex);

                var name = GetString(segment.nameIndex);

                int frameIndex =
                    segment.MapRelativeFrameIndex(
                        timeIndex.frameIndex);

                frameIndex += segment.source.firstFrame;

                return new AnimationSampleTimeIndex()
                {
                    clipGuid = segment.guid,
                    clipName = name,
                    animFrameIndex = frameIndex
                };
            }

            return AnimationSampleTimeIndex.CreateInvalid();
        }

        internal TimeIndex GetTimeIndexFromAnimSampleTime(AnimationSampleTimeIndex animSampleTime)
        {
            for (int i = 0; i < numSegments; ++i)
            {
                ref var segment = ref GetSegment(i);

                if (segment.guid == animSampleTime.clipGuid)
                {
                    if (segment.source.Contains(animSampleTime.animFrameIndex))
                    {
                        var relativeIndex =
                            animSampleTime.animFrameIndex - segment.source.firstFrame;

                        var mappedRelativeIndex =
                            segment.InverseMapRelativeFrameIndex(relativeIndex);

                        return TimeIndex.Create(i, mappedRelativeIndex);
                    }
                }
            }

            return TimeIndex.Invalid;
        }
    }
}
