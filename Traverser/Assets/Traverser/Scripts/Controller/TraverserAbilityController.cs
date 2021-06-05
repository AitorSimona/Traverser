using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserCharacterController))]
    public class TraverserAbilityController : MonoBehaviour // Layer to control all of the object's abilities 
    {
        // --- Private Variables ---

        private TraverserAbility currentAbility;
        private TraverserCharacterController controller;
        private TraverserAnimationController animationController;
        private TraverserAnimationController.AnimatorParameters animatorParameters;
        private TraverserAbility[] abilities;

        // --------------------------------

        // --- Basic methods ---
        public void Start()
        {
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();
            abilities = GetComponents<TraverserAbility>();

            Assert.IsTrue(controller != null);

            // --- Set animator parameters --- 
            animationController.InitializeAnimatorParameters(ref animatorParameters);
        }

        // MYTODO: If order of update is important, it would be wise to add a priority to abilities,
        // instead of following the arbitrary order in which they were added as components

        public void Update()
        {
            if (!controller.isActiveAndEnabled)
                return;

            // --- Let abilities poll for input ---
            foreach (TraverserAbility ability in abilities)
            {
                if (!ability.IsAbilityEnabled())
                    continue;

                ability.OnInputUpdate();
            }

            // --- Keep updating our current ability ---
            bool isEnabled = currentAbility == null ? false : currentAbility.IsAbilityEnabled();

            if (currentAbility != null && isEnabled)
                currentAbility = currentAbility.OnUpdate(Time.deltaTime);
            // --- If no ability is in control, look for one ---
            if (currentAbility == null || !isEnabled)
            {
                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in abilities)
                {
                    if (!ability.IsAbilityEnabled())
                        continue;

                    TraverserAbility result = ability.OnUpdate(Time.deltaTime);

                    // --- If an ability asks to take control, break ---
                    if (result != null)
                    {
                        currentAbility = result;
                        break;
                    }
                }
            }

            // --- Send updated animator parameters to animation controller ---
            if (animationController.isActiveAndEnabled)
            {
                animatorParameters.Move = TraverserInputLayer.GetMoveIntensity() > 0.0f;

                // --- We are not interested in Y speed, since then gravity would make us run in the animator! ---
                Vector2 speed;
                speed.x = controller.targetVelocity.x;
                speed.y = controller.targetVelocity.z;

                // TODO: Should we interpolate these values?

                animatorParameters.Speed = speed.magnitude;
                animatorParameters.Heading = controller.targetHeading;
                animationController.UpdateAnimator(ref animatorParameters);
            }

            // --- Perform movement and rotation, interpolate for smoothness ---
            if (!animationController.transition.isON)
            {
                controller.ForceMove(Vector3.Lerp(transform.position, transform.position + controller.targetDisplacement, Time.deltaTime / Time.fixedDeltaTime));
                controller.ForceRotate(Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.AngleAxis(controller.targetHeading, Vector3.up), Time.deltaTime / Time.fixedDeltaTime));

                //Debug.Log(Time.deltaTime / Time.fixedDeltaTime);
            }
        }

        // MYTODO: If order of update is important, it would be wise to add a priority to abilities,
        // instead of following the arbitrary order in which they were added as components

        private void FixedUpdate()
        {
            if (!controller.isActiveAndEnabled)
                return;

            bool isEnabled = currentAbility == null ? false : currentAbility.IsAbilityEnabled();

            // --- Keep updating our current ability ---
            if (currentAbility != null && isEnabled)
                currentAbility = currentAbility.OnFixedUpdate(Time.fixedDeltaTime);
            // --- If no ability is in control, look for one ---
            if (currentAbility == null || !isEnabled)
            {
                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in abilities)
                {
                    if (!ability.IsAbilityEnabled())
                        continue;

                    TraverserAbility result = ability.OnFixedUpdate(Time.fixedDeltaTime);

                    // --- If an ability asks to take control, break ---
                    if (result != null)
                    {
                        currentAbility = result;
                        break;
                    }
                }
            }
        }

        // --------------------------------

        // --- Utilites ---

        public bool isCurrent(TraverserAbility ability)
        {
            return currentAbility.Equals(ability);
        }

        // --------------------------------
    }
}

