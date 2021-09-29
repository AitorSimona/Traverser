using System;
using UnityEngine.Serialization;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The OverrideRigLayer is used to override constraints normally evaluated by
    /// a specified Rig component.
    /// </summary>
    [Serializable]
    public class OverrideRigLayer : IRigLayer
    {
        [SerializeField] [FormerlySerializedAs("rig")] private Rig m_Rig;
        [SerializeField] [FormerlySerializedAs("active")] private bool m_Active = true;

        private IRigConstraint[] m_Constraints;
        private IAnimationJob[] m_Jobs;

        /// <summary>The Rig associated to the OverrideRigLayer</summary>
        public Rig rig { get => m_Rig; private set => m_Rig = value; }
        /// <summary>The active state. True if the OverrideRigLayer is active, false otherwise.</summary>
        public bool active { get => m_Active; set => m_Active = value; }
        /// <summary>The OverrideRigLayer name.</summary>
        public string name { get => (rig != null ? rig.gameObject.name : "no-name"); }
        /// <summary>The list of constraints associated with the OverrideRigLayer.</summary>
        public IRigConstraint[] constraints { get => isInitialized ? m_Constraints : null; }
        /// <summary>The list of jobs built from constraints associated with the OverrideRigLayer.</summary>
        public IAnimationJob[] jobs { get => isInitialized ? m_Jobs : null; }
        /// <summary>Returns true if OverrideRigLayer was initialized or false otherwise.</summary>
        /// <seealso cref="RigLayer.Initialize"/>
        public bool isInitialized { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rig">The rig represented by this override rig layer.</param>
        /// <param name="constraints">The constraints that override those of the rig.</param>
        /// <param name="active">The active state of the override rig layer.</param>
        public OverrideRigLayer(Rig rig, IRigConstraint[] constraints, bool active = true)
        {
            this.rig = rig;
            this.active = active;

            m_Constraints = constraints;
        }

        /// <summary>
        /// Initializes the OverrideRigLayer.  This will create animation jobs using
        /// the rig constraints provided to the OverrideRigLayer.
        /// </summary>
        /// <param name="animator">The Animator used to animate the RigLayer constraints.</param>
        /// <returns>True if RigLayer was initialized properly, false otherwise.</returns>
        public bool Initialize(Animator animator)
        {
            if (isInitialized)
                return true;

            if (rig == null)
                return false;

            if (m_Constraints == null || m_Constraints.Length == 0)
                return false;

            m_Jobs = new IAnimationJob[m_Constraints.Length];
            for (int i = 0; i < m_Constraints.Length; ++i)
            {
                m_Jobs[i] = m_Constraints[i].CreateJob(animator);
            }

            return isInitialized = true;
        }

        /// <summary>
        /// Updates the OverrideRigLayer jobs. This is called during the Update loop before
        /// the Animator evaluates the PlayableGraph.
        /// </summary>
        public void Update()
        {
            if (!isInitialized)
                return;

            for (int i = 0; i < m_Constraints.Length; ++i)
            {
                m_Constraints[i].UpdateJob(m_Jobs[i]);
            }
        }

        /// <summary>
        /// Resets the OverrideRigLayer. This will destroy the animation jobs and
        /// free up memory.
        /// </summary>
        public void Reset()
        {
            if (!isInitialized)
                return;

            for (int i = 0, count = m_Constraints.Length; i < count; ++i)
            {
                m_Constraints[i].DestroyJob(m_Jobs[i]);
            }

            m_Constraints = null;
            m_Jobs = null;

            isInitialized = false;
        }

        /// <summary>
        /// Queries whether the OverrideRigLayer is valid.
        /// </summary>
        /// <returns>True if OverrideRigLayer is valid, false otherwise.</returns>
        public bool IsValid() => rig != null && isInitialized;
    }
}

