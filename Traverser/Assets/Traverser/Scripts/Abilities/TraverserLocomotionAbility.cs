using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public class TraverserLocomotionAbility : MonoBehaviour, TraverserAbility
    {
        // --- Attributes ---
        private CharacterController controller;
        private TraverserAbilityController abilityController;

        // -------------------------------------------------

        // --- World interactable elements ---
        //LedgeObject.LedgeGeometry ledgeGeometry;

        //bool isBraking = false;
        //bool preFreedrop = true;
        //float desiredLinearSpeed => InputLayer.capture.run ? desiredSpeedFast : desiredSpeedSlow;
        //float distance_to_fall = 3.0f; // initialized to maxFallPredictionDistance

        // -------------------------------------------------

        // --- Basic Methods ---

        public void OnEnable()
        {
            abilityController = GetComponent<TraverserAbilityController>();
            controller = GetComponent<CharacterController>();
            //InputLayer.capture.movementDirection = Missing.forward;
            //InputLayer.capture.moveIntensity = 0.0f;

            // --- Initialize arrays ---
            //ledgeGeometry = LedgeObject.LedgeGeometry.Create();
            //preFreedrop = freedrop;
        }

        // -------------------------------------------------

        // --- Ability class methods ---

        public bool OnContact(Transform contactTransform, float deltaTime)
        {
            return this;
        }

        public bool OnDrop(float deltaTime)
        {
            return this;
        }

        public TraverserAbility OnPostUpdate(float deltaTime)
        {
            return this;
        }

        public TraverserAbility OnUpdate(float deltaTime)
        {
            TraverserInputLayer.capture.UpdateLocomotion();
            abilityController.AccelerateMovement(TraverserInputLayer.capture.movementDirection*abilityController.maxMovementAcceleration);



            return this;
        }

        // -------------------------------------------------
    }
}
