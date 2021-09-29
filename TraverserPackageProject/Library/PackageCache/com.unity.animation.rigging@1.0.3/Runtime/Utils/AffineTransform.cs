namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// Rigid transform with only translation and rotation components.
    /// </summary>
    [System.Serializable]
    public struct AffineTransform
    {
        /// <summary>Translation component of the AffineTransform.</summary>
        public Vector3 translation;
        /// <summary>Rotation component of the AffineTransform.</summary>
        public Quaternion rotation;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="t">Translation component of the AffineTransform.</param>
        /// <param name="r">Rotation component of the AffineTransform.</param>
        public AffineTransform(Vector3 t, Quaternion r)
        {
            translation = t;
            rotation = r;
        }

        /// <summary>
        /// Sets the translation and rotation in the AffineTransform.
        /// </summary>
        /// <param name="t">Translation component of the AffineTransform.</param>
        /// <param name="r">Rotation component of the AffineTransform.</param>
        public void Set(Vector3 t, Quaternion r)
        {
            translation = t;
            rotation = r;
        }

        /// <summary>
        /// Transforms a Vector3 point by the AffineTransform.
        /// </summary>
        /// <param name="p">Vector3 point.</param>
        /// <returns>Transformed Vector3 point.</returns>
        public Vector3 Transform(Vector3 p) =>
            rotation * p + translation;

        /// <summary>
        /// Transforms a Vector3 point by the inverse of the AffineTransform.
        /// </summary>
        /// <param name="p">Vector3 point.</param>
        /// <returns>Transformed Vector3 point.</returns>
        public Vector3 InverseTransform(Vector3 p) =>
            Quaternion.Inverse(rotation) * (p - translation);

        /// <summary>
        /// Calculates the inverse of the AffineTransform.
        /// </summary>
        /// <returns>The inverse of the AffineTransform.</returns>
        public AffineTransform Inverse()
        {
            var invR = Quaternion.Inverse(rotation);
            return new AffineTransform(invR * -translation, invR);
        }

        /// <summary>
        /// Multiply a transform by the inverse of the AffineTransform.
        /// </summary>
        /// <param name="transform">AffineTransform value.</param>
        /// <returns>Multiplied AffineTransform result.</returns>
        public AffineTransform InverseMul(AffineTransform transform)
        {
            var invR = Quaternion.Inverse(rotation);
            return new AffineTransform(invR * (transform.translation - translation), invR * transform.rotation);
        }

        /// <summary>
        /// Transforms a Vector3 point by the AffineTransform.
        /// </summary>
        /// <param name="lhs">AffineTransform value.</param>
        /// <param name="rhs">Vector3 point.</param>
        /// <returns>Transformed Vector3 point.</returns>
        public static Vector3 operator *(AffineTransform lhs, Vector3 rhs) =>
            lhs.rotation * rhs + lhs.translation;

        /// <summary>
        /// Multiplies two AffineTransform.
        /// </summary>
        /// <param name="lhs">First AffineTransform value.</param>
        /// <param name="rhs">Second AffineTransform value.</param>
        /// <returns>Multiplied AffineTransform result.</returns>
        public static AffineTransform operator *(AffineTransform lhs, AffineTransform rhs) =>
            new AffineTransform(lhs.Transform(rhs.translation), lhs.rotation * rhs.rotation);

        /// <summary>
        /// Rotates an AffineTransform.
        /// </summary>
        /// <param name="lhs">Quaternion rotation.</param>
        /// <param name="rhs">AffineTransform value.</param>
        /// <returns>Rotated AffineTransform result.</returns>
        public static AffineTransform operator *(Quaternion lhs, AffineTransform rhs) =>
            new AffineTransform(lhs * rhs.translation, lhs * rhs.rotation);

        /// <summary>
        /// Transforms a Quaternion value by the AffineTransform.
        /// </summary>
        /// <param name="lhs">AffineTransform value.</param>
        /// <param name="rhs">Quaternion rotation.</param>
        /// <returns>Transformed AffineTransform result.</returns>
        public static AffineTransform operator *(AffineTransform lhs, Quaternion rhs) =>
            new AffineTransform(lhs.translation, lhs.rotation * rhs);

        /// <summary>
        /// AffineTransform identity value.
        /// </summary>
        public static AffineTransform identity { get; } = new AffineTransform(Vector3.zero, Quaternion.identity);
    }
}
