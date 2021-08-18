using UnityEngine;

namespace Traverser
{
    public class TraverserParkourObject : MonoBehaviour
    {
        // --- The different parkour types that will trigger different reactions from the player,
        // this script is added to world objects ---
        public enum TraverserParkourType
        {
            Wall,
            Table,
            Platform,
            Ledge,
            Tunnel,
            LedgeToLedge
        };

        public TraverserParkourType type = TraverserParkourType.Ledge;
    }
}
