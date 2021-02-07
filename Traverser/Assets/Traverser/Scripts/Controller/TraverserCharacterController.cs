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
            get => state.currentCollision.position;
            set => state.currentCollision.position = value;
        }
        public bool isGrounded
        {
            get => characterController.isGrounded;
        }

        public bool gravityEnabled = true;

        public ref TraverserCollision previous
        {
            get => ref state.previousCollision;
        }

        public ref TraverserCollision current
        {
            get => ref state.currentCollision;
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

        public void Move(float3 displacement)
        {

            state.desiredDisplacement += displacement;



            //// Update previous collision
            //state.previousCollision.CopyFrom(ref state.currentCollision);

            //// Apply gravity
            //motion.y += Physics.gravity.y * Time.deltaTime;

            //// Move
            //CollisionFlags flags = characterController.Move(Vector3.zero);
            //characterController.SimpleMove(motion);
            //// Update state
            //state.transform = characterController.transform;

            //return flags;
        }

        public void MoveTo(float3 position)
        {
            Move(position - Position);
        }

        public void Tick(float deltaTime)
        {
            UpdateVelocity(deltaTime);

            state.previousCollision.CopyFrom(ref state.currentCollision);
            state.currentCollision.Reset();
            state.currentCollision.position = state.previousCollision.position;

            UpdateMovement(deltaTime);
        }

        void UpdateVelocity(float deltaTime)
        {

            float3 verticalAccumulatedVelocity =
               Missing.project(state.accumulatedVelocity, math.up());

           if (math.dot(math.normalize(verticalAccumulatedVelocity), math.up()) <= 0.0f)
           {
                float3 lateralAccumulatedVelocity =
                   state.accumulatedVelocity - verticalAccumulatedVelocity;

               state.accumulatedVelocity = lateralAccumulatedVelocity;
           }
            
        }

        void UpdateMovement(float deltaTime)
        {
            state.currentCollision.kinematicDisplacement = state.desiredVelocity * deltaTime + state.desiredDisplacement;
            state.desiredDisplacement = Vector3.zero;

            //if (gravityEnabled)
            //{
            //    float3 gravity = Physics.gravity;
            //    state.accumulatedVelocity += gravity * deltaTime;
            //    state.currentCollision.dynamicsDisplacement = state.accumulatedVelocity * deltaTime;
            //}

            //if(characterController.isGrounded)
            //    state.kinematicDisplacement.y += transform.position.y - state.currentCollision.position.y;

            //float3 verticalDisplacement = Missing.project(state.currentCollision.dynamicsDisplacement, math.up());
            //float verticalDisplacementDot = math.dot(verticalDisplacement, math.up());

            //if (state.previousCollision.isGrounded && verticalDisplacementDot < 0.0f)
            //{
            //    state.currentCollision.dynamicsDisplacement -= verticalDisplacement;
            //    verticalDisplacement = float3.zero;
            //}

            //Debug.Log(state.currentCollision.dynamicsDisplacement);

            float3 desiredDisplacement = state.currentCollision.kinematicDisplacement + state.currentCollision.dynamicsDisplacement;

            //---React to collisions ---

            //if (characterController.detectCollisions)
            //{
            //    if (CastCollisions(Position, ref desiredDisplacement))
            //    {
            //        state.currentCollision.isColliding = true;
            //    }
            //}

            // --- Update position and velocity ---
            //state.currentCollision.position.y = transform.position.y;
            //Debug.Log(Position);
            //characterController.m
            float3 finalPosition = Position + desiredDisplacement;
            state.currentCollision.position = finalPosition;

            state.currentCollision.velocity = (finalPosition - state.previousCollision.position) / deltaTime;


            //if (state.currentCollision.isGrounded)
            //{
            //    float3 verticalAccumulatedVelocity =
            //        Missing.project(state.accumulatedVelocity, math.up());

            //    state.accumulatedVelocity -= verticalAccumulatedVelocity;
            //}

        }

        RaycastHit[] raycastHits = new RaycastHit[15];
        float3 startPosition;
        float3 endPosition;

        bool CastCollisions(float3 position, ref float3 displacement)
        {
            startPosition = position + Missing.Convert(characterController.center) - Missing.up * (characterController.height * 0.5f - characterController.radius);
            endPosition = position + Missing.Convert(characterController.center) + Missing.up * (characterController.height * 0.5f - characterController.radius);

            float3 normalizedDisplacement = math.normalizesafe(displacement);

            // Note we do not care about triggers
            int numContacts = TraverserCollisionLayer.CastCapsuleCollisions(startPosition, endPosition, characterController.radius,
                normalizedDisplacement, ref raycastHits, math.length(displacement), TraverserCollisionLayer.EnvironmentCollisionMask, QueryTriggerInteraction.Ignore);

            GameObject.Find("Capsule").transform.position = position;


            if (numContacts > 0)
            {
                ref RaycastHit result = ref raycastHits[0];

                for (int i = 0; i < numContacts; i++)
                {
                    float epsilon = 0.0001f;

                    if (math.dot(normalizedDisplacement, raycastHits[i].normal) > -epsilon)
                    {
                        continue;
                    }

                    if (raycastHits[i].distance < result.distance)
                    {
                        result = raycastHits[i];
                    }
                }

                state.currentCollision.collider = result.collider;
                state.currentCollision.colliderContactPoint = result.point;
                state.currentCollision.colliderContactNormal = result.normal;

                float3 adjustedDisplacement = float3.zero;
                float extraSpacing = 0.001f;

                if (result.distance > extraSpacing)
                {
                  
                }

                adjustedDisplacement =
                      math.normalizesafe(displacement) *
                          math.min(result.distance -
                              extraSpacing, math.length(displacement));
                //else if (result.distance < extraSpacing)
                //{
                //    adjustedDisplacement =
                //        math.normalizesafe(result.point - result.contactOrigin) *
                //            (result.distance - extraSpacing);
                //}
                //else if (result.distance < extraSpacing)
                //{
                //    adjustedDisplacement =
                //        math.normalizesafe(result.point - result.contactOrigin) *
                //            (result.distance - extraSpacing);
                //}

                displacement = adjustedDisplacement;

                //if (result.collider.gameObject.name != "Ground")
                //{
                //    Debug.Log("Collided with:");
                //    Debug.Log(result.collider.gameObject.name);
                //}

                return true;
            }


            return false;
        }

        public void ForceMove(Vector3 position)
        {

            characterController.Move(position - transform.position);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            //if (hit.gameObject.name != "Ground")
            //{
            //    Debug.Log("Collided with:");
            //    Debug.Log(hit.gameObject.name);
            //}
            state.previousCollision.CopyFrom(ref state.currentCollision);
            state.currentCollision.colliderContactPoint = hit.point;
            state.currentCollision.colliderContactNormal = hit.normal;
            state.currentCollision.collider = hit.collider;
            state.currentCollision.isColliding = true;
            //state.currentCollision
        }
    }
}
