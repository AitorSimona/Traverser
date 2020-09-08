using System;
using UnityEngine;

namespace Unity.SnapshotDebugger
{
    /// <summary>
    /// Identifier used to uniquely identify snapshot components.
    /// </summary>
    [Serializable]
    public struct Identifier<T> : IEquatable<Identifier<T>>
    {
        static internal int nextIdentifier;

        [SerializeField]
        int id;

        Identifier(int id)
        {
            this.id = id;
        }

        internal static Identifier<T> Create(int id)
        {
            return new Identifier<T>(id);
        }

        /// <summary>
        /// Creates a new snapshot identifier.
        /// </summary>
        public static Identifier<T> Create()
        {
            return Create(++nextIdentifier);
        }

        /// <summary>
        /// An undefined snapshot identifier.
        /// </summary>
        public static Identifier<T> Undefined
        {
            get { return Create(0); }
        }

        /// <summary>
        /// Determines if the given snapshot identifier is valid or not.
        /// </summary>
        /// <returns>True if the identifier is valid; false otherwise.</returns>
        public bool IsValid
        {
            get { return id > 0; }
        }

        /// <summary>
        /// Equality operator for snapshot identifiers.
        /// </summary>
        public static bool operator==(Identifier<T> lhs, Identifier<T> rhs) => Equals(lhs, rhs);

        /// <summary>
        /// Inequality operator for snapshot identifiers.
        /// </summary>
        public static bool operator!=(Identifier<T> lhs, Identifier<T> rhs) => !Equals(lhs, rhs);

        /// <summary>
        /// Determines whether two snapshot identifiers are equal.
        /// </summary>
        /// <param name="obj">The snapshot identifier to compare against the current snapshot identifier.</param>
        /// <returns>True if the specified snapshot identifier is equal to the current snapshot identifier; otherwise, false.</returns>
        public override bool Equals(object obj) => (obj is Identifier<T> identity) && Equals(identity);

        /// <summary>
        /// Determines whether two snapshot identifiers are equal.
        /// </summary>
        /// <param name="other">The snapshot identifier to compare against the current snapshot identifier.</param>
        /// <returns>True if the specified snapshot identifier is equal to the current snapshot identifier; otherwise, false.</returns>
        public bool Equals(Identifier<T> other) => id == other.id;

        /// <summary>
        /// Override for GetHashCode().
        /// </summary>
        public override int GetHashCode() => id.GetHashCode();

        /// <summary>
        /// Override for ToString().
        /// </summary>
        public override string ToString() => string.Format("Identifier({0})", id);

        /// <summary>
        /// Implicit conversion from a snapshot identifier to an integer.
        /// </summary>
        public static implicit operator int(Identifier<T> identity) => identity.id;
    }
}
