using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public struct Quantizer
        {
            public float minimum;
            public float range;

            public static Quantizer Create(float minimum, float range)
            {
                return new Quantizer
                {
                    minimum = minimum,
                    range = range
                };
            }

            public byte Encode(float value)
            {
                return (byte)(math.clamp((value - minimum) / range, 0.0f, 1.0f) * 255.0f);
            }
        }
    }
}
