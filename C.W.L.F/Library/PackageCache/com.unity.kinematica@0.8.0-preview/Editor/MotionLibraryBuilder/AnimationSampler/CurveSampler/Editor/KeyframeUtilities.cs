#define BABYPROOFING
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.Diagnostics;
using Unity.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif


public static class KeyframeUtilities
{
#if UNITY_EDITOR
    static PropertyInfo keyframeTangentMode;
    static PropertyInfo KeyframeTangentMode()
    {
        if (keyframeTangentMode == null)
        {
            keyframeTangentMode = typeof(Keyframe).GetProperty("tangentMode");
        }

        return keyframeTangentMode;
    }

    const int kBrokenMask = 1 << 0;
    const int kLeftTangentMask = 1 << 1 | 1 << 2 | 1 << 3 | 1 << 4;
    const int kRightTangentMask = 1 << 5 | 1 << 6 | 1 << 7 | 1 << 8;
    const int kLeftTangentOffset = 1;
    const int kRightTangentOffset = 5;

#endif

    [Conditional("UNITY_EDITOR")]
    public static void SetLeftTangentMode(ref Keyframe key, InterpolationMode mode)
    {
#pragma warning disable CS0618
        int tangentMode = key.tangentMode;

        tangentMode &= ~kLeftTangentMask;
        tangentMode |= (int)mode << kLeftTangentOffset;

        key.tangentMode = tangentMode;
#pragma warning restore CS0618
    }

    [Conditional("UNITY_EDITOR")]
    public static void SetRightTangentMode(ref Keyframe key, InterpolationMode mode)
    {
#pragma warning disable CS0618
        int tangentMode = key.tangentMode;

        tangentMode &= ~kRightTangentMask;
        tangentMode |= (int)mode << kRightTangentOffset;

        key.tangentMode = tangentMode;
#pragma warning restore CS0618
    }

    public enum InterpolationMode
    {
        Free,
        Auto,
        Linear,
        Constant,
        ClampedAuto,
    };

    public static float StartTime(Keyframe[] frames)
    {
        return frames[0].time;
    }

    public static float EndTime(Keyframe[] frames)
    {
        return frames[frames.Length - 1].time;
    }

    public static int Seek(float time, int index, Keyframe[] keyframes)
    {
        int numKeys = keyframes.Length;
        index = math.clamp(index, 0, numKeys - 1);

        if (time >= keyframes[index].time)
            while (index < (numKeys - 1) && time > keyframes[index + 1].time) { index++; }
        else
            while (index > 0 && time <= keyframes[index].time) { index--; }

        return index;
    }

    static public float Evaluate(float time, ref int index, Keyframe[] keyframes)
    {
        int numKeys = keyframes.Length;
        index = Seek(time, index, keyframes);
        Keyframe left = keyframes[index];
        Keyframe right = keyframes[math.clamp(index + 1, 0, numKeys - 1)];

        return Evaluate(math.clamp(time, left.time, right.time), left, right);
    }

    public static float Evaluate(float time, Keyframe lhs, Keyframe rhs)
    {
        float dx = rhs.time - lhs.time;
        float m0;
        float m1;
        float t;
        if (dx != 0.0f)
        {
            t = (time - lhs.time) / dx;
            m0 = lhs.outTangent * dx;
            m1 = rhs.inTangent * dx;
        }
        else
        {
            t = 0.0f;
            m0 = 0;
            m1 = 0;
        }

        return Hermite.Evaluate(t, lhs.value, m0, m1, rhs.value);
    }

    public static void AlignWithNext(ref Keyframe key, Keyframe next, bool broken)
    {
        float delta = (next.value - key.value) / (next.time - key.time);
        key.outTangent = delta;
        if (!broken)
        {
            key.inTangent = key.outTangent;
        }
    }

    public static void AlignWithPrevious(ref Keyframe key, Keyframe previous, bool broken)
    {
        float delta = (key.value - previous.value) / (key.time - previous.time);
        key.inTangent = delta;
        if (!broken)
        {
            key.outTangent = key.inTangent;
        }
    }

    public static void AlignWithNeighborsClamped(ref Keyframe key, Keyframe previous, Keyframe next)
    {
        const float bias = 0.5f;
        float min = math.min(previous.value, next.value);
        float max = math.max(previous.value, next.value);
        float range = max - min;
        float normalizedYPos = (key.value - min) / range;
        float factor = 2 * (1 - math.abs(2 * normalizedYPos - 1)) / bias;
        float clampedFactor = math.clamp(factor, 0, 1);

        float slope = (next.value - previous.value) / (next.time - previous.time);

        float smoothedSlope = slope * clampedFactor;

        key.inTangent = smoothedSlope;
        key.outTangent = smoothedSlope;
    }

    public static void AlignWithNeighbors(ref Keyframe key, Keyframe previous, Keyframe next)
    {
        float pre = (key.value - previous.value) / (key.time - previous.time);
        float post = (next.value - key.value) / (next.time - key.time);
        float avg = (pre + post) / 2;

        key.inTangent = avg;
        key.outTangent = avg;
    }

    public static void AlignTogether(ref Keyframe left, ref Keyframe right, bool broken)
    {
        float delta = (right.value - left.value) / (right.time - left.time);
        left.outTangent = right.inTangent = delta;
        if (!broken)
        {
            left.inTangent = left.outTangent;
            right.outTangent = right.inTangent;
        }
    }

    public static void AlignTangentsLinear(NativeArray<Keyframe> keyframes)
    {
        int numFrames = keyframes.Length;

        if (numFrames == 2)
        {
            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];
            AlignTogether(ref f, ref l, false);

            SetLeftTangentMode(ref f, InterpolationMode.Linear);
            SetRightTangentMode(ref f, InterpolationMode.Linear);
            SetLeftTangentMode(ref l, InterpolationMode.Linear);
            SetRightTangentMode(ref l, InterpolationMode.Linear);

            keyframes[0] = f;
            keyframes[1] = l;
        }
        else
        {
            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];
            for (int i = 1; i < numFrames - 1; i++)
            {
                Keyframe p = keyframes[i - 1];
                Keyframe c = keyframes[i];
                Keyframe n = keyframes[i + 1];

                AlignWithPrevious(ref c, p, true);
                AlignWithNext(ref c, n, true);

                SetLeftTangentMode(ref c, InterpolationMode.Linear);
                SetRightTangentMode(ref c, InterpolationMode.Linear);

                keyframes[i] = c;
            }

            AlignWithNext(ref f, keyframes[1], true);
            SetLeftTangentMode(ref f, InterpolationMode.Linear);
            SetRightTangentMode(ref f, InterpolationMode.Linear);

            AlignWithPrevious(ref l, keyframes[numFrames - 2], true);
            SetLeftTangentMode(ref l, InterpolationMode.Auto);
            SetRightTangentMode(ref l, InterpolationMode.Auto);

            keyframes[0] = f;
            keyframes[numFrames - 1] = l;
        }
    }

    public static void AlignTangentsSmooth(NativeArray<Keyframe> keyframes)
    {
        int numFrames = keyframes.Length;
        if (numFrames == 2)
        {
            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];

            AlignTogether(ref f, ref l, false);

            SetLeftTangentMode(ref f, InterpolationMode.Auto);
            SetRightTangentMode(ref f, InterpolationMode.Auto);

            SetLeftTangentMode(ref l, InterpolationMode.Auto);
            SetRightTangentMode(ref l, InterpolationMode.Auto);

            keyframes[0] = f;
            keyframes[1] = l;
        }
        else
        {
            for (int i = 1; i < numFrames - 1; i++)
            {
                Keyframe p = keyframes[i - 1];
                Keyframe c = keyframes[i];
                Keyframe n = keyframes[i + 1];

                AlignWithNeighbors(ref c, p, n);

                SetLeftTangentMode(ref c, InterpolationMode.Auto);
                SetRightTangentMode(ref c, InterpolationMode.Auto);

                keyframes[i] = c;
            }

            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];

            AlignWithNext(ref f, keyframes[1], false);
            SetLeftTangentMode(ref f, InterpolationMode.Auto);
            SetRightTangentMode(ref f, InterpolationMode.Auto);

            AlignWithPrevious(ref l, keyframes[numFrames - 2], false);
            SetLeftTangentMode(ref l, InterpolationMode.Auto);
            SetRightTangentMode(ref l, InterpolationMode.Auto);

            keyframes[0] = f;
            keyframes[numFrames - 1] = l;
        }
    }

    public static void AlignTangentsClamped(NativeArray<Keyframe> keyframes)
    {
        int numFrames = keyframes.Length;
        if (numFrames == 2)
        {
            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];

            AlignTogether(ref f, ref l, false);

            SetLeftTangentMode(ref f, InterpolationMode.ClampedAuto);
            SetRightTangentMode(ref f, InterpolationMode.ClampedAuto);

            SetLeftTangentMode(ref l, InterpolationMode.ClampedAuto);
            SetRightTangentMode(ref l, InterpolationMode.ClampedAuto);

            keyframes[0] = f;
            keyframes[1] = l;
        }
        else
        {
            for (int i = 1; i < numFrames - 1; i++)
            {
                Keyframe p = keyframes[i - 1];
                Keyframe c = keyframes[i];
                Keyframe n = keyframes[i + 1];

                AlignWithNeighborsClamped(ref c, p, n);

                SetLeftTangentMode(ref c, InterpolationMode.ClampedAuto);
                SetRightTangentMode(ref c, InterpolationMode.ClampedAuto);

                keyframes[i] = c;
            }

            Keyframe f = keyframes[0];
            Keyframe l = keyframes[numFrames - 1];

            AlignWithNext(ref f, keyframes[1], false);
            SetLeftTangentMode(ref f, InterpolationMode.ClampedAuto);
            SetRightTangentMode(ref f, InterpolationMode.ClampedAuto);

            AlignWithPrevious(ref l, keyframes[numFrames - 2], false);
            SetLeftTangentMode(ref l, InterpolationMode.ClampedAuto);
            SetRightTangentMode(ref l, InterpolationMode.ClampedAuto);

            keyframes[0] = f;
            keyframes[numFrames - 1] = l;
        }
    }
}
