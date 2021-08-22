using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserClimbingData")]

    public class TraverserClimbingData : ScriptableObject
    {
        // --- Contains all data required by the climbing ability ---

        [Header("Transitions")]
        public TraverserTransition.TraverserTransitionData mountTransitionData;
        public TraverserTransition.TraverserTransitionData jumpHangTransitionData;
        public TraverserTransition.TraverserTransitionData jumpHangShortTransitionData;
        public TraverserTransition.TraverserTransitionData dropDownTransitionData;
        public TraverserTransition.TraverserTransitionData HopUpTransitionData;
        public TraverserTransition.TraverserTransitionData HopRightTransitionData;
        public TraverserTransition.TraverserTransitionData HopLeftTransitionData;
        public TraverserTransition.TraverserTransitionData HopDownTransitionData;

        [Header("Animations")]

        public TraverserAnimationController.AnimationData fallTransitionAnimation;
        public TraverserAnimationController.AnimationData locomotionOnAnimation;
        public TraverserAnimationController.AnimationData ledgeIdleAnimation;
        public TraverserAnimationController.AnimationData fallLoopAnimation;
        public TraverserAnimationController.AnimationData ledgeRightAnimation;
        public TraverserAnimationController.AnimationData ledgeLeftAnimation;
        public TraverserAnimationController.AnimationData ledgeCornerRightAnimation;
        public TraverserAnimationController.AnimationData ledgeCornerLeftAnimation;
        public TraverserAnimationController.AnimationData pullUpAnimation;
        public TraverserAnimationController.AnimationData dismountAnimation;
        public TraverserAnimationController.AnimationData jumpBackAnimation;
    }
}
