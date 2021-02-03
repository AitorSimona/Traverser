

using UnityEngine;

namespace Traverser
{
    public interface TAbility 
    {
        //
        // Called once per frame for each ability to update the synthesizers trajectory.
        // The resulting ability reference indicates which ability gains
        // ownership of the policy.
        //

        TAbility OnUpdate(float deltaTime);

        //
        // Called from another ability to indicate that a predicted future
        // root transform makes contact with the environment (subject to the
        // collision shapes of the character). The return value indicates
        // if the called ability wants to handle the contact (and will subsequently
        // gain control over the policy).
        //

        bool OnContact(Transform contactTransform, float deltaTime);

        //
        // Called from another ability to indicate that a predicted future
        // root transform will no longer be grounded (subject to the collision
        // shapes of the character). The return value indicates if the called
        // ability want to handle this situation (and will subsequently gain
        // control over the policy).
        //

        bool OnDrop(float deltaTime);

        //
        //Called from ablity controller so abilities can apply last moment modifications to motion
        //

        TAbility OnPostUpdate(float deltaTime);
    }

    // -------------------------------------------------

    // --- Abilities may implement this to further modify motion after animations have taken place ---
    public interface TAbilityAnimatorMove
    {
        void OnAbilityAnimatorMove();
    }

    // -------------------------------------------------
}
