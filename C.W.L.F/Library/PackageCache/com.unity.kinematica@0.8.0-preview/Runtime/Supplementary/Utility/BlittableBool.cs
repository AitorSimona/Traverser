namespace Unity.Kinematica
{
    /// <summary>
    /// Plain struct that wraps a boolean value into a DOTS compatible data type.
    /// </summary>
    public struct BlittableBool
    {
        internal byte value;

        /// <summary>
        /// Constructs a blittable bool from a boolean value.
        /// </summary>
        public BlittableBool(bool value)
        {
            this.value = (byte)(value ? 1 : 0);
        }

        /// <summary>
        /// Implicit cast operator that allows to convert a blittable bool into a bool.
        /// </summary>
        public static implicit operator bool(BlittableBool value)
        {
            return value.value == 1;
        }

        /// <summary>
        /// Implicit cast operator that allows to convert a bool into a blittable bool.
        /// </summary>
        public static implicit operator BlittableBool(bool value)
        {
            return new BlittableBool(value);
        }
    }
}
