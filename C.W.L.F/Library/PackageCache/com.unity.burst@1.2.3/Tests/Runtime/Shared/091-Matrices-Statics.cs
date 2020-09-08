using NUnit.Framework;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    [TestFixture]
    internal partial class MatricesStatics
    {
        [TestCompiler]
        public static float IdentityFloat4x4()
        {
            var test = float4x4.identity;
            return test.c0.x + test.c1.y + test.c2.z + test.c3.w;
        }
        [TestCompiler]
        public static float IdentityFloat3x3()
        {
            var test = float3x3.identity;
            return test.c0.x + test.c1.y + test.c2.z;
        }
        [TestCompiler]
        public static float IdentityFloat2x2()
        {
            var test = float2x2.identity;
            return test.c0.x + test.c1.y;
        }
    }
}