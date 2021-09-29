using UnityEngine.Playables;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This is the base interface for all animation job binders.
    /// </summary>
    public interface IAnimationJobBinder
    {
        /// <summary>
        /// Creates the animation job.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <param name="data">The constraint data.</param>
        /// <param name="component">The constraint component.</param>
        /// <returns>Returns a new job interface.</returns>
        IAnimationJob Create(Animator animator, IAnimationJobData data, Component component = null);
        /// <summary>
        /// Destroys the animation job.
        /// </summary>
        /// <param name="job">The animation job to destroy.</param>
        void Destroy(IAnimationJob job);
        /// <summary>
        /// Updates the animation job.
        /// </summary>
        /// <param name="job">The animation job to update.</param>
        /// <param name="data">The constraint data.</param>
        void Update(IAnimationJob job, IAnimationJobData data);
        /// <summary>
        /// Creates an AnimationScriptPlayable with the specified animation job.
        /// </summary>
        /// <param name="graph">The PlayableGraph that will own the AnimationScriptPlayable.</param>
        /// <param name="job">The animation job to use in the AnimationScriptPlayable.</param>
        /// <returns>Returns a new AnimationScriptPlayable.</returns>
        AnimationScriptPlayable CreatePlayable(PlayableGraph graph, IAnimationJob job);
    }

    /// <summary>
    /// The is the base class for all animation job binders.
    /// </summary>
    /// <typeparam name="TJob">The constraint job.</typeparam>
    /// <typeparam name="TData">The constraint data.</typeparam>
    public abstract class AnimationJobBinder<TJob, TData> : IAnimationJobBinder
        where TJob : struct, IAnimationJob
        where TData : struct, IAnimationJobData
    {
        /// <summary>
        /// Creates the animation job.
        /// </summary>
        /// <param name="animator">The animated hierarchy Animator component.</param>
        /// <param name="data">The constraint data.</param>
        /// <param name="component">The constraint component.</param>
        /// <returns>Returns a new job interface.</returns>
        public abstract TJob Create(Animator animator, ref TData data, Component component);
        /// <summary>
        /// Destroys the animation job.
        /// </summary>
        /// <param name="job">The animation job to destroy.</param>
        public abstract void Destroy(TJob job);
        /// <summary>
        /// Updates the animation job.
        /// </summary>
        /// <param name="job">The animation job to update.</param>
        /// <param name="data">The constraint data.</param>
        public virtual void Update(TJob job, ref TData data) {}

        /// <inheritdoc />
        IAnimationJob IAnimationJobBinder.Create(Animator animator, IAnimationJobData data, Component component)
        {
            Debug.Assert(data is TData);
            TData tData = (TData)data;
            return Create(animator, ref tData, component);
        }
        /// <inheritdoc />
        void IAnimationJobBinder.Destroy(IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            Destroy((TJob)job);
        }
        /// <inheritdoc />
        void IAnimationJobBinder.Update(IAnimationJob job, IAnimationJobData data)
        {
            Debug.Assert(data is TData && job is TJob);
            TData tData = (TData)data;
            Update((TJob)job, ref tData);
        }
        /// <inheritdoc />
        AnimationScriptPlayable IAnimationJobBinder.CreatePlayable(PlayableGraph graph, IAnimationJob job)
        {
            Debug.Assert(job is TJob);
            return AnimationScriptPlayable.Create(graph, (TJob)job);
        }
    }
}
