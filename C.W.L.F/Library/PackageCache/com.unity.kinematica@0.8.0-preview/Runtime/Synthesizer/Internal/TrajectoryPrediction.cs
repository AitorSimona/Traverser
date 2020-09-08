using Unity.Mathematics;
using Unity.Kinematica.Supplementary;
using UnityEngine.Assertions;
using Unity.Collections;

namespace Unity.Kinematica
{
    /// <summary>
    /// Allows the generation of a desired future trajecory.
    /// </summary>
    /// <remarks>
    /// Allows the generation of a desired future trajecory by
    /// predicting the future root movement over time subject
    /// to a number of characteristics.
    /// <example>
    /// <code>
    /// var prediction = TrajectoryPrediction.Create(
    ///     ref synthesizer, desiredVelocity, desiredRotation,
    ///         trajectory, velocityPercentage, forwardPercentage);
    /// prediction.Generate();
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="TrajectoryPredictionTask"/>
    public struct TrajectoryPrediction
    {
        public static TrajectoryPrediction CreateFromDirection(ref MotionSynthesizer synthesizer, float3 desiredDirection, float desiredSpeed, Trajectory trajectory, float velocityFactor, float rotationFactor)
        {
            float3 desiredVelocity = desiredDirection * desiredSpeed;
            quaternion desiredRotation = Missing.forRotation(Missing.forward, desiredDirection);

            return Create(ref synthesizer, desiredVelocity, desiredRotation, trajectory, velocityFactor, rotationFactor);
        }

        /// <summary>
        /// Creates a trajectory prediction instance.
        /// </summary>
        /// <param name="synthesizer">Reference to the motion synthesizer to generate the trajectory for.</param>
        /// <param name="desiredLinearVelocity">Desired linear velocity in meters per second in character space.</param>
        /// <param name="desiredRotation">Desired orientation in character space.</param>
        /// <param name="trajectory">Memory array that the generated trajectory should be written to.</param>
        /// <param name="velocityFactor">Factor that determines when the desired velocity w.r.t. the time horizon should be reached.</param>
        /// <param name="rotationFactor">Factor that determines when the desired rotation w.r.t. the time horizon should be reached.</param>
        /// <returns>The newly created trajectory prediction instance.</returns>
        public static TrajectoryPrediction Create(ref MotionSynthesizer synthesizer, float3 desiredLinearVelocity, quaternion desiredRotation, Trajectory trajectory, float velocityFactor, float rotationFactor)
        {
            return TrajectoryPrediction.Create(ref synthesizer, desiredLinearVelocity, desiredRotation, trajectory, velocityFactor, rotationFactor, synthesizer.CurrentVelocity);
        }

        /// <summary>
        /// Creates a trajectory prediction instance.
        /// </summary>
        /// <param name="synthesizer">Reference to the motion synthesizer to generate the trajectory for.</param>
        /// <param name="desiredLinearVelocity">Desired linear velocity in meters per second in character space.</param>
        /// <param name="desiredRotation">Desired orientation in character space.</param>
        /// <param name="trajectory">Memory array that the generated trajectory should be written to.</param>
        /// <param name="velocityFactor">Factor that determines when the desired velocity w.r.t. the time horizon should be reached.</param>
        /// <param name="rotationFactor">Factor that determines when the desired rotation w.r.t. the time horizon should be reached.</param>
        /// <param name="currentLinearVelocity">Current linear velocity in meters per second in character space</param>
        /// <returns>The newly created trajectory prediction instance.</returns>
        public static TrajectoryPrediction Create(ref MotionSynthesizer synthesizer, float3 desiredLinearVelocity, quaternion desiredRotation, Trajectory trajectory, float velocityFactor, float rotationFactor, float3 currentLinearVelocity)
        {
            ref var binary = ref synthesizer.Binary;

            synthesizer.ClearTrajectory(trajectory);

            float sampleRate = binary.SampleRate;

            var worldRootTransform = synthesizer.WorldRootTransform;

            // Desired velocity in m/s in character space
            desiredLinearVelocity = Missing.rotateVector(
                Missing.conjugate(worldRootTransform.q),
                desiredLinearVelocity);

            // Desired rotation in character space
            desiredRotation =
                math.mul(Missing.conjugate(worldRootTransform.q),
                    desiredRotation);

            return new TrajectoryPrediction
            {
                sampleRate = sampleRate,

                velocityFactor = velocityFactor,
                rotationFactor = rotationFactor,

                desiredLinearVelocity = desiredLinearVelocity,
                desiredRotation = desiredRotation,

                forward = SmoothValue.Create(0.0f),
                linearVelocity = SmoothValue3.Create(currentLinearVelocity),

                rootTransform = AffineTransform.identity,

                currentIndex = 0,

                trajectory = trajectory
            };
        }

        /// <summary>
        /// Generates the next root transform of the trajectory.
        /// </summary>
        /// <remarks>
        /// The trajectory prediction generates samples along the trajectory
        /// which are subject to the time horizon and sample rate that are
        /// configured in the runtime asset.
        /// <para>
        /// The generated root transform can be modified before
        /// storing it in the trajectory buffer. A use case for this is
        /// for example collision detection and resolution.
        /// </para>
        /// </remarks>
        /// <seealso cref="Binary.TimeHorizon"/>
        /// <seealso cref="Binary.SampleRate"/>
        /// <seealso cref="Push"/>
        public AffineTransform Advance
        {
            get
            {
                int trajectoryLength = trajectory.Length;
                int halfTrajectoryLength = trajectoryLength / 2;

                if (currentIndex <= halfTrajectoryLength)
                {
                    float deltaTime = 1 / (float)halfTrajectoryLength;

                    float elapsedTime = ++currentIndex / (float)halfTrajectoryLength;

                    float3 velocity =
                        linearVelocity.ExponentialDecay(
                            desiredLinearVelocity, deltaTime,
                            math.max(0.0f, velocityFactor - elapsedTime));

                    rootTransform.t += velocity / sampleRate;

                    float weight =
                        forward.ExponentialDecay(1.0f,
                            deltaTime, math.max(0.0f,
                                rotationFactor - elapsedTime));

                    rootTransform.q = math.slerp(quaternion.identity,
                        desiredRotation, weight);
                }

                return rootTransform;
            }
        }

        /// <summary>
        /// Allows to get and set the current root transform during
        /// the trajectory generate.
        /// </summary>
        public AffineTransform Transform
        {
            get { return rootTransform; }
            set { rootTransform = value; }
        }

        /// <summary>
        /// Convenience method that generates the entire trajectory
        /// in a single call. It can be used instead of manually
        /// calling Advance() and Push() in case no root transform
        /// modification is required.
        /// </summary>
        public void Generate()
        {
            var transform = rootTransform;

            while (Push(transform))
            {
                transform = Advance;
            }
        }

        /// <summary>
        /// Stores the root transform passed as argument as the next
        /// trajectory element in the internal buffer. This method
        /// can be used in conjunction with Advance().
        /// </summary>
        /// <param name="transform">Root transform to be stored in the trajectory buffer.</param>
        public bool Push(AffineTransform transform)
        {
            int trajectoryLength = trajectory.Length;
            int halfTrajectoryLength = trajectoryLength / 2;

            int index = halfTrajectoryLength + currentIndex;

            if (index < trajectoryLength)
            {
                trajectory[index] = transform;

                return true;
            }

            return false;
        }

        float sampleRate;

        float velocityFactor;
        float rotationFactor;

        float3 desiredLinearVelocity;
        quaternion desiredRotation;

        SmoothValue forward;
        SmoothValue3 linearVelocity;

        AffineTransform rootTransform;

        int currentIndex;

        Trajectory trajectory;
    }
}
