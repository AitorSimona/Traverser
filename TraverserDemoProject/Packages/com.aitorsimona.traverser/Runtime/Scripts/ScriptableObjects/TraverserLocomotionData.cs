using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserLocomotionData")]

    public class TraverserLocomotionData : ScriptableObject
    {
        // --- Contains all data required by the climbing ability ---

        [Header("Transitions")]
        public TraverserTransition.TraverserTransitionData FallToRollTransitionData;

        [Header("Animations")]
        public string fallTransitionAnimation;

    }
}

