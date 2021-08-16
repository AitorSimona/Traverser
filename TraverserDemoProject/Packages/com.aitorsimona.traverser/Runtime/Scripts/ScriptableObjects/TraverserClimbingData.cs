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
        public TraverserTransition.TraverserTransitionData dropDownTransitionData;
        public TraverserTransition.TraverserTransitionData HopUpTransitionData;
        public TraverserTransition.TraverserTransitionData HopRightTransitionData;
        public TraverserTransition.TraverserTransitionData HopLeftTransitionData;
        public TraverserTransition.TraverserTransitionData HopDownTransitionData;

        [Header("Animations")]
        public string fallTransitionAnimation;
        public string locomotionOnAnimation;
        public string ledgeIdleAnimation;
        public string fallLoopAnimation;
        public string ledgeRightAnimation;
        public string ledgeLeftAnimation;
        public string ledgeCornerRightAnimation;
        public string ledgeCornerLeftAnimation;
        public string pullUpAnimation;
        public string dismountAnimation;
        public string JumpBackAnimation;

    }
}
