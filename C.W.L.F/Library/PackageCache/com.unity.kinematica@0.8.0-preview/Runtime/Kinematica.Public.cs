using UnityEngine;
using UnityEngine.Animations;

using Unity.Jobs;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    public partial class Kinematica : SnapshotProvider, IMotionSynthesizerProvider
    {
        //
        // Kinematica Public API
        //

        /// <summary>
        /// Play the first sequence from <code>queryResult</code>
        /// </summary>
        /// <remarks>
        /// Forwards the call to the motion synthesizer.
        /// </remarks>
        /// <param name="queryResult">Pose sequence that should be pushed to the motion synthesizer.</param>
        /// <seealso cref="MotionSynthesizer.Push"/>
        public void PlayFirstSequence(QueryResult queryResult)
        {
            Synthesizer.Ref.PlayFirstSequence(queryResult);
        }

        /// <summary>
        /// Generates a new semantic query.
        /// </summary>
        /// <remarks>
        /// This method generates an empty semantic query which
        /// should be filled out by specifying tag trait constraints
        /// and/or marker trait constraint.
        /// </remarks>
        /// <seealso cref="Unity.Kinematica.Query"/>
        /// <seealso cref="MotionSynthesizer.Query"/>
        public Query Query
        {
            get => Synthesizer.Ref.Query;
        }

        /// <summary>
        /// Schedules the job handle passed as argument
        /// as a dependency of the Animator component.
        /// </summary>
        /// <remarks>
        /// This method allows to schedule a job to be executed
        /// before the Animator component job executes. This in turn
        /// guarantees that the job passed as argument executes
        /// before Kinematica's motion synthesizer executes.
        /// </remarks>
        /// <param name="jobHandle">Job handle that should be scheduled as a dependency of the Animator component.</param>
        public void AddJobDependency(JobHandle jobHandle)
        {
            GetComponent<Animator>().AddJobDependency(jobHandle);
        }
    }
}
