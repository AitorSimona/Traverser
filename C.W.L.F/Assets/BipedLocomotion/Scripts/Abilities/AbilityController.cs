using Unity.Kinematica;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

using Unity.SnapshotDebugger;

namespace CWLF
{
    [RequireComponent(typeof(MovementController))]
    public class AbilityController : Kinematica
    {
        Ability currentAbility;

        public virtual new void Update()
        {
            if (currentAbility != null)
            {
                currentAbility = currentAbility.OnUpdate(_deltaTime);
            }

            if (currentAbility == null)
            {
                // Now iterate all abilities and update each one in turn.
                foreach (Ability ability in GetComponents(typeof(Ability)))
                {
                    // An ability can either return "null" or a reference to an ability.
                    // A "null" result signals that this ability doesn't require control.
                    // Otherwise the returned ability (which might be different from the
                    // one that we call "OnUpdate" on) will be the one that gains control.
                    Ability result = ability.OnUpdate(_deltaTime);

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
            ref MotionSynthesizer synthesizer = ref Synthesizer.Ref;

            if (currentAbility is AbilityAnimatorMove abilityAnimatorMove)
            {
                abilityAnimatorMove.OnAbilityAnimatorMove();
            }

            MovementController controller = GetComponent<MovementController>();

            Assert.IsTrue(controller != null);

            float3 controllerPosition = controller.Position;

            float3 desiredLinearDisplacement = synthesizer.WorldRootTransform.t - controllerPosition;

            controller.Move(desiredLinearDisplacement);
            controller.Tick(Debugger.instance.deltaTime);

            var worldRootTransform = AffineTransform.Create(controller.Position, synthesizer.WorldRootTransform.q);

            synthesizer.SetWorldTransform(worldRootTransform, true);

            transform.position = worldRootTransform.t;
            transform.rotation = worldRootTransform.q;
        }

    }
}
