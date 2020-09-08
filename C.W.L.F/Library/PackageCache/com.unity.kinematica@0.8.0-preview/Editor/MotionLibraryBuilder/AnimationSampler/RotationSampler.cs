using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal struct RotationSampler
    {
        public FloatSampler x;
        public FloatSampler y;
        public FloatSampler z;
        public FloatSampler w;

        public const int NumCurves = 4;

        public static RotationSampler CreateEmpty(quaternion defaultRotation)
        {
            return new RotationSampler()
            {
                x = FloatSampler.CreateEmpty(defaultRotation.value.x),
                y = FloatSampler.CreateEmpty(defaultRotation.value.y),
                z = FloatSampler.CreateEmpty(defaultRotation.value.z),
                w = FloatSampler.CreateEmpty(defaultRotation.value.w)
            };
        }

        public quaternion DefaultValue => new quaternion(x.DefaultValue, y.DefaultValue, z.DefaultValue, w.DefaultValue);

        public quaternion this[int index] => new quaternion(x[index], y[index], z[index], w[index]);

        public quaternion Evaluate(float sampleTimeInSeconds)
        {
            return new quaternion(
                x.Evaluate(sampleTimeInSeconds),
                y.Evaluate(sampleTimeInSeconds),
                z.Evaluate(sampleTimeInSeconds),
                w.Evaluate(sampleTimeInSeconds));
        }
    }
}
