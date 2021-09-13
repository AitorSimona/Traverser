using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserLocomotionData")]

    public class TraverserLocomotionData : ScriptableObject
    {
        // --- Contains all data required by the climbing ability ---

        [Header("Transitions")]
        public TraverserTransition.TraverserTransitionData fallToRollTransitionData;
        public TraverserTransition.TraverserTransitionData hardLandingTransitionData;

        [Header("Animations")]
        public TraverserAnimationController.AnimationData fallTransitionAnimation;
        public TraverserAnimationController.AnimationData jumpAnimation;
        public TraverserAnimationController.AnimationData jumpForwardAnimation;
        public TraverserAnimationController.AnimationData locomotionONAnimation;
        public TraverserAnimationController.AnimationData locomotionOFFAnimation;
        public TraverserAnimationController.AnimationData fallToLandAnimation;
        public TraverserAnimationController.AnimationData fallToRunAnimation;
    }
}

