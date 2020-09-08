using Unity.Collections;
using Unity.Curves;

namespace Unity.Kinematica.Editor
{
    internal struct FloatSampler
    {
        CurveData curveData;
        float defaultValue;

        public bool HasCurve => curveData.IsValid;

        public int Length => curveData.size;

        public float DefaultValue => defaultValue;

        public float this[int index]
        {
            get => HasCurve ? curveData[index].value : defaultValue;
        }

        public float Evaluate(float time)
        {
            return HasCurve ? curveData.ToCurve().Evaluate(time) : defaultValue;
        }

        public Curve SetCurve(Curve curve)
        {
            curveData = new CurveData(curve, Allocator.Persistent);
            return curve;
        }

        public static FloatSampler CreateEmpty(float defaultValue) => new FloatSampler()
        {
            curveData = CurveData.CreateInvalid(),
            defaultValue = defaultValue
        };
    }
}
