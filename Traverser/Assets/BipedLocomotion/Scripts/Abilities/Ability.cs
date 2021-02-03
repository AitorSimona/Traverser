using Unity.Kinematica;
using Unity.Mathematics;

// --- All abilities derive from this base interface (class with no implementations) ---

namespace Traverser
{
    public interface Ability
    {
        //
        // Called once per frame for each ability to update the synthesizers trajectory.
        // The resulting ability reference indicates which ability gains
        // ownership of the policy.
        //

        Ability OnUpdate(float deltaTime);

        //
        // Called from another ability to indicate that a predicted future
        // root transform makes contact with the environment (subject to the
        // collision shapes of the character). The return value indicates
        // if the called ability wants to handle the contact (and will subsequently
        // gain control over the policy).
        //

        bool OnContact(ref MotionSynthesizer synthesizer, AffineTransform contactTransform, float deltaTime);

        //
        // Called from another ability to indicate that a predicted future
        // root transform will no longer be grounded (subject to the collision
        // shapes of the character). The return value indicates if the called
        // ability want to handle this situation (and will subsequently gain
        // control over the policy).
        //

        bool OnDrop(ref MotionSynthesizer synthesizer, float deltaTime);

        //
        //Called from ablity controller so abilities can apply last moment modifications to motion
        //

        Ability OnPostUpdate(float deltaTime);

    }

    // -------------------------------------------------

    // --- Abilities may implement this to further modify motion after animations have taken place ---
    public interface AbilityAnimatorMove
    {
        void OnAbilityAnimatorMove();
    }

    // -------------------------------------------------
}

