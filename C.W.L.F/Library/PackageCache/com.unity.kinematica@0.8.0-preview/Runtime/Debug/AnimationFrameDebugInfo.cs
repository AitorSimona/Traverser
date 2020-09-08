using Unity.SnapshotDebugger;

namespace Unity.Kinematica
{
    /// <summary>
    /// Informations about a played animation during one given frame that will feed the animation timeline in the snapshot debugger
    /// </summary>
    public struct AnimationFrameDebugInfo : IFrameRecord
    {
        /// <summary>
        /// Unique identifier allowing the debugger to group frames sharing the same identifier into blocks onto the timeline
        /// </summary>
        public int     sequenceIdentifier;

        /// <summary>
        /// Name of the animation
        /// </summary>
        public string  animName;

        /// <summary>
        /// Animation clip frame index
        /// </summary>
        public float   animFrame;

        /// <summary>
        /// Weight of the animation in the final pose
        /// </summary>
        public float   weight;

        /// <summary>
        /// How long (in seconds) the frame will be blended out after the sequence isn't updated anymore
        /// </summary>
        public float   blendOutDuration;
    }
}
