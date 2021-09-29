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
        public TraverserTransition.TraverserTransitionData dismountTransitionData;
        public TraverserTransition.TraverserTransitionData pullUpTransitionData;
        public TraverserTransition.TraverserTransitionData jumpBackTransitionData;


        [Header("Animations")]
        public TraverserAnimationController.AnimationData fallTransitionAnimation;
        public TraverserAnimationController.AnimationData locomotionOnAnimation;
        public TraverserAnimationController.AnimationData ledgeIdleAnimation;
        public TraverserAnimationController.AnimationData fallLoopAnimation;
        public TraverserAnimationController.AnimationData ledgeRightAnimation;
        public TraverserAnimationController.AnimationData ledgeLeftAnimation;
    }
}
