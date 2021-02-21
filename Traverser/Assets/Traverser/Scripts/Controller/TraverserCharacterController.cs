using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        // --- Attributes ---
        [Tooltip("If active, debug utilities will be shown (information/geometry draw). Select the object to show geometry.")]
        public bool debugDraw = false;

        [Header("Controller")]
        [Tooltip("Whether or not gravity will be applied to the controller.")]
        public bool gravityEnabled = true;
        [Tooltip("How much will given displacement be increased, bigger stepping increases prediction reach at the cost of precision (void space). Can be overwriten by abilities.")]
        [Range(1.0f, 10.0f)]
        public float stepping = 1.0f;


        [Header("Collision")]
        [Tooltip("Radius of the ground collision check sphere.")]
        [Range(0.1f, 1.0f)]
        public float groundProbeRadius = 0.25f;

        [Tooltip("If enabled, character will be snapped to current ground, preventing it from falling down.")]
        public bool groundSnap = false;

        [Tooltip("The max length of the ray used to snap the character to current ground.")]
        public float groundSnapRayDistance = 0.1f;


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

        // --- The current state's velocity ---
        [HideInInspector]
        public float3 targetVelocity;

        // --- The current state's rotation ---
        [HideInInspector]
        public float targetHeading;

        // --- The current state's previous collision situation ---
        public ref TraverserCollision previous { get => ref state.previousCollision; }

        // --- The current state's actual collision situation ---
        public ref TraverserCollision current { get => ref state.currentCollision; }

        // --- Character controller's capsule collider height ---
        public float capsuleHeight { get => characterController.height; }

        // --- Whether or not the character controller's capsule collider is grounded ---
        public bool isGrounded { get => current.isGrounded; }

        // --- Whether or not the character controller reacts to collisions ---
        public bool collisionEnabled { get => characterController.detectCollisions; set => characterController.detectCollisions = value; }

        public TraverserAffineTransform lastContactTransform = TraverserAffineTransform.Create(float3.zero, quaternion.identity);

        // --------------------------------

        // --- Simulation methods ---

        public void Snapshot()  
        {
            // --- Check current state before simulation start --- 
            state.currentCollision.velocity = characterController.velocity / stepping;

            if (characterController.detectCollisions)
                CheckGroundCollision();

            // --- Copy current state to snapshot so we can rewind time ---
            snapshotState.CopyFrom(ref state);
        }

        public void Rewind()
        {
            // --- Store simulation's last position ---
            lastPosition = position;

            // --- Shrink geomtry debug lists if iteration number decreased ---
            if (debugDraw)
            {
                if(probePositions.Count > simulationCounter)
                    probePositions.RemoveRange(simulationCounter, probePositions.Count - simulationCounter);
                if (planePositions.Count > simulationCounter)
                    planePositions.RemoveRange(simulationCounter, planePositions.Count - simulationCounter);
                if (capsulePositions.Count > simulationCounter)
                    capsulePositions.RemoveRange(simulationCounter, capsulePositions.Count - simulationCounter);
            }

            // --- Reset counter ---
            simulationCounter = 0;          

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

        public void ForceRotate(float newHeading)
        {
            targetHeading = newHeading;
            transform.rotation = transform.rotation * Quaternion.AngleAxis(targetHeading, Vector3.up);
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

            // --- If speed value drops to nearly zero, but not 0, force it to zero ---
            if (math.length(state.currentCollision.velocity) < Vector3.one.magnitude/10.0f)
                state.currentCollision.velocity = float3.zero;

            // --- If on simulation's first tick, store position ---
            if (simulationCounter == 0)
            {
                // --- This are the target position, velocity and yaw rotation (heading) of the current frame ---
                targetPosition = transform.position;
                targetVelocity = state.currentCollision.velocity;
                //targetHeading = Vector3.SignedAngle(transform.forward, math.normalizesafe(targetVelocity), transform.up) * deltaTime;
            }

            simulationCounter++;
        }


        void UpdateMovement(float deltaTime)
        {
            // --- Compute kinematic displacement (from player input) ---
            state.currentCollision.kinematicDisplacement = state.desiredDisplacement * stepping;
            state.desiredDisplacement = Vector3.zero;

            // --- Apply gravity ---
            if (gravityEnabled)
            {
                float3 gravity = Physics.gravity;
                state.currentCollision.dynamicsDisplacement = gravity * deltaTime * stepping;
            }

            // --- Compute final displacement/position and move character controller ---
            float3 desiredDisplacement = state.currentCollision.kinematicDisplacement + state.currentCollision.dynamicsDisplacement;
            float3 finalPosition = position + desiredDisplacement;
            ForceMove(finalPosition);
            state.currentCollision.velocity = characterController.velocity / stepping;

            // --- Cast ground probe (ground collision detection) ---
            if (characterController.detectCollisions)
                CheckGroundCollision();        

            // --- Add capsule positions to geometry debug list ---
            if (debugDraw)
            {
                if (capsulePositions.Count == simulationCounter)
                    capsulePositions.Add(characterController.transform.position + Vector3.up*characterController.height/2.0f);
                else
                    capsulePositions[simulationCounter] = characterController.transform.position + Vector3.up * characterController.height / 2.0f;
            }
        }

        // --------------------------------
    }
}
