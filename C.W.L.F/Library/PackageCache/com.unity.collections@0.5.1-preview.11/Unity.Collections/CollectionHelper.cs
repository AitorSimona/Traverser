using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
#if !NET_DOTS
using System.Reflection;
#endif

namespace Unity.Collections
{
    public static class CollectionHelper
    {
        public const int CacheLineSize = JobsUtility.CacheLineSize;

        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            public long longValue;
            [FieldOffset(0)]
            public double doubleValue;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
    #if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
        [Obsolete("Use math.lzcnt from Unity.Mathematics instead. (RemovedAfter 2020-03-02). If you see this message in a user project, remove the UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE define from the Entities assembly definition file.")]
    #else
        [Obsolete("Use math.lzcnt from Unity.Mathematics instead. (RemovedAfter 2020-03-02) (UnityUpgradable) -> Unity.Mathematics.math.lzcnt(*)")]
    #endif
        public static int lzcnt(uint x)
        {
            return math.lzcnt(x);
        }

        public static int Log2Floor(int value)
        {
            return 31 - math.lzcnt((uint)value);
        }
        
        public static int Log2Ceil(int value)
        {
            return 32 - math.lzcnt((uint)value-1);
        }


        [EditorBrowsable(EditorBrowsableState.Never)]
    #if UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE
        [Obsolete("Use math.ceilpow2 from Unity.Mathematics instead. (RemovedAfter 2020-03-02). If you see this message in a user project, remove the UNITY_SKIP_UPDATES_WITH_VALIDATION_SUITE define from the Entities assembly definition file.")]
    #else
        [Obsolete("Use math.ceilpow2 from Unity.Mathematics instead. (RemovedAfter 2020-03-02) (UnityUpgradable) -> Unity.Mathematics.math.ceilpow2(*)")]
    #endif
        public static int CeilPow2(int i)
        {
            i -= 1;
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        public static void CheckIntPositivePowerOfTwo(int value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
                throw new ArgumentException("Alignment requested: {value} is not a non-zero, positive power of two.");
        }

        public static int Align(int size, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;
            
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        public static unsafe bool IsAligned(void* p, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return ((ulong)p & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        public static unsafe bool IsAligned(ulong offset, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return (offset & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        /// Returns hash value of memory block. Function is using djb2 (non-cryptographic hash).
        /// </summary>
        public static unsafe uint Hash(void* pointer, int bytes)
        {
            // djb2 - Dan Bernstein hash function
            // http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html
            byte* str = (byte*)pointer;
            ulong hash = 5381;
            while (bytes > 0)
            {
                ulong c = str[--bytes];
                hash = ((hash << 5) + hash) + c;
            }
            return (uint)hash;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        public static void CheckIsUnmanaged<T>()
        {
#if !UNITY_DOTSPLAYER // todo: enable when this is supported
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
#else
            if (!UnsafeUtility.IsUnmanaged<T>())
        #endif
                throw new ArgumentException($"{typeof(T)} used in native collection is not blittable, not primitive, or contains a type tagged as NativeContainer");
        }

        internal static void WriteLayout(Type type)
        {
#if !NET_DOTS
            Console.WriteLine("   Offset | Bytes  | Name     Layout: {0}", type.Name);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Console.WriteLine("   {0, 6} | {1, 6} | {2}"
                    , Marshal.OffsetOf(type, field.Name)
                    , Marshal.SizeOf(field.FieldType)
                    , field.Name
                    );
            }
#else
            _ = type;
#endif
        }

        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }   
    }
}
