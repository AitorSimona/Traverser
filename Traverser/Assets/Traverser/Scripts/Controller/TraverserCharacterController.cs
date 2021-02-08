using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        // --- Attributes ---

        // --- The current state's previous collision situation ---
        public ref TraverserCollision previous
        {
            get => ref state.previousCollision;
        }

        // --- The current state's actual collision situation ---
        public ref TraverserCollision current
        {
            get => ref state.currentCollision;
        }

        // --- The current state's position (simulation) ---
        public float3 position
        {
            get => state.currentCollision.position;
            set => state.currentCollision.position = value;
        }

        // --- The current state's velocity (simulation) ---
        public float3 velocity
        {
            get => state.currentCollision.velocity;
        }

        // --- The next position user code should move to ---
        public float3 targetPosition;

        // --- Whether or not gravity will be applied to the controller ---
        public bool gravityEnabled = true;

        // --- Character controller's capsule collider height ---
        public float capsuleHeight 
        {
            get => characterController.height;
        }

        // --- Utility to store only the first tick's target position ---
        private bool firstTick = true;

        // --------------------------------

        // --- Simulation methods ---

        public void Snapshot()
        {
            // --- Copy current state to snapshot so we can rewind time ---
            snapshotState.CopyFrom(ref state);
            firstTick = true;
        }

        public void Rewind()
        {
            // --- Use snapshotState values to return to pre-simulation situation ---
            state.CopyFrom(ref snapshotState);

            // --- Place character controller at the pre-simulation position ---
            characterController.enabled = false;
            transform.position = position;
            characterController.enabled = true;
        }

        // --------------------------------

        // --- Movement methods ---

        public void Move(float3 displacement)
        {
            state.desiredDisplacement += displacement;
        }

        public void MoveTo(float3 desiredPosition)
        {
            Move(desiredPosition - position);
        }

        public void ForceMove(Vector3 desiredPosition)
        {
            characterController.Move(desiredPosition - transform.position);
            position = transform.position;
        }

        public void Tick(float deltaTime)
        {
            // --- Update controller's movement with given deltaTime ---

            state.previousCollision.CopyFrom(ref state.currentCollision);
            state.currentCollision.Reset();
            state.currentCollision.position = state.previousCollision.position;

            UpdateMovement(deltaTime);

            // --- If on simulation's first tick, store position ---
            if (firstTick)
            {
                // --- This is the target position of the current frame ---
                targetPosition = transform.position;
                firstTick = false;
            }
        }

        void UpdateMovement(float deltaTime)
        {
            state.currentCollision.kinematicDisplacement = state.desiredDisplacement;
            state.desiredDisplacement = Vector3.zero;

            if (gravityEnabled)
            {
                float3 gravity = Physics.gravity;
                state.currentCollision.dynamicsDisplacement = gravity * deltaTime;
            }

            float3 desiredDisplacement = state.currentCollision.kinematicDisplacement + state.currentCollision.dynamicsDisplacement;

            float3 finalPosition = position + desiredDisplacement;
            ForceMove(finalPosition);

            //Debug.Log(state.desiredVelocity);
        }

        // --------------------------------

        // --- Events ---

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // --- Update current collision information with hit's information ---
            if (hit.gameObject.name != "Ground")
            {
                Debug.Log("Collided with:");
                Debug.Log(hit.gameObject.name);
            }
            state.previousCollision.CopyFrom(ref state.currentCollision);
            state.currentCollision.colliderContactPoint = hit.point;
            state.currentCollision.colliderContactNormal = hit.normal;
            state.currentCollision.collider = hit.collider;
            state.currentCollision.isColliding = true;
        }

        // --------------------------------
    }
}
