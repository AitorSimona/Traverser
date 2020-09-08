using System;

using UnityEngine;

using Unity.Collections;
using Unity.Curves;
using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal struct TransformSampler
    {
        public PositionSampler positions;
        public RotationSampler rotations;

        public const int NumCurves = PositionSampler.NumCurves + RotationSampler.NumCurves;

        public static TransformSampler CreateEmpty(AffineTransform defaultTransform)
        {
            return new TransformSampler()
            {
                positions = PositionSampler.CreateEmpty(defaultTransform.t),
                rotations = RotationSampler.CreateEmpty(defaultTransform.q)
            };
        }

        public AffineTransform DefaultTransform => AffineTransform.Create(positions.DefaultPosition, rotations.DefaultValue);

        public AffineTransform this[int index] => AffineTransform.Create(positions[index], rotations[index]);

        public AffineTransform Evaluate(float sampleTimeInSeconds)
        {
            return AffineTransform.Create(
                positions.Evaluate(sampleTimeInSeconds),
                rotations.Evaluate(sampleTimeInSeconds));
        }

        public FloatSampler GetCurveProxy(int curveIndex)
        {
            switch (curveIndex)
            {
                case 0: return positions.x;
                case 1: return positions.y;
                case 2: return positions.z;
                case 3: return rotations.x;
                case 4: return rotations.y;
                case 5: return rotations.z;
                case 6: return rotations.w;
            }

            return FloatSampler.CreateEmpty(0.0f);
        }

        public void SetCurve(int curveIndex, Curve curve)
        {
            switch (curveIndex)
            {
                case 0: positions.x.SetCurve(curve); break;
                case 1: positions.y.SetCurve(curve); break;
                case 2: positions.z.SetCurve(curve); break;
                case 3: rotations.x.SetCurve(curve); break;
                case 4: rotations.y.SetCurve(curve); break;
                case 5: rotations.z.SetCurve(curve); break;
                case 6: rotations.w.SetCurve(curve); break;
            }
        }

        public Curve? MapEditorCurve(string curveName, string posCurvePrefix, string rotCurvePrefix, AnimationCurve curve, out int curveIndex)
        {
            curveIndex = -1;

            string[] curveNameStrings = curveName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (curveNameStrings.Length != 2)
            {
                return null;
            }

            string curvePrefix = curveNameStrings[0];
            string curvePostfix = curveNameStrings[1];

            if (curvePrefix == posCurvePrefix)
            {
                if (curvePostfix == "x")
                {
                    curveIndex = 0;
                    return positions.x.SetCurve(new Curve(curve, Allocator.Persistent));
                }
                else if (curvePostfix == "y")
                {
                    curveIndex = 1;
                    return positions.y.SetCurve(new Curve(curve, Allocator.Persistent));
                }
                else if (curvePostfix == "z")
                {
                    curveIndex = 2;
                    return positions.z.SetCurve(new Curve(curve, Allocator.Persistent));
                }
            }
            else if (curvePrefix == rotCurvePrefix)
            {
                if (curvePostfix == "x")
                {
                    curveIndex = 3;
                    return rotations.x.SetCurve(new Curve(curve, Allocator.Persistent));
                }
                else if (curvePostfix == "y")
                {
                    curveIndex = 4;
                    return rotations.y.SetCurve(new Curve(curve, Allocator.Persistent));
                }
                else if (curvePostfix == "z")
                {
                    curveIndex = 5;
                    return rotations.z.SetCurve(new Curve(curve, Allocator.Persistent));
                }
                else if (curvePostfix == "w")
                {
                    curveIndex = 6;
                    return rotations.w.SetCurve(new Curve(curve, Allocator.Persistent));
                }
            }

            return null;
        }
    }
}
