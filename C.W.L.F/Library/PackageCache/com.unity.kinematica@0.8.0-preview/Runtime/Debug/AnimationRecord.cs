using Unity.SnapshotDebugger;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    internal struct AnimationFrameInfo
    {
        public float    endTime;
        public float    weight;

        public float    animFrame;
    }

    internal class AnimationRecord
    {
        public int                              sequenceIdentifier;
        public string                           animName;

        public float                            startTime;
        public float                            endTime;

        public float                            blendOutDuration;
        public float                            blendOutTime;

        public int                              rank;

        public CircularList<AnimationFrameInfo> animFrames;

        float GetAnimFrameStartTime(int index)
        {
            return index == 0 ? startTime : animFrames[index - 1].endTime;
        }

        public void AddAnimationFrame(float endTime, float weight, float animFrame, float blendOutDuration)
        {
            animFrames.PushBack(new AnimationFrameInfo()
            {
                endTime = endTime,
                weight = weight,
                animFrame = animFrame
            });

            if (blendOutDuration > 0.0f)
            {
                this.blendOutDuration = blendOutDuration;
                blendOutTime = blendOutDuration;
            }

            Assert.IsTrue(endTime <= this.endTime);
        }

        public void PruneAnimationFramesBeforeTimestamp(float timestamp)
        {
            while (animFrames.Count > 0 && animFrames[0].endTime < timestamp)
            {
                startTime = timestamp;
                animFrames.PopFront();
            }
        }

        public void PruneAnimationFramesStartingAfterTimestamp(float startTimeInSeconds)
        {
            while (animFrames.Count > 0 && GetAnimFrameStartTime(animFrames.Count - 1) > startTimeInSeconds)
            {
                animFrames.PopBack();
            }

            endTime = animFrames.Count == 0 ? startTime : animFrames.Last.endTime;
        }
    }
}
