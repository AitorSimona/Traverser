using System;

using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Unity.Collections;

namespace Unity.Kinematica
{
    /// <summary>
    /// This component is a wrapper around the motion synthesizer.
    /// </summary>
    /// <remarks>
    /// The motion synthesizer represents the actual core implementation
    /// of Kinematica which can be used in a pure DOTS environment directly.
    /// It provides a raw transform buffer which represents the current
    /// character pose and does not provide any infrastructure to feed
    /// the current pose to the character.
    /// <para>
    /// The Kinematica component is a wrapper around the motion synthesizer
    /// that can be used in scenarios where Kinematica is to be used in
    /// conjunction with stock Unity Game Objects.
    /// </para>
    /// <para>
    /// It establishes a Playable graph that forwards the character pose
    /// to the Animator component. It also provides automatic snapshots
    /// and rewind functionality, i.e. no additional user code is required
    /// to support snapshot debugging of the Kinematica component.
    /// </para>
    /// <para>
    /// The Kinematica component is not necessary to run Kinematica and all its
    /// associated tools (execution graph, snapshot debugger...), it is provided as an
    /// example on how to use Kinematica inside a component.
    /// </para>
    /// </remarks>
    /// <seealso cref="MotionSynthesizer"/>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Kinematica/Kinematica")]
    public partial class Kinematica : SnapshotProvider, IFrameDebugProvider, IMotionSynthesizerProvider
    {
        /// <summary>
        /// Allows access to the underlying Kinematica runtime asset.
        /// </summary>
        public BinaryReference resource;

        /// <summary>
        /// Denotes the default blend duration for the motion synthesizer.
        /// </summary>
        [Tooltip("Blending between poses will last for this duration in seconds.")]
        [Range(0.0f, 1.0f)]
        public float blendDuration = 0.25f;

        internal bool IsInitialized
        {
            get; private set;
        }

        [Tooltip("If true, Kinematica will apply root motion to move character in the world. Set this boolean to false in order to process root motion inside your script.")]
        public bool applyRootMotion = true;

        /// <summary>
        /// Denotes the delta time in seconds to be used during this frame.
        /// </summary>
        /// <remarks>
        /// The delta time in seconds mirrors Time.deltaTime during play mode
        /// unless the snapshot debugger rewinds to a recorded snapshot.
        /// The current frame delta time is recorded as part of a snapshot
        /// to guarantee the exact same evaluation result when in case the
        /// snapshot debugger rewinds to a previous snapshot frame.
        /// </remarks>
        [SerializeField]
        [HideInInspector]
        protected float _deltaTime;

        MotionSynthesizer synthesizer;

        PlayableGraph playableGraph;

        UpdateAnimationPoseJob job;

        /// <summary>
        /// Allows direct access to the underlying Kinematica runtime asset.
        /// </summary>
        public ref Binary Binary => ref synthesizer.Binary;

        /// <summary>
        /// Allows direct access to the motion synthesizer.
        /// </summary>
        /// <remarks>
        /// Most of Kinematica's API methods can be found in the
        /// motion synthesizer. API methods that are specific to the
        /// game object wrapper can be found on the Kinematica
        /// component directly.
        /// </remarks>
        public MemoryRef<MotionSynthesizer> Synthesizer
        {
            get
            {
                if (!synthesizer.IsValid)
                {
                    synthesizer = MotionSynthesizer.Create(resource, AffineTransform.Create(transform.position, transform.rotation), blendDuration, Allocator.Persistent);
                }

                return MemoryRef<MotionSynthesizer>.Create(ref synthesizer);
            }
        }

        public bool IsSynthesizerInitialized => synthesizer.IsValid;

        /// <summary>
        /// Override for OnEnable().
        /// </summary>
        /// <remarks>
        /// The Playable graph that forwards the current character pose to the
        /// Animator component gets constructed during the execution of this method.
        /// <para>
        /// This method also registers the Kinematica component with the snapshot debugger.
        /// </para>
        /// </remarks>
        public override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            Debugger.frameDebugger.AddFrameDebugProvider(this);
#endif
            OnEarlyUpdate(false);

            try
            {
                if (!CreatePlayableGraph())
                {
                    throw new Exception("Couldn't create playable graph");
                }

                IsInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Cannot play Kinematica asset on target {gameObject.name} : {e.Message}");
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Override for OnDisable()
        /// </summary>
        /// <remarks>
        /// This method releases all internally constructed objects and unregisters
        /// the Kinematica component from the snapshot debugger.
        /// </remarks>
        public override void OnDisable()
        {
            if (!IsInitialized)
            {
                return;
            }

            base.OnDisable();

#if UNITY_EDITOR
            Debugger.frameDebugger.RemoveFrameDebugProvider(this);
#endif

            synthesizer.Dispose();

            job.Dispose();

            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }
        }

        /// <summary>
        /// This callback will be automatically invoked
        /// during UnityEngine.PlayerLoop.EarlyUpdate().
        /// </summary>
        public virtual void EarlyUpdate()
        {
        }

        /// <summary>
        /// Override for OnEarlyUpdate() which will be invoked
        /// as part of the snapshot debugger infrastructure during
        /// the execution of UnityEngine.PlayerLoop.EarlyUpdate.
        /// </summary>
        /// <param name="rewind">True when the snapshot debugger rewinds to a previously recorded snapshot, false otherwise.</param>
        public override void OnEarlyUpdate(bool rewind)
        {
            _deltaTime = Debugger.instance.deltaTime;

            if (!rewind)
            {
                EarlyUpdate();
            }
        }

        /// <summary>
        /// Called during the regular game object update loop.
        /// </summary>
        public void Update()
        {
            if (synthesizer.IsValid)
            {
                synthesizer.UpdateFrameCount(Time.frameCount);
                synthesizer.UpdateDebuggingStatus();
            }
        }

        /// <summary>
        /// Handler method which gets invoked during the animator update.
        /// </summary>
        /// <remarks>
        /// The motion synthesizer maintains the full world space transform
        /// of the character at all times. This method simply forwards this
        /// transform to the game object's transform.
        /// </remarks>
        public virtual void OnAnimatorMove()
        {
            if (applyRootMotion && synthesizer.IsValid)
            {
                transform.position = synthesizer.WorldRootTransform.t;
                transform.rotation = synthesizer.WorldRootTransform.q;
            }
        }

        bool CreatePlayableGraph()
        {
            var animator = GetComponent<Animator>();
            if (animator.avatar == null)
            {
                animator.avatar = AvatarBuilder.BuildGenericAvatar(animator.gameObject, transform.name);
                animator.avatar.name = "Avatar";
            }

            var deltaTimeProperty = animator.BindSceneProperty(transform, typeof(Kinematica), "_deltaTime");

            job = new UpdateAnimationPoseJob();
            if (!job.Setup(animator,
                GetComponentsInChildren<Transform>(), ref Synthesizer.Ref, deltaTimeProperty))
            {
                return false;
            }

            playableGraph =
                PlayableGraph.Create(
                    $"Kinematica_{animator.transform.name}");

            var output = AnimationPlayableOutput.Create(playableGraph, "output", animator);

            var playable = AnimationScriptPlayable.Create(playableGraph, job);

            output.SetSourcePlayable(playable);

            playableGraph.Play();

            return true;
        }
    }
}
