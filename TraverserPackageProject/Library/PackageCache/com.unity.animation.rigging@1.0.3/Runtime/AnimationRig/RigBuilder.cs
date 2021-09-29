using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.Experimental.Animations;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// RigBuilder is the root component that holds the Rigs that create an Animation Rigging hierarchy.
    /// Its purpose is to create the PlayableGraph that will be used in the associated Animator component to animate
    /// a character with constraints.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent, ExecuteInEditMode, AddComponentMenu("Animation Rigging/Setup/Rig Builder")]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.0?preview=1&subfolder=/manual/index.html")]
    public class RigBuilder : MonoBehaviour, IAnimationWindowPreview, IRigEffectorHolder
    {
        [SerializeField] private List<RigLayer> m_RigLayers;

        private IRigLayer[] m_RuntimeRigLayers;
        private SyncSceneToStreamLayer m_SyncSceneToStreamLayer;

#if UNITY_EDITOR
        [SerializeField] private List<RigEffectorData> m_Effectors = new List<RigEffectorData>();

        /// <inheritdoc />
        public IEnumerable<RigEffectorData> effectors { get => m_Effectors; }
#endif

        /// <summary>
        /// Delegate function that covers a RigBuilder calling OnEnable.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder component</param>
        public delegate void OnAddRigBuilderCallback(RigBuilder rigBuilder);
        /// <summary>
        /// Delegate function that covers a RigBuilder calling OnDisable.
        /// </summary>
        /// <param name="rigBuilder">The RigBuilder component</param>
        public delegate void OnRemoveRigBuilderCallback(RigBuilder rigBuilder);

        /// <summary>
        /// Notification callback that is sent whenever a RigBuilder calls OnEnable.
        /// </summary>
        public static OnAddRigBuilderCallback onAddRigBuilder;
        /// <summary>
        /// Notification callback that is sent whenever a RigBuilder calls OnDisable.
        /// </summary>
        public static OnRemoveRigBuilderCallback onRemoveRigBuilder;

        void OnEnable()
        {
            // Build runtime data.
            if (Application.isPlaying)
                Build();

            onAddRigBuilder?.Invoke(this);
        }

        void OnDisable()
        {
            // Clear runtime data.
            if (Application.isPlaying)
                Clear();

            onRemoveRigBuilder?.Invoke(this);
        }

        void OnDestroy()
        {
            Clear();
        }

        void Update()
        {
            if (!graph.IsValid())
                return;

            syncSceneToStreamLayer.Update(m_RuntimeRigLayers);

            for (int i = 0, count = m_RuntimeRigLayers.Length; i < count; ++i)
            {
                if (m_RuntimeRigLayers[i].IsValid() && m_RuntimeRigLayers[i].active)
                    m_RuntimeRigLayers[i].Update();
            }
        }

        /// <summary>Builds the RigBuilder PlayableGraph.</summary>
        /// <returns>Returns true if the RigBuilder has created a valid PlayableGraph. Returns false otherwise.</returns>
        public bool Build()
        {
            Clear();

            var animator = GetComponent<Animator>();
            if (animator == null || layers.Count == 0)
                return false;

            // Make a copy of the layers list.
            m_RuntimeRigLayers = layers.ToArray();

            graph = RigBuilderUtils.BuildPlayableGraph(animator, m_RuntimeRigLayers, syncSceneToStreamLayer);

            if (!graph.IsValid())
                return false;

            graph.Play();

            return true;
        }

        /// <summary>
        /// Destroys the RigBuilder PlayableGraph and frees associated RigLayers memory.
        /// </summary>
        public void Clear()
        {
            if (graph.IsValid())
                graph.Destroy();

            if (m_RuntimeRigLayers != null)
            {
                foreach (var layer in m_RuntimeRigLayers)
                    layer.Reset();

                m_RuntimeRigLayers = null;
            }

            syncSceneToStreamLayer.Reset();
        }

        //
        // IAnimationWindowPreview methods implementation
        //

        /// <summary>Notification callback when the animation previewer starts previewing an AnimationClip.</summary>
        /// <remarks>This is called by the Animation Window or the Timeline Editor.</remarks>
        public void StartPreview()
        {
            if (!enabled)
                return;

            // Make a copy of the layer list if it doesn't already exist.
            if (m_RuntimeRigLayers == null)
                m_RuntimeRigLayers = layers.ToArray();

            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                foreach (var layer in m_RuntimeRigLayers)
                {
                    layer.Initialize(animator);
                }
            }
        }

        /// <summary>Notification callback when the animation previewer stops previewing an AnimationClip.</summary>
        /// <remarks>This is called by the Animation Window or the Timeline Editor.</remarks>
        public void StopPreview()
        {
            if (!enabled)
                return;

            if (Application.isPlaying)
                return;

            Clear();
        }

        /// <summary>Notification callback when the animation previewer updates its PlayableGraph before sampling an AnimationClip.</summary>
        /// <remarks>This is called by the Animation Window or the Timeline Editor.</remarks>
        /// <param name="graph">The animation previewer PlayableGraph</param>
        public void UpdatePreviewGraph(PlayableGraph graph)
        {
            if (!enabled)
                return;

            if (!graph.IsValid())
                return;

            syncSceneToStreamLayer.Update(m_RuntimeRigLayers);

            foreach (var layer in m_RuntimeRigLayers)
            {
                if (layer.IsValid() && layer.active)
                    layer.Update();
            }
        }

        /// <summary>
        /// Appends custom Playable nodes to the animation previewer PlayableGraph.
        /// </summary>
        /// <param name="graph">The animation previewer PlayableGraph</param>
        /// <param name="inputPlayable">The current root of the PlayableGraph</param>
        /// <returns></returns>
        public Playable BuildPreviewGraph(PlayableGraph graph, Playable inputPlayable)
        {
            if (!enabled)
                return inputPlayable;

            var animator = GetComponent<Animator>();
            if (animator == null || m_RuntimeRigLayers.Length == 0)
                return inputPlayable;

            var playableChains = RigBuilderUtils.BuildPlayables(animator, graph, m_RuntimeRigLayers, syncSceneToStreamLayer);
            foreach(var chain in playableChains)
            {
                if (chain.playables == null || chain.playables.Length == 0)
                    continue;

                chain.playables[0].AddInput(inputPlayable, 0, 1);
                inputPlayable = chain.playables[chain.playables.Length - 1];
            }

            return inputPlayable;
        }

#if UNITY_EDITOR
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

        /// <summary>
        /// Returns a list of RigLayer associated to this RigBuilder.
        /// </summary>
        public List<RigLayer> layers
        {
            get
            {
                if (m_RigLayers == null)
                    m_RigLayers = new List<RigLayer>();

                return m_RigLayers;
            }

            set => m_RigLayers = value;
        }

        private SyncSceneToStreamLayer syncSceneToStreamLayer
        {
            get
            {
                if (m_SyncSceneToStreamLayer == null)
                    m_SyncSceneToStreamLayer = new SyncSceneToStreamLayer();

                return m_SyncSceneToStreamLayer;
            }

            set => m_SyncSceneToStreamLayer = value;
        }

        /// <summary>
        /// Retrieves the PlayableGraph created by this RigBuilder.
        /// </summary>
        public PlayableGraph graph { get; private set; }
    }
}
