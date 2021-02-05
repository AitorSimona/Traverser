using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        public void Snapshot()
        {
            snapshotState.CopyFrom(ref state);
        }

        public void Rewind()
        {
            state.CopyFrom(ref snapshotState);
        }
    }
}
