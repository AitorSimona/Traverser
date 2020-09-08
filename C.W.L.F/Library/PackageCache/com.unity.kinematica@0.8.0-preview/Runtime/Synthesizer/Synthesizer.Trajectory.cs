using Unity.Collections;
using Unity.Mathematics;
namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        /// <summary>
        /// Denotes the world space root transform of the character.
        /// </summary>
        public AffineTransform WorldRootTransform
        {
            get { return rootTransform; }
            set { rootTransform = value; }
        }

        /// <summary>
        /// Allows for non-continuous root displacements.
        /// </summary>
        /// <remarks>
        /// This method can be used to teleport the character
        /// by utilizing a displacement transform.
        /// </remarks>
        /// <param name="deltaTransform">Delta transform to be applied to the character root transform.</param>
        /// <param name="bKeepPastTrajectoryInWorldSpace">If true, past trajectory will stay at the same location in world space, otherwise it will follow the root transform</param>
        public void AdjustTrajectory(AffineTransform deltaTransform, bool bKeepPastTrajectoryInWorldSpace = false)
        {
            rootTransform *= deltaTransform;
            rootTransform.q = math.normalize(rootTransform.q);

            if (bKeepPastTrajectoryInWorldSpace)
            {
                trajectory.KeepPastTrajectoryInWorldSpace(deltaTransform);
            }
        }

        /// <summary>
        /// Teleport character to a target transform
        /// </summary>
        /// <remarks>
        /// This method can be used to teleport the character
        /// by providing a target world transform
        /// </remarks>
        /// <param name="transform">Target transform where the character should be teleported.</param>
        /// <param name="bKeepPastTrajectoryInWorldSpace">If true, past trajectory will stay at the same location in world space, otherwise it will follow the root transform</param>
        public void SetWorldTransform(AffineTransform targetTransform, bool bKeepPastTrajectoryInWorldSpace = false)
        {
            AffineTransform deltaTransform = rootTransform.inverseTimes(targetTransform);
            AdjustTrajectory(deltaTransform, bKeepPastTrajectoryInWorldSpace);
        }

        /// <summary>
        /// Returns the interpolated transform between the root delta transform from Kinematica binary for the current frame, and the desired delta transform.
        /// </summary>
        /// <param name="desiredTrajectory">Desired trajectory that will be sampled from time 0 to <code>_deltaTime</code> in order to compute the desired delta transform</param>
        /// <param name="translationWeight">Interpolation weight between the root delta position at 0, and the desired delta position at 1</param>
        /// <param name="rotationWeight">Interpolation weight between the root delta rotation at 0, and the desired delta rotation at 1</param>
        /// <param name="startSpeed">Root speed (m/s) at which steering will start to be effective, steering weight will then increase linearly as root speed increases toward <code>endSpeed</code></param>
        /// <param name="endSpeed">Root speed (m/s) at which steering will be fully effective</param>
        /// <returns></returns>
        public AffineTransform SteerRootMotion(Trajectory desiredTrajectory, float translationWeight, float rotationWeight, float startSpeed = 0.0f, float endSpeed = 0.15f)
        {
            if (_deltaTime <= 0.0f)
            {
                return AffineTransform.identity;
            }

            AffineTransform binaryRootDelta = rootDeltaTransform;
            AffineTransform desiredRootDelta = Utility.SampleTrajectoryAtTime(desiredTrajectory, _deltaTime, Binary.TimeHorizon);

            return Utility.SteerRootMotion(binaryRootDelta, desiredRootDelta, _deltaTime, translationWeight, rotationWeight, startSpeed, endSpeed);
        }

        /// <summary>
        /// Allows access to the trajectory of the motion synthesizer.
        /// </summary>
        /// <remarks>
        /// The trajectory consists out of a series of transforms
        /// that correspond to the transform of the root joint over time.
        /// <para>
        /// The sampling frequency and length of the trajectory is defined by
        /// the sampling rate and time horizon respectively. Both parameters
        /// are defined as a global parameter in the runtime asset.
        /// </para>
        /// <para>
        /// The trajectory covers a duration which is twice as long as
        /// the time horizon. The first half of the trajectory represents
        /// the past movement of the character, whereas the second half
        /// represents the future movement of the character.
        /// </para>
        /// <para>
        /// All transforms stored in the trajectory are always relative
        /// to the current character root transform, i.e. the trajectory
        /// is maintained in character space.
        /// </para>
        /// </remarks>
        public NativeSlice<AffineTransform> TrajectoryArray => trajectory.Array;

        public Trajectory CreateTrajectory(Allocator allocator)
        {
            return Trajectory.Create(TrajectoryArray.Length, allocator);
        }

        public void ClearTrajectory(NativeSlice<AffineTransform> trajectory)
        {
            trajectory.CopyFrom(TrajectoryArray);

            int halfTrajectoryLength = TrajectoryArray.Length / 2;
            for (int i = halfTrajectoryLength; i < trajectory.Length; ++i)
            {
                trajectory[i] = AffineTransform.identity;
            }
        }

        internal AffineTransform DesiredTargetWorldTransform
        {
            get
            {
                return trajectory.RootTransformAtTime(Binary.TimeHorizon);
            }
        }

        internal float3 DesiredTargetVelocity
        {
            get
            {
                float timeHorizon = 3.0f / Binary.SampleRate;
                return Missing.rotateVector(WorldRootTransform.q,
                    trajectory.GetRootVelocity(
                        Binary.TimeHorizon, timeHorizon));
            }
        }

        /// <summary>
        /// Velocity from binary at current sampling time, in meters per second, in character space
        /// </summary>
        public float3 CurrentVelocity
        {
            get
            {
                float sampleRate = Binary.SampleRate;
                float inverseSampleRate = 1.0f / sampleRate;
                AffineTransform transform =
                    GetTrajectoryDeltaTransform(
                        inverseSampleRate);
                return transform.t * sampleRate;
            }
        }

        /// <summary>
        /// Calculates the curent root displacement transform.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        /// <returns>Root displacement transform at the current sampling time.</returns>
        public AffineTransform GetTrajectoryDeltaTransform(float deltaTime)
        {
            if (samplingTime.IsValid)
            {
                return
                    Binary.GetTrajectoryTransformBetween(
                    Time, deltaTime);
            }

            return AffineTransform.identity;
        }
    }
}
