using UnityEngine;

public class TraverserAnimationController : MonoBehaviour
{
    // --- Attributes ---

    public struct AnimatorParameters
    {
        public bool Move;
        public float Speed;
        public float Heading;
    }

    [Header("Animation")]
    [Tooltip("Reference to the skeleton's parent. The controller positions the skeleton at the skeletonRef's position. Used to kill animation's root motion.")]
    public Transform skeleton;
    [Tooltip("Reference to the skeleton's reference position. A transform that follows the controller's object motion, with an offset to the bone position (f.ex hips).")]
    public Transform skeletonRef;

    // --------------------------------

    // --- Basic Methods ---

    public void UpdateAnimator(ref AnimatorParameters parameters)
    {
        // --- Update animator with the given parameter's values ---


    }

    private void LateUpdate()
    {
        // --- Move all the skeleton to the character's position ---
        skeleton.position = skeletonRef.position;
    }

    // --------------------------------
}
