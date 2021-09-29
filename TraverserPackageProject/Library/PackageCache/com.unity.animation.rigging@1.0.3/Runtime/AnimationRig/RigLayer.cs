using System;
using UnityEngine.Serialization;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// The RigLayer is used by the RigBuilder to control in which order rigs will be
    /// evaluated and whether they are active or not.
    /// </summary>
    [Serializable]
    public class RigLayer : IRigLayer
    {
        [SerializeField][FormerlySerializedAs("rig")] private Rig m_Rig;
        [SerializeField][FormerlySerializedAs("active")] private bool m_Active = true;

        private IRigConstraint[] m_Constraints;
        private IAnimationJob[]  m_Jobs;

        /// <summary>The Rig associated to the RigLayer</summary>
        public Rig rig { get => m_Rig; private set => m_Rig = value; }
        /// <summary>The active state. True if the RigLayer is active, false otherwise.</summary>
        public bool active { get => m_Active; set => m_Active = value; }
        /// <summary>The RigLayer name.</summary>
        public string name { get => (rig != null ? rig.gameObject.name : "no-name"); }
        /// <summary>The list of constraints associated with the RigLayer.</summary>
        public IRigConstraint[] constraints { get => isInitialized ? m_Constraints : null; }
        /// <summary>The list of jobs built from constraints associated with the RigLayer.</summary>
        public IAnimationJob[] jobs { get => isInitialized ? m_Jobs : null; }
        /// <summary>Returns true if RigLayer was initialized or false otherwise.</summary>
        /// <seealso cref="RigLayer.Initialize"/>
        public bool isInitialized { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="rig">The rig represented by this rig layer.</param>
        /// <param name="active">The active state of the rig layer.</param>
        public RigLayer(Rig rig, bool active = true)
        {
            this.rig = rig;
            this.active = active;
        }

        /// <summary>
        /// Initializes the RigLayer.  This will retrieve the constraints associated with the Rig
        /// and create the animation jobs required by the PlayableGraph.
        /// </summary>
        /// <param name="animator">The Animator used to animate the RigLayer constraints.</param>
        /// <returns>True if RigLayer was initialized properly, false otherwise.</returns>
        public bool Initialize(Animator animator)
        {
            if (isInitialized)
                return true;

            if (rig != null)
            {
                m_Constraints = RigUtils.GetConstraints(rig);
                if (m_Constraints == null || m_Constraints.Length == 0)
                    return false;

                m_Jobs = RigUtils.CreateAnimationJobs(animator, m_Constraints);

                return (isInitialized = true);
            }

            return false;
        }

        /// <summary>
        /// Updates the RigLayer jobs. This is called during the Update loop before
        /// the Animator evaluates the PlayableGraph.
        /// </summary>
        public void Update()
        {
            if (!isInitialized)
                return;

            for (int i = 0, count = m_Constraints.Length; i < count; ++i)
                m_Constraints[i].UpdateJob(m_Jobs[i]);
        }

        /// <summary>
        /// Resets the RigLayer. This will destroy the animation jobs and
        /// free up memory.
        /// </summary>
        public void Reset()
        {
            if (!isInitialized)
                return;

            RigUtils.DestroyAnimationJobs(m_Constraints, m_Jobs);
            m_Constraints = null;
            m_Jobs = null;

            isInitialized = false;
        }

        /// <summary>
        /// Queries whether the RigLayer is valid.
        /// </summary>
        /// <returns>True if RigLayer is valid, false otherwise.</returns>
        public bool IsValid() => rig != null && isInitialized;
    }
}
