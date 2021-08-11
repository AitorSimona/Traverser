using UnityEngine;

namespace Traverser
{
    public class TraverserClimbingObject : MonoBehaviour
    {
        // --- The different climbable types that will trigger different reactions from the player, this script is added to world objects ---

        public enum TraverserClimbingType
        {
            Wall,
            Ledge
        };

        public TraverserClimbingType type = TraverserClimbingType.Ledge;
    }
}
