using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        // --- Attributes ---
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Select the object to show geometry.")]
        public bool showDebug = false;

        [Header("Controller")]
        [Tooltip("Whether or not gravity will be applied to the controller.")]
        public bool gravityEnabled = true;
        [Tooltip("How much will given displacement be increased, bigger stepping increases prediction reach at the cost of precision (void space). Can be overwriten by abilities.")]
        [Range(1.0f, 10.0f)]
        public float stepping = 1.0f;

        [Header("Collision")]
        [Tooltip("Radius of the ground collision check sphere.")]
        [Range(0.25f, 0.5f)]
        public float groundProbeRadius = 0.25f;

        [Header("Debug")]
        [Tooltip("Reference to capsule mesh for debugging purposes.")]
        public Mesh capsuleDebugMesh;
        [Tooltip("The debug mesh's scale.")]
        public float3 capsuleDebugMeshScale = Vector3.one;

        // --- The current state's position (simulation) ---
        public float3 position { get => state.currentCollision.position; set => state.currentCollision.position = value; }

        // --- The next position user code should move to ---
        [HideInInspector]
        public float3 targetPosition;

        // --- The current state's velocity (simulation) ---
        public float3 velocity { get => state.currentCollision.velocity; }

        // --- The current state's previous collision situation ---
        public ref TraverserCollision previous { get => ref state.previousCollision; }

        // --- The current state's actual collision situation ---
        public ref TraverserCollision current { get => ref state.currentCollision; }

        // --- Character controller's capsule collider height ---
        public float capsuleHeight { get => characterController.height; }

        // --- Whether or not the character controller's capsule collider is grounded ---
        public bool isGrounded { get => current.isGrounded; }

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
            // --- Store simulation's last position ---
            lastPosition = position;

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
            state.currentCollision.kinematicDisplacement = state.desiredDisplacement * stepping;
            state.desiredDisplacement = Vector3.zero;

            if (gravityEnabled)
            {
                float3 gravity = Physics.gravity;
                state.currentCollision.dynamicsDisplacement = gravity * deltaTime;
            }

            float3 desiredDisplacement = state.currentCollision.kinematicDisplacement + state.currentCollision.dynamicsDisplacement;

            float3 finalPosition = position + desiredDisplacement;
            ForceMove(finalPosition);
            state.currentCollision.velocity = characterController.velocity / stepping;

            if(characterController.detectCollisions)
                CheckGroundCollision();
        }

        // --------------------------------
    }
}
