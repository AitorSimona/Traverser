using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserParkourData")]

    public class TraverserParkourData : ScriptableObject
    {
        // --- Contains all data required by the climbing ability ---

        [Header("Transitions")]
        public TraverserTransition.TraverserTransitionData vaultTableJogTransitionData;
        public TraverserTransition.TraverserTransitionData vaultTableRunTransitionData;

        public TraverserTransition.TraverserTransitionData climbPlatformWalkTransitionData;
        public TraverserTransition.TraverserTransitionData climbPlatformJogTransitionData;
        public TraverserTransition.TraverserTransitionData climbPlatformRunTransitionData;

        public TraverserTransition.TraverserTransitionData dropPlatformWalkTransitionData;
        public TraverserTransition.TraverserTransitionData dropPlatformJogTransitionData;
        public TraverserTransition.TraverserTransitionData dropPlatformRunTransitionData;

        public TraverserTransition.TraverserTransitionData vaultLedgeWalkTransitionData;
        public TraverserTransition.TraverserTransitionData vaultLedgeJogTransitionData;
        public TraverserTransition.TraverserTransitionData vaultLedgeRunTransitionData;

        public TraverserTransition.TraverserTransitionData slideTunnelJogTransitionData;
        public TraverserTransition.TraverserTransitionData slideTunnelRunTransitionData;

        public TraverserTransition.TraverserTransitionData ledgeToLedgeTransitionData;

    }
}
