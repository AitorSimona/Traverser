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

        [Tooltip("Reference to the skeleton's parent. The controller positions the skeleton at the skeletonRef's position. Used to kill animation's root motion.")]
        public Transform skeleton;
        [Tooltip("Reference to the skeleton's reference position. A transform that follows the controller's object motion, with an offset to the bone position (f.ex hips).")]
        public Transform skeletonRef;

        // --------------------------------

        // --- Basic methods ---
        public void OnEnable()
        {
            controller = GetComponent<TraverserCharacterController>();
        }
        
        public void Update()
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
                    //SnapshotProvider component = ability as SnapshotProvider;

                    //if (!component.enabled)
                    //    continue;

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
                //if (component.enabled)
                abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            Assert.IsTrue(controller != null);

            controller.ForceMove(controller.realPosition);

            // --- Let abilities apply final changes to motion, if needed ---
            if (currentAbility != null /*&& component.enabled*/)
                currentAbility.OnPostUpdate(Time.deltaTime);
        }

        private void LateUpdate()
        {
            // --- Move all the skeleton to the character's position ---
            skeleton.position = skeletonRef.position;
        }

        // --------------------------------
    }
}

