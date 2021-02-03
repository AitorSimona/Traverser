using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Traverser
{
    public class TLocomotionAbility : MonoBehaviour, TAbility
    {
        // --- Attributes ---
        private TLocomotionAbility copy;
        private CharacterController controller;

        // -------------------------------------------------

        // --- World interactable elements ---
        //LedgeObject.LedgeGeometry ledgeGeometry;

        //MovementController controller;

        bool isBraking = false;
        bool preFreedrop = true;

        // TODO: Remove from here
        //float desiredLinearSpeed => InputLayer.capture.run ? desiredSpeedFast : desiredSpeedSlow;
        float distance_to_fall = 3.0f; // initialized to maxFallPredictionDistance

        // -------------------------------------------------

        // --- Basic Methods ---

        public void OnEnable()
        {
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

        public TAbility OnPostUpdate(float deltaTime)
        {
            return this;
        }

        public TAbility OnUpdate(float deltaTime)
        {
            return this;
        }

        // -------------------------------------------------
    }
}
