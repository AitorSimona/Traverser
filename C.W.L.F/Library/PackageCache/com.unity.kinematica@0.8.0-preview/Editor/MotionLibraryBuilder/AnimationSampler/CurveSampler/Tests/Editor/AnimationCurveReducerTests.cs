using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class AnimationCurveReducerTests
    {
        [Test]
        public void ConstantCurve_Reduction_ProducesZeroError()
        {
            var curve = AnimationCurve.Constant(0, 1, 2);
            var reducedCurve = new AnimationCurve(curve.keys);
            float error = AnimationCurveReducer.ReduceWithMaximumAbsoluteError(reducedCurve, 2.0f);

            Assert.Zero(error);
            Assert.AreEqual(curve.length, reducedCurve.length);
            Assert.AreEqual(curve.keys[0], reducedCurve.keys[0]);
            Assert.AreEqual(curve.keys[1], reducedCurve.keys[1]);
        }

        [Test]
        public void Baked_LinearCurve_Reduction_ProducesZeroError()
        {
            var curve = AnimationCurve.Linear(0, 0, 2, 2);
            var keys = curve.keys;
            var reducedCurve = new AnimationCurve(keys);
            AnimationCurveBake.Bake(reducedCurve, 30, AnimationCurveBake.InterpolationMode.Linear);

            float error = AnimationCurveReducer.ReduceWithMaximumAbsoluteError(reducedCurve, 0.01f);

            Assert.Zero(error);
            Assert.AreEqual(curve.length, reducedCurve.length);
        }
    }
}
