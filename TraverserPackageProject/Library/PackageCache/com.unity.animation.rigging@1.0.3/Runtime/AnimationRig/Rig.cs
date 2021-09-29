using System.Collections.Generic;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The Rig component is used to group constraints under its GameObject local hierarchy.
    /// </summary>
    [DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Setup/Rig")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/index.html")]
    public class Rig : MonoBehaviour, IRigEffectorHolder
    {
        [SerializeField, Range(0f, 1f)]
        private float m_Weight = 1f;

        /// <summary>The weight given to this rig and its associated constraints. This is a value in between 0 and 1.</summary>
        public float weight { get => m_Weight; set => m_Weight = Mathf.Clamp01(value); }

#if UNITY_EDITOR
        [SerializeField] private List<RigEffectorData> m_Effectors = new List<RigEffectorData>();

        /// <inheritdoc />
        public IEnumerable<RigEffectorData> effectors { get => m_Effectors; }

        /// <inheritdoc />
        public void AddEffector(Transform transform, RigEffectorData.Style style)
        {
            var effector = new RigEffectorData();
            effector.Initialize(transform, style);

            m_Effectors.Add(effector);
        }

        /// <inheritdoc />
        public void RemoveEffector(Transform transform)
        {
            m_Effectors.RemoveAll((RigEffectorData data) => data.transform == transform);
        }

        /// <inheritdoc />
        public bool ContainsEffector(Transform transform)
        {
            return m_Effectors.Exists((RigEffectorData data) => data.transform == transform);
        }
#endif
    }
}
