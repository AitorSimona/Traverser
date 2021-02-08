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
        public float3 Position
        {
            get => state.currentCollision.position;
            set => state.currentCollision.position = value;
        }

        // --- The next position user code should move to ---
        public float3 targetPosition;

        // --- Whether or not gravity will be applied to the controller ---
        public bool gravityEnabled = true;

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
            //// --- TEMPORAL DEBUG UTILITY ---
            //float3 dummyPos = Position;
            //dummyPos.y += characterController.height / 2;
            //GameObject.Find("Capsule").transform.position = dummyPos;

            // --- Use snapshotState values to return to pre-simulation situation ---
            state.CopyFrom(ref snapshotState);

            // --- Place character controller at the pre-simulation position ---
            characterController.enabled = false;
            transform.position = Position;
            characterController.enabled = true;
        }

        // --------------------------------

        // --- Movement methods ---

        public void Move(float3 displacement)
        {
            state.desiredDisplacement += displacement;
        }

        public void MoveTo(float3 position)
        {
            Move(position - Position);
        }

        public void ForceMove(Vector3 position)
        {
            characterController.Move(position - transform.position);
            Position = transform.position;
        }

        public void Tick(float deltaTime)
        {
            UpdateVelocity(deltaTime);

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

        void UpdateVelocity(float deltaTime)
        {
           // float3 verticalAccumulatedVelocity =
           //    Missing.project(state.accumulatedVelocity, math.up());

           //if (math.dot(math.normalize(verticalAccumulatedVelocity), math.up()) <= 0.0f)
           //{
           //     float3 lateralAccumulatedVelocity =
           //        state.accumulatedVelocity - verticalAccumulatedVelocity;

           //    state.accumulatedVelocity = lateralAccumulatedVelocity;
           //}         
        }

        void UpdateMovement(float deltaTime)
        {
            state.currentCollision.kinematicDisplacement = state.desiredVelocity * deltaTime + state.desiredDisplacement;
            state.desiredDisplacement = Vector3.zero;

            if (gravityEnabled)
            {
                float3 gravity = Physics.gravity;
                state.accumulatedVelocity += gravity * deltaTime;
                state.currentCollision.dynamicsDisplacement = state.accumulatedVelocity;
            }

            float3 desiredDisplacement = state.currentCollision.kinematicDisplacement + state.currentCollision.dynamicsDisplacement;

            float3 finalPosition = Position + desiredDisplacement;
            ForceMove(finalPosition);
            state.currentCollision.velocity = (finalPosition - state.previousCollision.position) / deltaTime;
        }

        // --------------------------------

        // --- Events ---

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
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
