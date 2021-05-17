using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    // --- Wrapper that holds a position and a rotation, to ease handling of transformations ---
    public struct TraverserTransform
    {
        public float3 t;
        public Quaternion q;

        public TraverserTransform(float3 t_, Quaternion q_)
        {
            t = t_;
            q = q_;
        }

        public static TraverserTransform Create(float3 t_, Quaternion q_)
        {
            TraverserTransform affineTransform = new TraverserTransform();
            affineTransform.t = t_;
            affineTransform.q = q_;
            return affineTransform;
        }

        public static TraverserTransform Get(float3 t_, Quaternion q_)
        {
            TraverserTransform affineTransform;
            affineTransform.t = t_;
            affineTransform.q = q_;
            return affineTransform;
        }

        // transforms point p from local to world                
        public float3 transform(float3 point)
        {
            Matrix4x4 m = Matrix4x4.TRS(t, q, Vector3.one);
            return m.MultiplyPoint3x4(point);
        }

        // transforms direction from local to world
        public float3 transformDirection(float3 direction)
        {
            Matrix4x4 m = Matrix4x4.TRS(t, q, Vector3.one);
            return m.MultiplyVector(direction);
        }

        // transforms direction from world to local
        public float3 inverseTransformDirection(float3 direction)
        {
            Matrix4x4 m = Matrix4x4.TRS(t, q, Vector3.one);
            return m.inverse.MultiplyVector(direction);
        }

        // transforms point p from world to local
        public float3 inverseTransform(float3 point)
        {
            Matrix4x4 m = Matrix4x4.TRS(t, q, Vector3.one);
            return m.inverse.MultiplyPoint3x4(point);
        }
    }
}
