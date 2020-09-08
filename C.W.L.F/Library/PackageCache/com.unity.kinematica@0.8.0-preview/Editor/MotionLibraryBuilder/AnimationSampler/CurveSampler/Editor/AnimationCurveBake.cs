using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO.Pipes;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Curves;
using Unity.Jobs;
using NUnit.Framework;

public static class AnimationCurveBake
{
    public enum InterpolationMode
    {
        ConstantPre,
        ConstantPost,
        Linear,
        Auto,
        ClampedAuto
    };

    public struct SampleRange
    {
        public int startFrameIndex;
        public int numFrames;
    }

    public static int Bake(AnimationCurve curve, float frameRate, InterpolationMode mode)
    {
        var keys = new NativeArray<Keyframe>(curve.keys, Allocator.Temp);

        float duration = keys[keys.Length - 1].time - keys[0].time;
        int frameCount = (int)math.ceil(frameRate * duration);
        var bakedKeys = new NativeArray<Keyframe>(frameCount, Allocator.Temp);

        switch (mode)
        {
            case InterpolationMode.Linear:
                KeyframeUtilities.AlignTangentsLinear(bakedKeys);
                break;
            case InterpolationMode.Auto:
                KeyframeUtilities.AlignTangentsSmooth(bakedKeys);
                break;
            case InterpolationMode.ClampedAuto:
                KeyframeUtilities.AlignTangentsClamped(bakedKeys);
                break;
            default:
                throw new System.InvalidOperationException("Not Implemented");
        }
        curve.keys = bakedKeys.ToArray();
        keys.Dispose();
        bakedKeys.Dispose();
        return frameCount;
    }

    public static int Bake(ref Keyframe[] keys, float frameRate, InterpolationMode mode = InterpolationMode.Auto)
    {
        var nativeKeys = new NativeArray<Keyframe>(keys, Allocator.Temp);
        float duration = keys[keys.Length - 1].time - keys[0].time;
        int frameCount = (int)math.ceil(frameRate * duration);
        var bakedKeys = new NativeArray<Keyframe>(frameCount, Allocator.Temp);

        ThreadSafe.Bake(nativeKeys, frameRate, bakedKeys, new SampleRange() { startFrameIndex = 0, numFrames = frameCount });

        switch (mode)
        {
            case InterpolationMode.Linear:
                KeyframeUtilities.AlignTangentsLinear(bakedKeys);
                break;
            case InterpolationMode.Auto:
                KeyframeUtilities.AlignTangentsSmooth(bakedKeys);
                break;
            case InterpolationMode.ClampedAuto:
                KeyframeUtilities.AlignTangentsClamped(bakedKeys);
                break;
            default:
                throw new System.InvalidOperationException("Not Implemented");
        }


        keys = bakedKeys.ToArray();
        bakedKeys.Dispose();
        return frameCount;
    }

    public static class ThreadSafe
    {
        [BurstCompile(CompileSynchronously = true)]
        public struct BakeJob : IJobParallelFor, IDisposable
        {
            public int numJobs;
            public NativeArray<CurveData> curves;
            public NativeArray<CurveData> outCurves;
            public SampleRange sampleRange;

            public float frameRate;


            public void Execute(int index)
            {
                Curve curve = curves[index].ToCurve();
                Curve outCurve = outCurves[index].ToCurve();

                Bake(curve, frameRate, outCurve, sampleRange);
            }

            public void Dispose()
            {
                curves.Dispose();
                outCurves.Dispose();
            }
        }

        public static BakeJob ConfigureBake(Curve[] curves, float frameRate, Curve[] outCurves, Allocator alloc, SampleRange sampleRange)
        {
            var job = new BakeJob();
            job.curves = new NativeArray<CurveData>(curves.Length, alloc);
            job.frameRate = frameRate;
            job.outCurves = new NativeArray<CurveData>(curves.Length, alloc);
            job.sampleRange = sampleRange;

            for (int i = 0; i < curves.Length; i++)
            {
                job.curves[i] = new CurveData(curves[i], alloc);
                int frameCount = (int)math.ceil(frameRate * curves[i].Duration);
                //var outCurve = new Curve(frameCount, alloc);
                //outCurves[i] = outCurve;
                job.outCurves[i] = new CurveData(outCurves[i], alloc);
            }

            return job;
        }

        public static BakeJob ConfigureBake(Curve[] curves, float frameRate, Curve[] outCurves, Allocator alloc)
        {
            SampleRange sampleRange = new SampleRange()
            {
                startFrameIndex = 0,
                numFrames = curves[0].Length
            };

            return ConfigureBake(curves, frameRate, outCurves, alloc, sampleRange);
        }

        public static int Bake(Curve curve, float frameRate, Curve outCurve, SampleRange sampleRange)
        {
            NativeArray<Keyframe> keys = curve.Keys;
            NativeArray<Keyframe> outKeys = outCurve.Keys;
            int bakedFrames = Bake(keys, frameRate, outKeys, sampleRange);
            //  KeyframeUtilities.AlignTangentsSmooth(outKeys, sampleRange);
            return bakedFrames;
        }

        public static int Bake(NativeArray<Keyframe> keyframes, float frameRate, NativeArray<Keyframe> bakedFrames, SampleRange sampleRange)
        {
            int numKeys = keyframes.Length;
            float start = keyframes[0].time;
            float end = keyframes[numKeys - 1].time;
            float duration = end - start;
            float frame = 1 / frameRate;
            int numFrames = bakedFrames.Length;

            for (int i = 0; i < sampleRange.numFrames; i++)
            {
                int frameIndex = sampleRange.startFrameIndex + i;
                float time = math.clamp(start + frameIndex * frame, start, end);

                float value = CurveSampling.ThreadSafe.EvaluateWithinRange(keyframes, time, 0, numKeys - 1);
                bakedFrames[frameIndex] = new Keyframe(time, value);
            }

            return sampleRange.numFrames;
        }
    }
}
