using Unity.Mathematics;

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
    }
}
