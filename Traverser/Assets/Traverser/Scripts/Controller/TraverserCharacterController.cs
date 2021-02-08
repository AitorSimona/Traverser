using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


namespace Traverser
{
    [RequireComponent(typeof(CharacterController))]

    public partial class TraverserCharacterController : MonoBehaviour
    {
        public float3 realPosition;

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

        bool firstTick = true;

        public void Snapshot()
        {
            snapshotState.CopyFrom(ref state);
            firstTick = true;
        }

        public void Rewind()
        {
            float3 dummyPos = Position;
            dummyPos.y += characterController.height / 2;
            GameObject.Find("Capsule").transform.position = dummyPos;

            state.CopyFrom(ref snapshotState);
            characterController.enabled = false;
            transform.position = Position;
            //transform.rotation = state.transform.rotation;
            characterController.enabled = true;
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

            if (firstTick)
            {
                //Debug.Log(realPosition);
                realPosition = transform.position;
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
            ForceMove(finalPosition);
            //state.currentCollision.position = characterController.transform.position;
            state.currentCollision.velocity = (finalPosition - state.previousCollision.position) / deltaTime;
        }

        public void ForceMove(Vector3 position)
        {
            characterController.Move(position - transform.position);
            Position = transform.position;
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
        }
    }
}
