using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserFreeHangData")]

    public class TraverserFreeHangData : ScriptableObject
    {
        // --- Contains all data required by the procedurally animated free hang ---
        public Vector3 hipsRotationOffset;
        public Vector3 hipsPositionOffset;

        public Vector3 spinePositionOffset;
        public Vector3 aimTargetPositionOffset;

        public Vector3 legsPositionOffset;
        public Vector3 legsRotationOffset;
    }
}

