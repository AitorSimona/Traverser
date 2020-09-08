using UnityEngine.Assertions;
using Unity.SnapshotDebugger;
using Unity.Mathematics;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        //
        // Kinematica Public API
        //

        public bool IsValid => isValid;

        /// <summary>
        /// Return blend duration between segments in seconds
        /// </summary>
        public float BlendDuration => poseGenerator.BlendDuration;

        internal void UpdateFrameCount(int frameCount)
        {
            this.frameCount = frameCount;
        }

        /// <summary>
        /// Introduces a new semantic query expression.
        /// </summary>
        /// <seealso cref="Unity.Kinematica.Query"/>
        public Query Query
        {
            get => Query.Create(ref Binary);
        }

        /// <summary>
        /// Play the first sequence from <code>queryResult</code>
        /// </summary>
        /// <remarks>
        /// This method accepts a query result (most likely obtained from
        /// a semantic query). It unconditionally extracts the first pose
        /// from the pose sequence and forwards it as the next sampling time
        /// to the internal pose stream generator.
        /// </remarks>
        /// <param name="queryResult">Query result obtained from a semantic query.</param>
        /// <seealso cref="Query"/>
        public void PlayFirstSequence(PoseSet poseSet)
        {
            if (poseSet.sequences.Length > 0)
            {
                ref var binary = ref Binary;

                ref var interval = ref binary.GetInterval(
                    poseSet.sequences[0].intervalIndex);

                PlayAtTime(interval.GetTimeIndex(interval.firstFrame));
            }
        }

        internal AffineTransform GetLocalTransform(int i)
        {
            return LocalSpaceTransformBuffer[i];
        }

        internal int TransformCount => poseGenerator.LocalSpaceTransformBuffer.Length;

        internal TransformBuffer LocalSpaceTransformBuffer => poseGenerator.LocalSpaceTransformBuffer;
    }
}
