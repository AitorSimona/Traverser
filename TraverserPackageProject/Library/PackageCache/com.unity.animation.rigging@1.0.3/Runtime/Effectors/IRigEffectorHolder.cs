using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This interface represents classes that hold and serialize effectors.
    /// </summary>
    public interface IRigEffectorHolder
    {
#if UNITY_EDITOR
        /// <summary>List of rig effectors associated with this IRigEffectorHolder.
        IEnumerable<RigEffectorData> effectors { get; }

        /// <summary>Adds a new effector to the IRigEffectorHolder.</summary>
        /// <param name="transform">The Transform represented by the effector.</param>
        /// <param name="style">The visual style of the effector.</param>
        void AddEffector(Transform transform, RigEffectorData.Style style);
        /// <summary>Removes an effector from the IRigEffectorHolder.</summary>
        /// <param name="transform">The Transform from which to remove the effector.</param>
        void RemoveEffector(Transform transform);
        /// <summary>Queries whether there is an effector for the specified Transform.</summary>
        /// <param name="transform">The Transform to query.</param>
        /// <returns>True if there is an effector for this transform. False otherwise.</returns>
        bool ContainsEffector(Transform transform);
#endif
    }
}
