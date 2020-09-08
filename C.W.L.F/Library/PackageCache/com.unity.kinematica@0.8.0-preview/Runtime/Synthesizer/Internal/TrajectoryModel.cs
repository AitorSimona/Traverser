using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine.Assertions;

using UnityEngine;

namespace Unity.Kinematica
{
    /// <summary>
    /// The trajectory model maintains a representation of the simulated
    /// character movement over the global time horizon.
    /// </summary>
    /// <remarks>
    /// The simulated trajectory is relative to the current world
    /// root transform, i.e. in character space.
    /// </para>
    /// </remarks>
    public struct TrajectoryModel
    {
        internal static void Reserve(ref ArrayMemory memory, ref Binary binary)
        {
            float sampleRate = binary.SampleRate;
            float timeHorizon = binary.TimeHorizon;

            int trajectoryLength =
                DurationToFrames(timeHorizon * 2.0f, sampleRate);

            memory.Reserve<AffineTransform>(trajectoryLength);
            memory.Reserve<AccumulatedTransform>(trajectoryLength * 2);
        }

        internal TrajectoryModel(ref ArrayMemory memory, ref Binary binary)
        {
            //
            // Initialize attributes
            //

            this.binary = MemoryRef<Binary>.Create(ref binary);

            float sampleRate = binary.SampleRate;
            float timeHorizon = binary.TimeHorizon;

            int trajectoryLength =
                DurationToFrames(timeHorizon * 2.0f, sampleRate);

            trajectory = memory.CreateSlice<AffineTransform>(trajectoryLength);
            for (int i = 0; i < trajectoryLength; ++i)
            {
                trajectory[i] = AffineTransform.identity;
            }

            var accumulatedIdentity =
                AccumulatedTransform.Create(
                    AffineTransform.identity, math.rcp(binary.SampleRate));

            deltaSpaceTrajectory = memory.CreateSlice<AccumulatedTransform>(trajectoryLength * 2);
            for (int i = 0; i < trajectoryLength * 2; ++i)
            {
                deltaSpaceTrajectory[i] = accumulatedIdentity;
            }

            Assert.IsTrue(trajectoryLength == TrajectoryLength);
        }

        internal void Update(AffineTransform worldRootTransform, AffineTransform deltaTransform, float deltaTime)
        {
            NativeSlice<AffineTransform> trajectory = Array;

            int trajectoryLength = TrajectoryLength;
            int halfTrajectoryLength = trajectoryLength / 2;

            //
            // Since the trajectory is relative to the world root transform
            // and the synthesizer is adding 'deltaTransform' we need
            // to account for this by inverse transforming the
            // character space trajectory transforms.
            //

            var inverseQ = math.normalize(
                Missing.conjugate(deltaTransform.q));

            for (int i = halfTrajectoryLength; i < trajectoryLength; ++i)
            {
                var transform = trajectory[i];
                transform.t =
                    Missing.rotateVector(
                        inverseQ, transform.t);
                transform.q = math.normalize(
                    math.mul(inverseQ, transform.q));
                trajectory[i] = transform;
            }

            //
            // Update past trajectory
            //

            for (int i = deltaSpaceTrajectory.Length - 1; i > 0; --i)
            {
                deltaSpaceTrajectory[i] = deltaSpaceTrajectory[i - 1];
            }

            var accumulatedTransform = deltaSpaceTrajectory[1].transform * deltaTransform;
            accumulatedTransform.q = math.normalize(accumulatedTransform.q);

            deltaSpaceTrajectory[0] =
                AccumulatedTransform.Create(
                    accumulatedTransform, deltaTime);

            var referenceTransform = deltaSpaceTrajectory[0].transform;

            float sampleTimeInSeconds = 0.0f;

            float inverseSampleRate = Missing.recip(Binary.SampleRate);

            int numTransforms = deltaSpaceTrajectory.Length;

            var sampler = SubSampler.Create();

            for (int i = halfTrajectoryLength - 1; i >= 0; --i)
            {
                sampleTimeInSeconds += inverseSampleRate;

                var deltaTransformAtSampleRate =
                    sampler.SampleAt(deltaSpaceTrajectory,
                        sampleTimeInSeconds);

                trajectory[i] =
                    referenceTransform.inverseTimes(
                        deltaTransformAtSampleRate);
            }
        }

        /// <summary>
        /// Adjust the past trajectory so that trajectory points conserve their world transform after the root transform
        /// has been modified.
        /// </summary>
        /// <param name="rootDeltaTransform"></param>
        public void KeepPastTrajectoryInWorldSpace(AffineTransform rootDeltaTransform)
        {
            int trajectoryLength = TrajectoryLength;
            int halfTrajectoryLength = trajectoryLength / 2;

            AffineTransform invDelta = rootDeltaTransform.inverse();

            for (int i = 0; i < halfTrajectoryLength - 1; ++i)
            {
                trajectory[i] = invDelta * trajectory[i];
            }

            var delta = deltaSpaceTrajectory[0];

            delta.transform *= rootDeltaTransform;
            delta.transform.q = math.normalize(delta.transform.q);

            deltaSpaceTrajectory[0] = delta;
        }

        internal void WriteToStream(Buffer buffer)
        {
            trajectory.WriteToStream(buffer);
            deltaSpaceTrajectory.WriteToStream(buffer);
        }

        internal void ReadFromStream(Buffer buffer)
        {
            trajectory.ReadFromStream(buffer);
            deltaSpaceTrajectory.ReadFromStream(buffer);
        }

        internal static int DurationToFrames(float durationInSeconds, float samplesPerSecond)
        {
            return Missing.truncToInt(
                durationInSeconds * samplesPerSecond) + 1;
        }

        /// <summary>
        /// Denotes the length of the trajectory.
        /// </summary>
        public int TrajectoryLength
        {
            get
            {
                float sampleRate = Binary.SampleRate;
                float timeHorizon = Binary.TimeHorizon;

                return DurationToFrames(timeHorizon * 2.0f, sampleRate);
            }
        }

        /// <summary>
        /// Allows access to a single element of the trajectory.
        /// </summary>
        public AffineTransform this[int index]
        {
            get
            {
                Assert.IsTrue(index >= 0);
                Assert.IsTrue(index < TrajectoryLength);
                return trajectory[index];
            }
            set
            {
                trajectory[index] = value;
            }
        }

        /// <summary>
        /// Allows direct access to the trajectory array.
        /// </summary>
        /// <returns>Memory array that references the trajectory array.</returns>
        public NativeSlice<AffineTransform> Array
        {
            get
            {
                Assert.IsTrue(trajectory.Length == TrajectoryLength);
                return trajectory;
            }
        }

        /// <summary>
        /// Calculates a root transform along the current trajectory
        /// at a sampling time in seconds passed as argument.
        /// </summary>
        /// <param name="sampleTimeInSeconds">The sampling time in seconds.</param>
        /// <returns>Memory array that references the trajectory array.</returns>
        public AffineTransform RootTransformAtTime(float sampleTimeInSeconds)
        {
            return Utility.SampleTrajectoryAtTime(trajectory, sampleTimeInSeconds, Binary.TimeHorizon);
        }

        /// <summary>
        /// Calculates the root velocity along the current trajectory
        /// at a sampling time in seconds passed as argument.
        /// </summary>
        /// <param name="sampleTimeInSeconds">The sampling time in seconds.</param>
        /// <param name="timeHorizon">Time horizon in seconds over which the velocity should be approximated.</param>
        /// <returns>Velocity in meters per second subject to the sampling time passed as argument.</returns>
        public float3 GetRootVelocity(float sampleTimeInSeconds, float timeHorizon)
        {
            AffineTransform referenceTransform =
                RootTransformAtTime(sampleTimeInSeconds);

            float deltaTime = 1.0f / Binary.SampleRate;

            float3 result = Missing.zero;
            int numSamples = 0;

            sampleTimeInSeconds = math.clamp(
                sampleTimeInSeconds, -Binary.TimeHorizon,
                Binary.TimeHorizon);

            timeHorizon = math.max(deltaTime, timeHorizon);

            sampleTimeInSeconds -= timeHorizon * 0.5f;

            sampleTimeInSeconds = math.clamp(
                sampleTimeInSeconds, -Binary.TimeHorizon,
                Binary.TimeHorizon - timeHorizon);

            float3 previousPosition =
                referenceTransform.inverseTimes(
                    RootTransformAtTime(sampleTimeInSeconds)).t;

            float remainingTimeInSeconds = timeHorizon;
            while (remainingTimeInSeconds > 0.0f)
            {
                float timeInSecondsToConsume =
                    math.min(deltaTime, remainingTimeInSeconds);

                sampleTimeInSeconds += timeInSecondsToConsume;
                remainingTimeInSeconds -= timeInSecondsToConsume;

                AffineTransform localTransform =
                    referenceTransform.inverseTimes(
                        RootTransformAtTime(sampleTimeInSeconds));

                float3 currentPosition = localTransform.t;

                float3 displacement =
                    currentPosition - previousPosition;

                result += displacement / timeInSecondsToConsume;

                numSamples++;

                previousPosition = currentPosition;
            }

            return result / numSamples;
        }

        /// <summary>
        /// Displays the trajectory model in world space.
        /// </summary>
        /// <param name="worldRootTransform">Anchor transform to be used as a reference for the trajectory display.</param>
        /// <param name="color">The color to be used for the trajectory display.</param>
        public void Display(AffineTransform worldRootTransform, Color color)
        {
            Color fillColor = color;
            Color linesAndContourColor = color;

            linesAndContourColor.a = 0.9f;

            int trajectoryLength = TrajectoryLength;

            Binary.DebugDrawTransform(worldRootTransform, 0.1f);

            var previousRootTransform = worldRootTransform * this[0];

            for (int i = 1; i < trajectoryLength; ++i)
            {
                var currentRootTransform =
                    worldRootTransform * this[i];

                DebugDraw.DrawLine(previousRootTransform.t,
                    currentRootTransform.t, color);

                previousRootTransform = currentRootTransform;
            }

            void DrawArrow(AffineTransform transform)
            {
                DebugDraw.DrawArrow(
                    worldRootTransform * transform,
                    fillColor, linesAndContourColor, 0.25f);
            }

            int stepSize = Missing.truncToInt(Binary.SampleRate / 3);
            int numSteps = trajectoryLength / stepSize;
            for (int i = 0; i < numSteps; ++i)
            {
                DrawArrow(this[i * stepSize]);
            }

            DrawArrow(this[trajectoryLength - 1]);
        }

        private ref Binary Binary
        {
            get { return ref binary.Ref; }
        }

        //
        // Private attributes
        //

        [ReadOnly]
        private MemoryRef<Binary> binary;

        private NativeSlice<AffineTransform> trajectory;

        struct SubSampler
        {
            public int index;
            public float accumulatedTime;

            public static SubSampler Create()
            {
                return new SubSampler();
            }

            public AffineTransform SampleAt(NativeSlice<AccumulatedTransform> trajectory, float sampleTimeInSeconds)
            {
                int numTransforms = trajectory.Length;

                while (index < numTransforms - 1)
                {
                    float accumulatedTimePlusDeltaTime =
                        accumulatedTime + trajectory[index].deltaTime;

                    if (sampleTimeInSeconds <= accumulatedTimePlusDeltaTime)
                    {
                        float remainingTime = sampleTimeInSeconds - accumulatedTime;

                        if (trajectory[index].deltaTime < 0.0001f)
                        {
                            Assert.IsTrue(true);
                        }

                        float theta = math.clamp(remainingTime / trajectory[index].deltaTime, 0.0f, 1.0f);

                        return Missing.lerp(
                            trajectory[index].transform,
                            trajectory[index + 1].transform, theta);
                    }

                    index++;

                    accumulatedTime = accumulatedTimePlusDeltaTime;
                }

                return trajectory[numTransforms - 1].transform;
            }
        }

        struct AccumulatedTransform
        {
            public AffineTransform transform;
            public float deltaTime;

            public static AccumulatedTransform identity
            {
                get
                {
                    return new AccumulatedTransform
                    {
                        transform = AffineTransform.identity,
                        deltaTime = 0.0f
                    };
                }
            }

            public static AccumulatedTransform Create(AffineTransform transform, float deltaTime)
            {
                return new AccumulatedTransform
                {
                    transform = transform,
                    deltaTime = deltaTime
                };
            }
        }

        private NativeSlice<AccumulatedTransform> deltaSpaceTrajectory;
    }
}
