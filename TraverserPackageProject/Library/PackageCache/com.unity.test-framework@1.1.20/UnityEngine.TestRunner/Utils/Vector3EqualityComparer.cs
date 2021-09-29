using System.Collections.Generic;


namespace UnityEngine.TestTools.Utils
{
    /// <summary>
    /// Use this class to compare two Vector3 objects for equality with NUnit constraints. Call Vector3EqualityComparer.Instance comparer to perform a comparison with the default calculation error value 0.0001f. To specify a different error value, use the one argument constructor to instantiate a new comparer.
    /// </summary>
    public class Vector3EqualityComparer : IEqualityComparer<Vector3>
    {
        private const float k_DefaultError = 0.0001f;
        private readonly float AllowedError;

        private static readonly Vector3EqualityComparer m_Instance = new Vector3EqualityComparer();

        /// <returns>A comparer instance with the default calculation error value equal to 0.0001f.
        ///</returns>
        public static Vector3EqualityComparer Instance { get { return m_Instance; } }

        private Vector3EqualityComparer() : this(k_DefaultError) {}

        /// <param name="allowedError">This value identifies the calculation error allowed.</param>
        /// <typeparam name="float">A float number.</typeparam>
        /// <returns>A comparer with custom allowed calculation error </returns>
        public Vector3EqualityComparer(float allowedError)
        {
            this.AllowedError = allowedError;
        }
        ///<summary>
        /// Compares the actual and expected Vector3 objects
        /// for equality using <see cref="Utils.AreFloatsEqual"/> to compare the x, y, and z attributes of Vector3.
        /// </summary>
        /// <param name="expected">The expected Vector3 used for comparison</param>
        /// <typeparam name="Vector3"></typeparam>
        /// <param name="actual">The actual Vector3 to test</param>
        /// <typeparam name="Vector3"></typeparam>
        /// <returns>True if the vectors are equals, false otherwise.</returns>
        /// <example>
        /// The following example shows how to verify if two Vector3 are equals
        /// <code>
        /// [TestFixture]
        /// public class Vector3Test
        /// {
        ///     [Test]
        ///     public void VerifyThat_TwoVector3ObjectsAreEqual()
        ///     {
        ///     //Custom error 10e-6f
        ///     var actual = new Vector3(10e-8f, 10e-8f, 10e-8f);
        ///     var expected = new Vector3(0f, 0f, 0f);
        ///     var comparer = new Vector3EqualityComparer(10e-6f);
        ///
        /// 
        ///     Assert.That(actual, Is.EqualTo(expected).Using(comparer));
        ///
        ///     //Default error 0.0001f
        ///     actual = new Vector3(0.01f, 0.01f, 0f);
        ///     expected = new Vector3(0.01f, 0.01f, 0f);
        ///
        ///     Assert.That(actual, Is.EqualTo(expected).Using(Vector3EqualityComparer.Instance));
        ///     }
        /// }
        /// </code>
        ///</example>

        public bool Equals(Vector3 expected, Vector3 actual)
        {
            return Utils.AreFloatsEqual(expected.x, actual.x, AllowedError) &&
                Utils.AreFloatsEqual(expected.y, actual.y, AllowedError) &&
                Utils.AreFloatsEqual(expected.z, actual.z, AllowedError);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <param name="vec3">A not null Vector3</param>
        /// <returns>Returns 0</returns>
        public int GetHashCode(Vector3 vec3)
        {
            return 0;
        }
    }
}
