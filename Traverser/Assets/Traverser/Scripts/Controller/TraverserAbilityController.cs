using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    [RequireComponent(typeof(TraverserCharacterController))]
    public class TraverserAbilityController : MonoBehaviour // Layer to control all of the object's abilities 
    {
        // --- Attributes ---

        TraverserAbility currentAbility;
        TraverserCharacterController controller;
        TraverserAnimationController animationController;
        TraverserAnimationController.AnimatorParameters animatorParameters;

        // --------------------------------

        // --- Basic methods ---
        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
            animationController = GetComponent<TraverserAnimationController>();

            // --- Set animator parameters --- 
            animationController.InitializeAnimatorParameters(ref animatorParameters);
        }

        public void Update()
        {
            if (!controller.isActiveAndEnabled)
                return;

            bool isEnabled = currentAbility == null ? false : currentAbility.IsAbilityEnabled();

            // --- Keep updating our current ability ---
            if (currentAbility != null && isEnabled)
            {
                currentAbility = currentAbility.OnUpdate(Time.deltaTime);
            }

            // --- If no ability is in control, look for one ---
            if (currentAbility == null || !isEnabled)
            {
                // MYTODO: Order of update is important, it would be wise to add a priority to abilities,
                // instead of following the arbitrary order in which they were added as components

                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                {
                    if (!ability.IsAbilityEnabled())
                        continue;

                    // An ability can either return "null" or a reference to an ability.
                    // A "null" result signals that this ability doesn't require control.
                    // Otherwise the returned ability (which might be different from the
                    // one that we call "OnUpdate" on) will be the one that gains control.
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
                animatorParameters.Speed = math.length(controller.targetVelocity);
                animatorParameters.Heading = controller.targetHeading;

                //Debug.Log(animatorParameters.Heading);
                animationController.UpdateAnimator(ref animatorParameters);
            }

        }

        private void FixedUpdate()
        {
            if (!controller.isActiveAndEnabled)
                return;

            bool isEnabled = currentAbility == null ? false : currentAbility.IsAbilityEnabled();

            // --- Keep updating our current ability ---
            if (currentAbility != null && isEnabled)
            {
                currentAbility = currentAbility.OnFixedUpdate(Time.fixedDeltaTime);
            }

            // --- If no ability is in control, look for one ---
            if (currentAbility == null || !isEnabled)
            {
                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
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

        public void OnAnimatorMove()
        {
            if (!controller.isActiveAndEnabled)
                return;

            // --- After all animations are evaluated, perform movement ---
            if (currentAbility == null)
                return;

            bool isEnabled = currentAbility.IsAbilityEnabled();

            // --- Let abilities modify motion ---
            if (currentAbility is TraverserAbilityAnimatorMove abilityAnimatorMove)
            {
                if (isEnabled)
                    abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            Assert.IsTrue(controller != null);

            controller.ForceMove(controller.targetPosition);
            

            // --- Let abilities apply final changes to motion, if needed ---
            if (isEnabled)
                currentAbility.OnPostUpdate(Time.deltaTime);
        }

        // --------------------------------
    }
}

