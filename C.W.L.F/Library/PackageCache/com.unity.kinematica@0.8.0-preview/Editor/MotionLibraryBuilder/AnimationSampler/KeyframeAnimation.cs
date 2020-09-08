using System;

using UnityEngine;
using UnityEditor;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Curves;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Kinematica.Editor
{
    internal struct KeyframeAnimation : IDisposable
    {
        struct CurveInfo
        {
            public Curve curve;
            public int jointIndex;
            public int curveIndex;

            public int CompareTo(CurveInfo otherCurve)
            {
                int result = jointIndex.CompareTo(otherCurve.jointIndex);
                if (result == 0)
                {
                    result = curveIndex.CompareTo(otherCurve.curveIndex);
                }

                return result;
            }
        }

        List<CurveInfo> animationCurves; // curves from animation that are actually animated
        NativeArray<TransformSampler> jointSamplers; // will return the animated curves for that joint if any, or default rig transform
        float duration;
        int numFrames; // only set for fixed framerate anim

        public float Duration => duration;

        public Curve[] AnimationCurves => animationCurves.Select(c => c.curve).ToArray();

        public NativeArray<TransformSampler> JointSamplers => jointSamplers;

        public int NumFrames => numFrames;

        public static KeyframeAnimation Create(AnimationSampler animSampler, AnimationClip animationClip)
        {
            KeyframeAnimation anim = new KeyframeAnimation();
            anim.InitWithRigTransforms(animSampler.TargetRig);
            anim.duration = Utility.ComputeAccurateClipDuration(animationClip);
            anim.numFrames = 0;

            var bindings = AnimationUtility.GetCurveBindings(animationClip);

            foreach (EditorCurveBinding binding in bindings)
            {
                int jointIndex = animSampler.TargetRig.GetJointIndexFromPath(binding.path);

                if (jointIndex >= 0)
                {
                    var curve = AnimationUtility.GetEditorCurve(animationClip, binding);

                    if (jointIndex == 0 && animationClip.hasMotionCurves)
                    {
                        if (binding.propertyName.Contains("Motion"))
                        {
                            anim.MapEditorCurve(jointIndex, binding.propertyName, "MotionT", "MotionQ", curve);
                        }
                    }
                    else if (jointIndex == 0 && animationClip.hasRootCurves)
                    {
                        if (binding.propertyName.Contains("Root"))
                        {
                            anim.MapEditorCurve(jointIndex, binding.propertyName, "RootT", "RootQ", curve);
                        }
                    }
                    else
                    {
                        anim.MapEditorCurve(jointIndex, binding.propertyName, "m_LocalPosition", "m_LocalRotation", curve);
                    }
                }
            }

            anim.animationCurves.Sort((x, y) => x.CompareTo(y));

            return anim;
        }

        public AffineTransform SampleLocalJoint(int jointIndex, float sampleTimeInSeconds)
        {
            return jointSamplers[jointIndex].Evaluate(sampleTimeInSeconds);
        }

        public KeyframeAnimation AllocateCopyAtFixedSampleRate(float sampleRate)
        {
            int numJoints = jointSamplers.Length;

            KeyframeAnimation anim = new KeyframeAnimation();
            anim.animationCurves = new List<CurveInfo>(animationCurves.Count);
            anim.jointSamplers = new NativeArray<TransformSampler>(numJoints, Allocator.Persistent);
            anim.numFrames = (int)math.ceil(sampleRate * duration);

            for (int jointIndex = 0; jointIndex < numJoints; ++jointIndex)
            {
                TransformSampler sourceSampler = jointSamplers[jointIndex];
                TransformSampler destinationSampler = TransformSampler.CreateEmpty(sourceSampler.DefaultTransform);

                for (int curveIndex = 0; curveIndex < TransformSampler.NumCurves; ++curveIndex)
                {
                    if (sourceSampler.GetCurveProxy(curveIndex).HasCurve)
                    {
                        Curve curve = new Curve(anim.numFrames, Allocator.Persistent); // fixed framerate curve
                        anim.animationCurves.Add(new CurveInfo()
                        {
                            curve = curve,
                            jointIndex = jointIndex,
                            curveIndex = curveIndex,
                        });
                        destinationSampler.SetCurve(curveIndex, curve);
                    }
                }

                anim.jointSamplers[jointIndex] = destinationSampler;
            }

            return anim;
        }

        public void Dispose()
        {
            foreach (CurveInfo curveInfo in animationCurves)
            {
                curveInfo.curve.Dispose();
            }

            jointSamplers.Dispose();
        }

        void InitWithRigTransforms(AnimationRig targetRig)
        {
            animationCurves = new List<CurveInfo>();
            jointSamplers = new NativeArray<TransformSampler>(targetRig.NumJoints, Allocator.Persistent);

            for (int i = 0; i < targetRig.NumJoints; ++i)
            {
                jointSamplers[i] = TransformSampler.CreateEmpty(targetRig.Joints[i].localTransform);
            }
        }

        void MapEditorCurve(int jointIndex, string curveName, string posCurvePrefix, string rotCurvePrefix, AnimationCurve editorCurve)
        {
            int curveIndex;
            TransformSampler sampler = jointSamplers[jointIndex];
            Curve? curve = sampler.MapEditorCurve(curveName, posCurvePrefix, rotCurvePrefix, editorCurve, out curveIndex);
            jointSamplers[jointIndex] = sampler;

            if (curve.HasValue)
            {
                animationCurves.Add(new CurveInfo()
                {
                    curve = curve.Value,
                    jointIndex = jointIndex,
                    curveIndex = curveIndex
                });
            }
        }
    }
}
