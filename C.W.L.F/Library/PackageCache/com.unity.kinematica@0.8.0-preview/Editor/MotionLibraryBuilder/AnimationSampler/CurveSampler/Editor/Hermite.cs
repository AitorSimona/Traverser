using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public static class Hermite
{
    public static float Evaluate(float t, float p0, float m0, float m1, float p1)
    {
        // Unrolled the equations to avoid precision issue.
        // (2 * t^3 -3 * t^2 +1) * p0 + (t^3 - 2 * t^2 + t) * m0 + (-2 * t^3 + 3 * t^2) * p1 + (t^3 - t^2) * m1

        var a = 2.0f * p0 + m0 - 2.0f * p1 + m1;
        var b = -3.0f * p0 - 2.0f * m0 + 3.0f * p1 - m1;
        var c = m0;
        var d = p0;

        return t * (t * (a * t + b) + c) + d;
    }
}
