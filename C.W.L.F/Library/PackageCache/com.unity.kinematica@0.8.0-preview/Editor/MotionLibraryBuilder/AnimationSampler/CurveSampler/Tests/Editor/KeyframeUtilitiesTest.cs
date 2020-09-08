using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using Unity.Collections;

namespace Tests
{
    public class KeyframeUtilitiesTests
    {
        public class Seek
        {
            [TestCase(0, 0, 0, "Seek, on key time, with expected index")]
            [TestCase(-1, 0, 0, "Seek, negative time, with expected index")]
            [TestCase(-1, 5, 0, "Seek, negative time, with last index")]
            [TestCase(-1, 100, 0, "Seek, negative time, with index out of range positive")]
            [TestCase(-1, -1, 0, "Seek, negative time, with index out of range negative")]
            [TestCase(0.5f, 1, 0, "Seek between first and second keyframes, from next keyframe")]
            [TestCase(0.5f, 5, 0, "Seek between first and second keyframes, from last keyframe")]
            [TestCase(4.5f, 6, 4, "Seek between second-to-last and last keyframes, from last keyframe")]
            [TestCase(4.5f, 4, 4, "Seek between second-to-last and last keyframes, from correct keyframe")]
            public void Seek_Backwards(float time, int index, int expected, string message)
            {
                var frames = new Keyframe[]
                {
                    new Keyframe(0, 0),
                    new Keyframe(1, 1),
                    new Keyframe(2, 2),
                    new Keyframe(3, 3),
                    new Keyframe(4, 4),
                    new Keyframe(5, 5)
                };

                Assert.AreEqual(expected, KeyframeUtilities.Seek(time, index, frames), message);
            }

            [TestCase(5, 5, 5, "Seek, on key time, with expected index")]
            [TestCase(100, 5, 5, "Seek, time out of range, with expected index")]
            [TestCase(100, 0, 5, "Seek, time out of range, with first index")]
            [TestCase(100, -1, 5, "Seek, time out of range, with index out of range negative")]
            [TestCase(100, 100, 5, "Seek, time out of range, with index out of range positive")]
            [TestCase(0.5f, 0, 0, "Seek between first and second keyframes, from start")]
            [TestCase(1.5f, 1, 1, "Seek between second and third keyframes, from correct keyframe")]
            [TestCase(4.5f, 0, 4, "Seek between ssecond-to-last and last keyframes, from start")]
            [TestCase(4.5f, 4, 4, "Seek between ssecond-to-last and last keyframes, from correct keyframe")]
            public void Seek_Forward(float time, int index, int expected, string message)
            {
                var frames = new Keyframe[]
                {
                    new Keyframe(0, 0),
                    new Keyframe(1, 1),
                    new Keyframe(2, 2),
                    new Keyframe(3, 3),
                    new Keyframe(4, 4),
                    new Keyframe(5, 5),
                };

                Assert.AreEqual(expected, KeyframeUtilities.Seek(time, index, frames), message);
            }
        }

        public class AlignTangents
        {
            [TestCase(0, 0, 0, 0)]
            [TestCase(0, 1, 1, 0)]
            [TestCase(0, 1, 1, 1)]
            public void AlignTangents_Linear_ProducesSame_LinearCurve(float start, float startValue, float end, float endValue)
            {
                var curve = AnimationCurve.Linear(start, startValue, end, endValue);

                var keys = new NativeArray<Keyframe>(30, Allocator.Temp);
                for (int i = 0; i < 30; i++)
                {
                    float time = 1 / 30 * i;
                    keys[i] = new Keyframe(time, curve.Evaluate(time));
                }

                KeyframeUtilities.AlignTangentsLinear(keys);

                var alignedCurve = new AnimationCurve(keys.ToArray());
                for (int i = 0; i < 60; i++)
                {
                    float time = 1 / 60 * i;
                    Assert.AreEqual(curve.Evaluate(time), alignedCurve.Evaluate(time));
                }

                keys.Dispose();
            }

            [TestCase(0, 0, 0, 0)]
            [TestCase(0, 1, 1, 0)]
            [TestCase(0, 1, 1, 1)]
            public void AlignTangents_Smooth_ProducesSame_LinearCurve(float start, float startValue, float end, float endValue)
            {
                var curve = AnimationCurve.Linear(start, startValue, end, endValue);

                var keys = new NativeArray<Keyframe>(30, Allocator.Temp);
                for (int i = 0; i < 30; i++)
                {
                    float time = 1 / 30 * i;
                    keys[i] = new Keyframe(time, curve.Evaluate(time));
                }

                KeyframeUtilities.AlignTangentsSmooth(keys);

                var alignedCurve = new AnimationCurve(keys.ToArray());
                for (int i = 0; i < 60; i++)
                {
                    float time = 1 / 60 * i;
                    Assert.AreEqual(curve.Evaluate(time), alignedCurve.Evaluate(time));
                }

                keys.Dispose();
            }
        }
    }
}
