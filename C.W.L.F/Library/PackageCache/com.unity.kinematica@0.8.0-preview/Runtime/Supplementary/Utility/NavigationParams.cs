using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    /// <summary>
    /// Parameters of navigation query asked to Kinematica in order to move an agent along a path made of control points
    /// </summary>
    public struct NavigationParams
    {
        /// <summary>
        /// Agent will try to reach <c>desiredSpeed</c> over time while walking through the path
        /// </summary>
        public float desiredSpeed;

        /// <summary>
        /// Max agent speed when crossing a control point at a 90 degrees angle. Angle at a control point n is computed as the angle between segment from control point n-1
        /// to control point n and segment from control point n to control point n+1.
        /// If corner angle is bigger than 90 degrees, <c>maxSpeedAtRightAngle</c> will still be used as max speed.
        /// If corner angle is smaller than 90 degrees, max speed will be interpolated in function of corner angle between <c>maxSpeedAtRightAngle</c> at 90 degrees and
        /// current speed at 0 degree.
        /// </summary>
        public float maxSpeedAtRightAngle;

        /// <summary>
        /// Maximum acceleration to increase agent speed toward <c>desiredSpeed</c>
        /// </summary>
        public float maximumAcceleration;

        /// <summary>
        /// Maximum deceleration to decrease current speed (absolute value of a negative acceleration). Use a small value so that agent start slowing down from a long distance
        /// to stop smoothly at target.
        /// </summary>
        public float maximumDeceleration;

        /// <summary>
        /// When agent distance from current control point become lower or equal to <c>intermediateControlPointRadius</c>, agent is considered to have reached control point and will
        /// start moving to the next control point. This doesn't apply to the last control point
        /// </summary>
        public float intermediateControlPointRadius;

        /// <summary>
        /// When agent distance from last control point become lower or equal to <c>finalControlPointRadius</c>, agent is considered to have reached target and navigation will stop.
        /// </summary>
        public float finalControlPointRadius;

        /// <summary>
        /// Approximate arc length (in meters) of each curvature of the path. A value of 0 means the agent will move in straight lines between each control point.
        /// </summary>
        public float pathCurvature;

        public static float ComputeAccelerationToReachSpeed(float targetSpeed, float distance)
        {
            return ComputeAccelerationToReachSpeed(0.0f, targetSpeed, distance);
        }

        public static float ComputeAccelerationToReachSpeed(float startSpeed, float targetSpeed, float distance)
        {
            Assert.IsTrue(distance > 0.0f);
            return ((targetSpeed - startSpeed) * (targetSpeed + startSpeed) * 0.5f) / distance;
        }

        public static float ComputeDistanceToReachSpeed(float startSpeed, float targetSpeed, float acceleration)
        {
            Assert.IsTrue(acceleration != 0.0f);
            return ((targetSpeed - startSpeed) * (targetSpeed + startSpeed) * 0.5f) / acceleration;
        }
    }
}
