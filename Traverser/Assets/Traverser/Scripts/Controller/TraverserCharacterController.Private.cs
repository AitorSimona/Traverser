using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public partial class TraverserCharacterController : MonoBehaviour
    {

        // --- Wrapper to define a controller's state collision situation and movement information ---
        public struct TraverserCollision
        {
            // --- Attributes ---

            // --- The collider we just made contact with ---
            public Collider collider;

            // --- States if we are currently colliding ---
            public bool isColliding;

            // --- The point at which we collided with the current collider ---
            public float3 colliderContactPoint;

            // --- The current collider's normal direction ---
            public float3 colliderContactNormal;

            // --- Transform of the current ground, the object below the character ---
            public Collider ground;

            // --- States if the controller is grounded ---
            public bool isGrounded;

            // --- The controller's position (simulation) ---
            public float3 position;

            // --- The controller's velocity (simulation) ---
            public float3 velocity;

            // --- The controller's current kinematic based displacement ---
            public float3 kinematicDisplacement;

            // --- The controller's current dynamics based displacement ---
            public float3 dynamicsDisplacement;

            // --------------------------------

            // --- Basic methods ---

            internal static TraverserCollision Create()
            {
                return new TraverserCollision()
                {
                    position = float3.zero,
                    velocity = float3.zero,
                    collider = null,
                    isColliding = false,
                    colliderContactNormal = float3.zero,
                    colliderContactPoint = float3.zero,
                    ground = null,
                    isGrounded = false,
                    kinematicDisplacement = Vector3.zero,
                    dynamicsDisplacement = Vector3.zero,
                };
            }

            internal void Reset()
            {
                position = float3.zero;
                velocity = float3.zero;
                collider = null;
                isColliding = false;
                colliderContactNormal = float3.zero;
                colliderContactPoint = float3.zero;
                ground = null;
                isGrounded = false;
                kinematicDisplacement = float3.zero;
                dynamicsDisplacement = float3.zero;
            }

            internal void CopyFrom(ref TraverserCollision copyCollision)
            {
                collider = copyCollision.collider;
                isColliding = copyCollision.isColliding;
                colliderContactPoint = copyCollision.colliderContactPoint;
                colliderContactNormal = copyCollision.colliderContactNormal;
                ground = copyCollision.ground;
                position = copyCollision.position;
                velocity = copyCollision.velocity;
                isGrounded = copyCollision.isGrounded;
                kinematicDisplacement = copyCollision.kinematicDisplacement;
                dynamicsDisplacement = copyCollision.dynamicsDisplacement;
            }

            // --------------------------------
        }

        // --- Wrapper to define a controller's state, including previous and current collision situations ---
        public struct TraverserState
        {
            // --- Attributes ---

            // --- The state's previous collision situation --- 
            public TraverserCollision previousCollision;

            // --- The state's actual collision situation ---
            public TraverserCollision currentCollision;

            // --- The state's desired absolute displacement ---
            public float3 desiredDisplacement;

            // --------------------------------

            // --- Basic methods ---

            internal static TraverserState Create()
            {
                return new TraverserState()
                {
                    previousCollision = TraverserCollision.Create(),
                    currentCollision = TraverserCollision.Create(),
                    desiredDisplacement = float3.zero,
                };
            }

            internal void CopyFrom(ref TraverserState copyState)
            {
                previousCollision.CopyFrom(ref copyState.previousCollision);
                currentCollision.CopyFrom(ref copyState.currentCollision);
                desiredDisplacement = copyState.desiredDisplacement;
            }

            // --------------------------------
        }

        // --------------------------------

        // --- Attributes ---

        // --- Unity's character controller ---
        private CharacterController characterController;

        // --- Actual controller state ---
        private TraverserState state;

        // --- State snapshot to save current state before movement simulation ---
        private TraverserState snapshotState;

        // --------------------------------

        // --- Basic methods ---

        // Start is called before the first frame update
        void Start()
        {
            // --- Initialize controller ---
            state = TraverserState.Create();
            snapshotState = TraverserState.Create();
            characterController = GetComponent<CharacterController>();
            current.position = transform.position;
            targetPosition = transform.position;
        }

        // --------------------------------

        // --- Collisions ---

        // TODO: Move to collision layer 

        Collider[] hitColliders = new Collider[3];

        void CheckGroundCollision()
        {
            // --- Ground collision check ---
            float3 colliderPosition;
            int numColliders = Physics.OverlapSphereNonAlloc(position, groundProbeRadius, hitColliders, TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            if (numColliders > 0)
            {
                float minDistance = groundProbeRadius * 2.0f;
                int chosenGround = -1;

                for (int i = 0; i < numColliders; ++i)
                {
                    colliderPosition = hitColliders[i].transform.position;

                    if (math.distance(colliderPosition.y, position.y) < minDistance)
                        chosenGround = i;
                }

                if (chosenGround != -1)
                {
                    current.ground = hitColliders[chosenGround];
                    current.isGrounded = true;

                    Debug.Log("Ground is:");
                    Debug.Log(current.ground.gameObject.name);
                }
            }
        }

        // --------------------------------

        // --- Events ---

        void OnDrawGizmosSelected()
        {
            if (!showDebug)
                return;

            // Draw a yellow sphere at the transform's position
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, groundProbeRadius);
        }

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