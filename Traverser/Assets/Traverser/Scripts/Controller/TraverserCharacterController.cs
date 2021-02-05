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

           var verticalAccumulatedVelocity =
               Missing.project(state.accumulatedVelocity, math.up());

           if (math.dot(math.normalize(verticalAccumulatedVelocity), math.up()) <= 0.0f)
           {
               var lateralAccumulatedVelocity =
                   state.accumulatedVelocity - verticalAccumulatedVelocity;

               state.accumulatedVelocity = lateralAccumulatedVelocity;
           }
            
        }

        void UpdateMovement(float deltaTime)
        {
            var finalDisplacement = state.desiredVelocity * deltaTime + state.desiredDisplacement;
            state.desiredDisplacement = Vector3.zero;

           // if (gravityEnabled)
            //{
                //float3 gravity = Physics.gravity;
                //state.accumulatedVelocity += gravity * deltaTime;
                //state.desiredDisplacement = state.accumulatedVelocity * deltaTime;
            //}


            var finalPosition = Position + finalDisplacement;
            state.currentCollision.position = finalPosition;
            state.currentCollision.velocity = (finalPosition - state.previousCollision.position) / deltaTime;

            //if (state.currentCollision.isGrounded)
            //{
                var verticalAccumulatedVelocity =
                    Missing.project(state.accumulatedVelocity, math.up());

                state.accumulatedVelocity -= verticalAccumulatedVelocity;
            //}

            //characterController.m
        }
    }
}
