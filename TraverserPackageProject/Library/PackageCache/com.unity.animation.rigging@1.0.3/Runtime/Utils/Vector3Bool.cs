namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Three-dimensional boolean vector.
    /// </summary>
    [System.Serializable]
    public struct Vector3Bool
    {
        /// <summary>X component of the vector.</summary>
        public bool x;
        /// <summary>Y component of the vector.</summary>
        public bool y;
        /// <summary>Z component of the vector.</summary>
        public bool z;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="val">Boolean value for x, y and z.</param>
        public Vector3Bool(bool val)
        {
            x = y = z = val;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="x">Boolean value for x.</param>
        /// <param name="y">Boolean value for y.</param>
        /// <param name="z">Boolean value for z.</param>
        public Vector3Bool(bool x, bool y, bool z)
        {
            this.x = x; this.y = y; this.z = z;
        }
    }
}
