using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    // --- Wrapper that holds a position and a rotation, to ease handling of transformations ---
    public struct TraverserAffineTransform
    {
        public float3 t;
        public quaternion q;

        public TraverserAffineTransform(float3 t_, quaternion q_)
        {
            t = t_;
            q = q_;
        }

        public static TraverserAffineTransform Create(float3 t_, quaternion q_)
        {
            TraverserAffineTransform affineTransform;
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

        // transforms point p from world to local
        public float3 inverseTransform(float3 point)
        {
            Matrix4x4 m = Matrix4x4.TRS(t, q, Vector3.one);
            return m.inverse.MultiplyPoint3x4(point);
        }
    }
}
