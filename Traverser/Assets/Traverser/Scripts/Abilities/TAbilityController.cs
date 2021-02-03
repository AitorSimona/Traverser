using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Traverser
{
    //[RequireComponent(typeof(MovementController))]
    public class TAbilityController : MonoBehaviour
    {
        // --- Attributes ---
        TAbility currentAbility;
        CharacterController controller;

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
                foreach (TAbility ability in GetComponents(typeof(TAbility)))
                {

                    // An ability can either return "null" or a reference to an ability.
                    // A "null" result signals that this ability doesn't require control.
                    // Otherwise the returned ability (which might be different from the
                    // one that we call "OnUpdate" on) will be the one that gains control.
                    TAbility result = ability.OnUpdate(Time.deltaTime);

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

            //ref MotionSynthesizer synthesizer = ref Synthesizer.Ref;
            //SnapshotProvider component = currentAbility as SnapshotProvider;

            // --- Let abilities modify motion ---
            if (currentAbility is TAbilityAnimatorMove abilityAnimatorMove)
            {
                //if (component.enabled)
                    abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            //Assert.IsTrue(controller != null);

            // --- Move and update the controller ---
            //float3 controllerPosition = controller.Position;
            //float3 desiredLinearDisplacement = synthesizer.WorldRootTransform.t - controllerPosition;
            //controller.Move(desiredLinearDisplacement);
            //controller.Tick(Time.deltaTime);

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