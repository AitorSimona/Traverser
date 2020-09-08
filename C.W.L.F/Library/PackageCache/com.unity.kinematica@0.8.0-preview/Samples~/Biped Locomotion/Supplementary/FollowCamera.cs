using Unity.Mathematics;
using UnityEngine;
using Unity.Kinematica;

namespace BipedLocomotion
{
    public class FollowCamera : MonoBehaviour
    {
        //
        // Target transform to be tracked
        //

        public Transform targetTransform;

        //
        // Offset to be maintained between camera and target
        //

        private float3 offset;

        [Range(0.01f, 1.0f)]
        public float smoothFactor = 0.5f;

        public float degreesPerSecond = 180.0f;

        public float maximumYawAngle = 45.0f;

        public float minimumHeight = 0.2f;

        public float heightOffset = 1.0f;

        void Start()
        {
            offset = Convert(transform.position) - TargetPosition;
        }

        void LateUpdate()
        {
            float radiansPerSecond = math.radians(degreesPerSecond);

            float horizontal = InputUtility.GetCameraHorizontalInput();
            float vertical = InputUtility.GetCameraVerticalInput();

            if (math.abs(horizontal) >= 0.2f)
            {
                RotateOffset(Time.deltaTime * horizontal * radiansPerSecond, Vector3.up);
            }

            if (math.abs(vertical) >= 0.2f)
            {
                float angleAt = math.abs(math.asin(transform.forward.y));
                float maximumAngle = math.radians(maximumYawAngle);
                float angleDeltaDesired = Time.deltaTime * vertical * radiansPerSecond;
                float angleDeltaClamped =
                    CalculateAngleDelta(angleDeltaDesired,
                        maximumAngle - angleAt);

                RotateOffset(angleDeltaClamped, transform.right);
            }

            Vector3 cameraPosition = TargetPosition + offset;

            if (cameraPosition.y <= minimumHeight)
            {
                cameraPosition.y = minimumHeight;
            }

            transform.position = Vector3.Slerp(transform.position, cameraPosition, smoothFactor);

            transform.LookAt(TargetPosition);
        }

        private float CalculateAngleDelta(float angleDeltaDesired, float angleRemaining)
        {
            if (math.dot(transform.forward, Missing.up) >= 0.0f)
            {
                return -math.min(-angleDeltaDesired, angleRemaining);
            }
            else
            {
                return math.min(angleDeltaDesired, angleRemaining);
            }
        }

        private void RotateOffset(float angleInRadians, float3 axis)
        {
            offset = math.mul(quaternion.AxisAngle(axis, angleInRadians), offset);
        }

        private static float3 Convert(Vector3 p)
        {
            return p;
        }

        private float3 TargetPosition
        {
            get { return Convert(targetTransform.position) + new float3(0.0f, heightOffset, 0.0f); }
        }
    }
}
