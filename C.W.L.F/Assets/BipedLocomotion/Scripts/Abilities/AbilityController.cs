﻿using Unity.Kinematica;
using Unity.Mathematics;

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

using Unity.SnapshotDebugger;

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

}
