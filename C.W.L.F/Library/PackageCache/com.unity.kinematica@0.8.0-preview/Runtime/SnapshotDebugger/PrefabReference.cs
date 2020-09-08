using UnityEngine;

namespace Unity.SnapshotDebugger
{
    [CreateAssetMenu(menuName = "Prefab Reference")]
    internal class PrefabReference : ScriptableObject
    {
        public GameObject gameObject;
    }
}
