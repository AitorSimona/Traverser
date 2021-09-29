using Burst.Compiler.IL.Tests.Helpers;
using System.Linq.Expressions;
using Unity.Mathematics;

namespace Burst.Compiler.IL.Tests
{
    /// <summary>
    /// Tests a few single float functions for <see cref="Unity.Mathematics.math"/> functions.
    /// </summary>
    internal class TestUnityMath
    {
        [TestCompiler(DataRange.Standard)]
        public static float TestCos(float value)
        {
            return math.cos(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestSin(float value)
        {
            return math.sin(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestTan(float value)
        {
            return (float) math.tan(value);
        }

        [TestCompiler(-1000000f)]
        [TestCompiler(-1.2f)]
        public static float TestTan2(float value)
        {
            return (float)math.tan(value);
        }

        [TestCompiler(DataRange.Standard11)]
        public static float TestAcos(float value)
        {
            return math.acos(value);
        }

        [TestCompiler(DataRange.Standard11)]
        public static float TestAsin(float value)
        {
            return math.asin(value);
        }

        [TestCompiler(DataRange.Standard11)]
        public static float TestAtan(float value)
        {
            return (float)math.atan(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestCosh(float value)
        {
            return math.cosh(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestSinh(float value)
        {
            return math.sinh(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestTanh(float value)
        {
            return (float)math.tanh(value);
        }

        [TestCompiler(DataRange.StandardPositive)]
        public static float TestSqrt(float value)
        {
            return math.sqrt(value);
        }

        [TestCompiler(DataRange.StandardPositive & ~DataRange.Zero)]
        public static float TestLog(float value)
        {
            return math.log(value);
        }

        [TestCompiler(DataRange.StandardPositive & ~DataRange.Zero)]
        public static float TestLog10(float value)
        {
            return math.log10(value);
        }

        [TestCompiler(DataRange.StandardPositive)]
        public static float TestExp(float value)
        {
            return math.exp(value);
        }

        [TestCompiler(DataRange.Standard & ~(DataRange.Zero|DataRange.NaN), DataRange.Standard)]
        [TestCompiler(DataRange.Standard & ~DataRange.Zero, DataRange.Standard & ~DataRange.Zero)]
        public static float TestPow(float value, float power)
        {
            return math.pow(value, power);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestAbsFloat(float value)
        {
            return math.abs(value);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int TestMaxInt(int left, int right)
        {
            return math.max(left, right);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static int TestMinInt(int left, int right)
        {
            return math.min(left, right);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static float TestMaxfloat(float left, float right)
        {
            return math.max(left, right);
        }

        [TestCompiler(DataRange.Standard, DataRange.Standard)]
        public static float TestMinfloat(float left, float right)
        {
            return math.min(left, right);
        }

        [TestCompiler(DataRange.Standard & ~DataRange.NaN)]
        public static float TestSignFloat(float value)
        {
            return math.sign(value);
        }

        [TestCompiler(-123.45)]
        [TestCompiler(-1E-20)]
        [TestCompiler(0.0)]
        [TestCompiler(1E-10)]
        [TestCompiler(123.45)]
        [TestCompiler(double.NegativeInfinity)]
        [TestCompiler(double.NaN)]
        [TestCompiler(double.PositiveInfinity)]
        public static double TestSignDouble(double value)
        {
            return math.sign(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestCeilingfloat(float value)
        {
            return math.ceil(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestFloorfloat(float value)
        {
            return math.floor(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestRoundfloat(float value)
        {
            return math.round(value);
        }

        [TestCompiler(DataRange.Standard)]
        public static float TestTruncatefloat(float value)
        {
            return math.trunc(value);
        }

        private readonly static float3 a = new float3(1, 2, 3);

        [TestCompiler]
        public static bool TestStaticLoad()
        {
            var cmp = a == new float3(1, 2, 3);

            return cmp.x && cmp.y && cmp.z;
        }

        [TestCompiler(42L)]
        public static long TestLongCountbits(long value)
        {
            return math.countbits(value) - 1;
        }

        [TestCompiler(42L)]
        public static long TestLongLzcnt(long value)
        {
            return math.lzcnt(value) - 1;
        }

        [TestCompiler(42L)]
        public static long TestLongTzcnt(long value)
        {
            return math.tzcnt(value) - 1;
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestUshortAddInt2(int2* o, ushort i)
        {
            *o = i + new int2(0, 0);
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestInt2AddUshort(int2* o, ushort i)
        {
            *o = new int2(0, 0) + i;
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestUshortSubInt2(int2* o, ushort i)
        {
            *o = i - new int2(0, 0);
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestInt2SubUshort(int2* o, ushort i)
        {
            *o = new int2(0, 0) - i;
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestUshortMulInt2(int2* o, ushort i)
        {
            *o = i * new int2(0, 0);
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestInt2MulUshort(int2* o, ushort i)
        {
            *o = new int2(0, 0) * i;
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestUshortDivInt2(int2* o, ushort i)
        {
            *o = i / new int2(1, 1);
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestInt2DivUshort(int2* o, ushort i)
        {
            *o = new int2(0, 0) / i;
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestUshortModInt2(int2* o, ushort i)
        {
            *o = i % new int2(1, 1);
        }

        [TestCompiler(typeof(ReturnBox), (ushort)42)]
        public static unsafe void TestInt2ModUshort(int2* o, ushort i)
        {
            *o = new int2(0, 0) % i;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortEqInt2(ushort i)
        {
            return math.all(i == new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2EqUshort(ushort i)
        {
            return math.all(new int2(0, 0) == i) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortNeInt2(ushort i)
        {
            return math.all(i != new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2NeUshort(ushort i)
        {
            return math.all(new int2(0, 0) != i) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortGeInt2(ushort i)
        {
            return math.all(i >= new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2GeUshort(ushort i)
        {
            return math.all(new int2(0, 0) >= i) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortGtInt2(ushort i)
        {
            return math.all(i > new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2GtUshort(ushort i)
        {
            return math.all(new int2(0, 0) > i) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortLtInt2(ushort i)
        {
            return math.all(i < new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2LtUshort(ushort i)
        {
            return math.all(new int2(0, 0) < i) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestUshortLeInt2(ushort i)
        {
            return math.all(i <= new int2(0, 0)) ? 1 : 0;
        }

        [TestCompiler((ushort)42)]
        public static unsafe int TestInt2LeUshort(ushort i)
        {
            return math.all(new int2(0, 0) <= i) ? 1 : 0;
        }
    }
}
