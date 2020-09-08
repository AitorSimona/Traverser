using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        /// <summary>
        /// Determines whether the specified trait value matches a payload stored in the runtime asset.
        /// </summary>
        /// <param name="value">Trait value to check the payload against.</param>
        /// <param name="payload">Payload index to check the trait for.</param>
        /// <returns>True if the trait value matches the payload store in the runtime asset; false otherwise.</returns>
        public unsafe bool IsPayload<T>(ref T value, int payload) where T : struct
        {
            void* src = (byte*)payloads.GetUnsafePtr() + payload;
            void* dst = UnsafeUtility.AddressOf(ref value);
            long size = UnsafeUtility.SizeOf<T>();

            return UnsafeUtility.MemCmp(src, dst, size) == 0;
        }

        /// <summary>
        /// Retrieves a reference to a payload stored in the runtime asset.
        /// </summary>
        /// <param name="payload">Identifies the payload that should be retrieved.</param>
        /// <returns>Reference to the payload that corresponds to the index passed as argument.</returns>
        public unsafe ref T GetPayload<T>(int payload) where T : struct
        {
            return ref UnsafeUtilityEx.AsRef<T>((byte*)payloads.GetUnsafePtr() + payload);
        }
    }
}
