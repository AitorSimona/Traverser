using NUnit.Framework;
using static Unity.Mathematics.math;

namespace Burst.Compiler.IL.Tests
{
    using Unity.Mathematics;

    [TestFixture]
    internal partial class MatricesTransforms
    {
        [TestCompiler]
        public static float TestlookRotationToMatrix()
        {
            var test = float4x4.LookAt(float3(0, 0, 1), float3(0, 1, 0), float3(1, 0, 0));
            return test.c0.x + test.c1.y + test.c2.z;
        }
    }
}