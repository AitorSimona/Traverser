using Unity.Mathematics;

// --- All abilities derive from this base interface (class with no implementations) ---

namespace Traverser
{
    public interface TraverserAbility
    {
        //
        // Called once per frame for each ability to update the synthesizers trajectory.
        // The resulting ability reference indicates which ability gains
        // ownership of the policy.
        //

        TraverserAbility OnUpdate(float deltaTime);

        TraverserAbility OnFixedUpdate(float deltaTime);

        //
        // Called from another ability to indicate that a predicted future
        // root transform makes contact with the environment (subject to the
        // collision shapes of the character). The return value indicates
        // if the called ability wants to handle the contact (and will subsequently
        // gain control over the policy).
        //

        bool OnContact(float3 contactTransform, float deltaTime);

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

        TraverserAbility OnPostUpdate(float deltaTime);

        bool IsAbilityEnabled();
    }

    // -------------------------------------------------

    // --- Abilities may implement this to further modify motion after animations have taken place ---
    public interface TraverserAbilityAnimatorMove
    {
        void OnAbilityAnimatorMove();
    }

    // -------------------------------------------------
}

