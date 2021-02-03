using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    //[RequireComponent(typeof(MovementController))]
    public class TraverserAbilityController : MonoBehaviour
    {
        // --- Attributes ---
        private TraverserAbility currentAbility;
        private CharacterController controller;

        // -------------------------------------------------

        // --- Basic methods ---

        // Start is called before the first frame update
        void Start()
        {
            controller = GetComponent<CharacterController>();

        }

        // Update is called once per frame
        void Update()
        {
            // --- Keep updating our current ability ---
            if (currentAbility != null)
            {
                currentAbility = currentAbility.OnUpdate(Time.deltaTime);
            }

            // --- If no ability is in control, look for one ---
            if (currentAbility == null)
            {

                // MYTODO: Order of update is important, it would be wise to add a priority to abilities,
                // instead of following the arbitrary order in which they were added as components

                // --- Iterate all abilities and update each one until one takes control ---
                foreach (TraverserAbility ability in GetComponents(typeof(TraverserAbility)))
                {

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
        }

        public void OnAnimatorMove()
        {
            // --- After all animations are evaluated, perform movement ---

            // --- Let abilities modify motion ---
            if (currentAbility is TraverserAbilityAnimatorMove abilityAnimatorMove)
            {
                abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            Assert.IsTrue(controller != null);

            // --- Move and update the controller ---
            float3 controllerPosition = controller.transform.position;
            float3 desiredPosition = controller.transform.position + transform.forward * Time.deltaTime;
            float3 desiredLinearDisplacement = desiredPosition - controllerPosition;
            controller.Move(desiredLinearDisplacement);

            // --- Move the game object ---
            //AffineTransform worldRootTransform = AffineTransform.Create(controller.Position, synthesizer.WorldRootTransform.q);
            //synthesizer.SetWorldTransform(worldRootTransform, true);
            //transform.position = worldRootTransform.t;
            //transform.rotation = worldRootTransform.q;

            // --- Let abilities apply final changes to motion, if needed ---
            if (currentAbility != null /*&& component.enabled*/)
                currentAbility.OnPostUpdate(Time.deltaTime);
        }

        // -------------------------------------------------
    }
}
