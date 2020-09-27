using Unity.Kinematica;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.SnapshotDebugger;


[RequireComponent(typeof(MovementController))]
public class AbilityController : Kinematica
{
    Ability currentAbility;

    public virtual new void Update()
    {
        // --- Keep updating our current ability ---
        if (currentAbility != null)
        {
            currentAbility = currentAbility.OnUpdate(_deltaTime);
        }

        // --- If no ability is in control, look for one ---
        if (currentAbility == null)
        {

            // MYTODO: Order of update is important, it would be wise to add a priority to abilities,
            // instead of following the arbitrary order in which they were added as components

            // --- Iterate all abilities and update each one until one takes control ---
            foreach (Ability ability in GetComponents(typeof(Ability)))
            {
                // An ability can either return "null" or a reference to an ability.
                // A "null" result signals that this ability doesn't require control.
                // Otherwise the returned ability (which might be different from the
                // one that we call "OnUpdate" on) will be the one that gains control.
                Ability result = ability.OnUpdate(_deltaTime);

                // --- If an ability asks to take control, break ---
                if (result != null)
                {
                    currentAbility = result;
                    //AddAbilityDebugRecord(currentAbility);
                    break;
                }
            }
        }
        //else
        //{
        //    AddAbilityDebugRecord(currentAbility);
        //}

        base.Update();
    }

    public override void OnAnimatorMove()
    {
        // --- After all animations are evaluated, perform movement ---

        ref MotionSynthesizer synthesizer = ref Synthesizer.Ref;

        // --- Let abilities modify motion ---
        if (currentAbility is AbilityAnimatorMove abilityAnimatorMove)
        {
            abilityAnimatorMove.OnAbilityAnimatorMove();
        }

        MovementController controller = GetComponent<MovementController>();
        Assert.IsTrue(controller != null);

        // --- Move and update the controller ---
        float3 controllerPosition = controller.Position;
        float3 desiredLinearDisplacement = synthesizer.WorldRootTransform.t - controllerPosition;
        controller.Move(desiredLinearDisplacement);
        controller.Tick(Debugger.instance.deltaTime);

        // --- Move the game object ---
        AffineTransform worldRootTransform = AffineTransform.Create(controller.Position, synthesizer.WorldRootTransform.q);
        synthesizer.SetWorldTransform(worldRootTransform, true);
        transform.position = worldRootTransform.t;
        transform.rotation = worldRootTransform.q;
    }

}

