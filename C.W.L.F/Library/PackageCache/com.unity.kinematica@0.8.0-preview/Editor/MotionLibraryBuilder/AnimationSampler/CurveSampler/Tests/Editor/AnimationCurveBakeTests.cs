using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;

namespace Tests
{
    public class AnimationCurveBakeTests
    {
        public class Keyframes
        {
            [Test, Ignore("Not working")]
            public void Bake_Keyframes_ProducesExpectedNumberOfKeyframes()
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

                Assert.AreEqual(30 * 5 + 1, AnimationCurveBake.Bake(ref frames, 30));
            }

            [Test, Ignore("Not working")]
            public void Bake_Keyframes_IsAccurateOnKey()
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

                var newFrames = new Keyframe[6];
                frames.CopyTo(newFrames, 0);

                AnimationCurveBake.Bake(ref newFrames, 30);
                int seekIndex = 0;
                for (int i = 0; i < frames.Length; i++)
                {
                    float actual = KeyframeUtilities.Evaluate(frames[i].time, ref seekIndex, newFrames);
                    float expected = KeyframeUtilities.Evaluate(frames[i].time, ref i, frames);
                    Assert.AreEqual(expected, actual);
                }
            }

            [Test]
            public void Bake_Keyframes_IsAccurateOnBakedKeys()
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

                var newFrames = new Keyframe[6];
                frames.CopyTo(newFrames, 0);

                AnimationCurveBake.Bake(ref newFrames, 30);
                int seekIndex = 0;
                for (int i = 0; i < newFrames.Length; i++)
                {
                    float actual = KeyframeUtilities.Evaluate(newFrames[i].time, ref i, newFrames);
                    float expected = KeyframeUtilities.Evaluate(newFrames[i].time, ref seekIndex, frames);
                    Assert.AreEqual(expected, actual, 0.000001f);
                }
            }

            [Test]
            public void Bake_Keyframes_IsAccurateInBetweenKeys()
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

                var newFrames = new Keyframe[6];
                frames.CopyTo(newFrames, 0);

                AnimationCurveBake.Bake(ref newFrames, 30);
                int index1 = 0;
                int index2 = 0;

                float deltaTime = 5 / 900;
                for (int i = 0; i < 900; i++)
                {
                    float actual = KeyframeUtilities.Evaluate(i * deltaTime, ref index1, newFrames);
                    float expected = KeyframeUtilities.Evaluate(i * deltaTime, ref index2, frames);
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        public class AnimationCurves
        {
            [TestCase(AnimationCurveBake.InterpolationMode.Linear)]
            [TestCase(AnimationCurveBake.InterpolationMode.Auto)]
            [TestCase(AnimationCurveBake.InterpolationMode.ClampedAuto)]
            public void Bake_LinearCurve_IsAccurate(AnimationCurveBake.InterpolationMode mode)
            {
                float frameRate = 7;
                float sampleRate = 30;

                var sourceCurve = AnimationCurve.Linear(0, 0, 5, 5);
                var curveKeys = sourceCurve.keys;
                var destCurve = new AnimationCurve(curveKeys);

                float start = KeyframeUtilities.StartTime(curveKeys);
                float end = KeyframeUtilities.EndTime(curveKeys);
                float duration = end - start;

                AnimationCurveBake.Bake(destCurve, frameRate, mode);


                float delta = (end - start) / sampleRate;
                int numSamples = (int)math.ceil(duration * frameRate) + 1;
                for (int i = 0; i <= numSamples; i++)
                {
                    float time = math.clamp(start + i * delta, start, end);
                    float expected = sourceCurve.Evaluate(time);
                    float actual = destCurve.Evaluate(time);
                }
            }
        }
    }
}
