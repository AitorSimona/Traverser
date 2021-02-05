using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        public float3 Position
        {
            get => characterController.transform.position;
            set => characterController.transform.position = value;
        }

        public void Snapshot()
        {
            snapshotState.CopyFrom(ref state);
        }

        public void Rewind()
        {
            state.CopyFrom(ref snapshotState);
            characterController.transform.position = state.transform.position;
            characterController.transform.rotation = state.transform.rotation;
        }

        public CollisionFlags Move(Vector3 motion)
        {
            state.previousCollision.CopyFrom(ref state.currentCollision);
            CollisionFlags flags = characterController.Move(motion);
            state.transform = characterController.transform;

            return flags;
        }
    }
}
