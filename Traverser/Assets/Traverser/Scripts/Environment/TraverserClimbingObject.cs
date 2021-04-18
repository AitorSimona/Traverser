using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Traverser
{
    public class TraverserClimbingObject : MonoBehaviour
    {
        //// --- The different parkour types that will trigger different reactions from the player, this script is added to world objects ---
        //public enum TraverserClimbingType
        //{
        //    Wall,
        //    Table,
        //    Platform,
        //    Ledge,
        //    Tunnel
        //};

        public List<Transform> annotations;
        //public TraverserClimbingType type = TraverserClimbingType.Ledge;
    }
}
