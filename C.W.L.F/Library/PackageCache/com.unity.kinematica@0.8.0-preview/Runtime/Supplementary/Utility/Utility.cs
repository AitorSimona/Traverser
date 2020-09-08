using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    /// <summary>
    /// Static class that contains various extension methods.
    /// </summary>
    public static class Utility
    {
        public static void GetInputMove(ref float3 movementDirection, ref float intensity)
        {
#if UNITY_EDITOR
            if (Unity.SnapshotDebugger.Debugger.instance.rewind)
            {
                return;
            }
#endif

            var horizontal = InputUtility.GetMoveHorizontalInput();
            var vertical = InputUtility.GetMoveVerticalInput();

            float3 analogInput = GetAnalogInput(horizontal, vertical);

            intensity = math.length(analogInput);

            if (intensity <= 0.1f)
            {
                intensity = 0.0f;
            }
            else
            {
                movementDirection = GetDesiredForwardDirection(analogInput, movementDirection);
            }
        }

        /// <summary>
        /// Converts a world space vector into a view-direction relative vector.
        /// </summary>
        /// <remarks>
        /// For a third-person controller it is common to convert a 3d-vector into
        /// a representation that is relative to a camera. This method takes as input
        /// a normalized view direction (e.g. the forward vector of a camera transform)
        /// and converts a vector passed as argument into a vector that is relative to
        /// the view direction.
        /// </remarks>
        /// <param name="absoluteLinearVelocity">The world space vector to be converted.</param>
        /// <param name="normalizedViewDirection">The normalized view direction.</param>
        /// <returns>The converted view-direction relative vector.</returns>
        public static float3 GetRelativeLinearVelocity(float3 absoluteLinearVelocity, float3 normalizedViewDirection)
        {
            float3 forward2d = math.normalize(
                new float3(normalizedViewDirection.x, 0.0f,
                    normalizedViewDirection.z));

            quaternion cameraRotation =
                Missing.forRotation(Missing.forward, forward2d);

            return
                Missing.rotateVector(
                cameraRotation, absoluteLinearVelocity);
        }

        /// <summary>
        /// Converts a world space vector into a relative vector w.r.t. the current main camera.
        /// </summary>
        /// <param name="absoluteLinearVelocity">The world space vector to be converted.</param>
        /// <param name="forwardDirection">The default forward direction, used if the linear velocity is zero.</param>
        /// <returns>The converted view-direction relative vector.</returns>
        public static float3 GetDesiredForwardDirection(float3 absoluteLinearVelocity, float3 forwardDirection)
        {
            var relativeDesiredVelocity =
                GetRelativeLinearVelocity(
                    absoluteLinearVelocity,
                    Camera.main.transform.forward);

            return math.normalizesafe(
                relativeDesiredVelocity, forwardDirection);
        }

        /// <summary>
        /// Converts a world space vector into a relative vector.
        /// </summary>
        /// <param name="absoluteLinearVelocity">The world space vector to be converted.</param>
        /// <param name="forwardDirection">The default forward direction, used if the linear velocity is zero.</param>
        /// <param name="cameraForward">The view direction to be used for the conversion.</param>
        /// <returns>The converted view-direction relative vector.</returns>
        public static float3 GetDesiredForwardDirection(float3 absoluteLinearVelocity, float3 forwardDirection, float3 cameraForward)
        {
            var relativeDesiredVelocity =
                GetRelativeLinearVelocity(
                    absoluteLinearVelocity,
                    cameraForward);

            return math.normalizesafe(
                relativeDesiredVelocity, forwardDirection);
        }

        /// <summary>
        /// Combines two floating point values into a float2 value.
        /// </summary>
        /// <remarks>
        /// Normalizes the resulting float2 value if the magnitude is greater than 1.0.
        /// </remarks>
        /// <param name="x">The horizontal value; expected to be in the range -1 to 1.</param>
        /// <param name="y">The vertical value; expected to be in the range -1 to 1.</param>
        /// <returns>The corresponding float2 value.</returns>
        public static float3 GetAnalogInput(float x, float y)
        {
            var analogInput = new float3(x, 0.0f, y);

            if (math.length(analogInput) > 1.0f)
            {
                analogInput =
                    math.normalize(analogInput);
            }

            return analogInput;
        }

        public static AffineTransform SampleTrajectoryAtTime(NativeSlice<AffineTransform> trajectory, float sampleTimeInSeconds, float timeHorizon)
        {
            int trajectoryLength = trajectory.Length;

            float adjustedTimeInSeconds =
                timeHorizon + sampleTimeInSeconds;

            float sampleRate = timeHorizon > 0.0f ? (trajectoryLength - 1) / (2.0f * timeHorizon) : 0.0f;

            float fractionalKeyFrame =
                adjustedTimeInSeconds * sampleRate;

            int sampleKeyFrame =
                Missing.truncToInt(fractionalKeyFrame);

            if (sampleKeyFrame < 0)
            {
                return trajectory[0];
            }
            else if (sampleKeyFrame >= trajectoryLength - 1)
            {
                return trajectory[trajectoryLength - 1];
            }

            float theta = math.saturate(fractionalKeyFrame - sampleKeyFrame);

            if (theta <= Missing.epsilon)
            {
                return trajectory[sampleKeyFrame];
            }

            AffineTransform t0 = trajectory[sampleKeyFrame + 0];
            AffineTransform t1 = trajectory[sampleKeyFrame + 1];

            return Missing.lerp(t0, t1, theta);
        }

        /// <summary>
        /// Returns the interpolated transform between the current and desired root motion, taking into account individual translation and rotation weights
        /// </summary>
        /// <param name="rootMotion">Root motion to steer, represents a root delta transform for a frame</param>
        /// <param name="desiredRootMotion">Desired root motion toward which we steer the current root motion</param>
        /// <param name="deltaTime">Delta time associated to the root motion. That means root velocity for the frame is <code>rootMotion.t / deltaTime</code></param>
        /// <param name="translationWeight">Interpolation weight between the root delta position at 0, and the desired delta position at 1</param>
        /// <param name="rotationWeight">Interpolation weight between the root delta rotation at 0, and the desired delta rotation at 1</param>
        /// <param name="startSpeed">Root speed (m/s) at which steering will start to be effective, steering weight will then increase linearly as root speed increases toward <code>endSpeed</code></param>
        /// <param name="endSpeed">Root speed (m/s) at which steering will be fully effective</param>
        /// <returns></returns>
        public static AffineTransform SteerRootMotion(AffineTransform rootMotion, AffineTransform desiredRootMotion, float deltaTime, float translationWeight, float rotationWeight, float startSpeed = 0.0f, float endSpeed = 0.15f)
        {
            if (deltaTime <= 0.0f)
            {
                return AffineTransform.identity;
            }

            AffineTransform steeredRootMotion = rootMotion;

            float speed = math.length(desiredRootMotion.t / deltaTime);
            float weight;
            if (endSpeed > startSpeed)
            {
                weight = math.clamp((speed - startSpeed) / (endSpeed - startSpeed), 0.0f, 1.0f);
            }
            else
            {
                weight = speed >= startSpeed ? 1.0f : 0.0f;
            }

            quaternion steerRotation = math.mul(desiredRootMotion.q, math.conjugate(rootMotion.q));
            steerRotation = math.slerp(quaternion.identity, steerRotation, rotationWeight * weight);
            steeredRootMotion.t = math.rotate(steerRotation, steeredRootMotion.t);
            steeredRootMotion.q = math.mul(steerRotation, steeredRootMotion.q);

            steeredRootMotion.t = math.lerp(steeredRootMotion.t, desiredRootMotion.t, translationWeight * weight);

            return steeredRootMotion;
        }
    }
}
