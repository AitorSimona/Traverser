using Burst.Compiler.IL.Tests.Helpers;
using NUnit.Framework;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    [TestFixture]
    internal partial class VectorsEquality
    {
        // TODO: Add tests for Uint4/3/2, Bool4/3/2

        // Float4
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Float4Equals(ref float4 a, ref float4 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float4Equality(ref float4 a, ref float4 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float4Inequality(ref float4 a, ref float4 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float4EqualityWithFloat(ref float4 a, float b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float4InequalityWithFloat(ref float4 a, float b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        // Float3
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Float3Equals(ref float3 a, ref float3 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float3Equality(ref float3 a, ref float3 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float3Inequality(ref float3 a, ref float3 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float3EqualityWithFloat(ref float3 a, float b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float3InequalityWithFloat(ref float3 a, float b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        // Float2
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Float2Equals(ref float2 a, ref float2 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float2Equality(ref float2 a, ref float2 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float2Inequality(ref float2 a, ref float2 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float2EqualityWithFloat(ref float2 a, float b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Float2InequalityWithFloat(ref float2 a, float b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        // Int4
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Int4Equals(ref int4 a, ref int4 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int4Equality(ref int4 a, ref int4 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int4Inequality(ref int4 a, ref int4 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int4EqualityWithInt(ref int4 a, int b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int4InequalityWithInt(ref int4 a, int b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        // Int3
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Int3Equals(ref int3 a, ref int3 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int3Equality(ref int3 a, ref int3 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int3Inequality(ref int3 a, ref int3 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int3EqualityWithInt(ref int3 a, int b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int3InequalityWithInt(ref int3 a, int b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        // Int2
        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static bool Int2Equals(ref int2 a, ref int2 b)
        {
            return a.Equals(b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int2Equality(ref int2 a, ref int2 b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int2Inequality(ref int2 a, ref int2 b)
        {
            return Vectors.ConvertToInt(a != b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int2EqualityWithInt(ref int2 a, int b)
        {
            return Vectors.ConvertToInt(a == b);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int Int2InequalityWithInt(ref int2 a, int b)
        {
            return Vectors.ConvertToInt(a != b);
        }
    }
}