using Unity.Mathematics;
using Unity.Kinematica;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        internal class PayloadBuilder : Editor.PayloadBuilder
        {
            public PayloadBuilder(Builder builder, Interval destinationInterval, float sourceToTargetScale)
            {
                this.builder = builder;
                this.destinationInterval = destinationInterval;
                this.sourceToTargetScale = sourceToTargetScale;
            }

            Builder builder;
            Interval destinationInterval;
            float sourceToTargetScale;

            public int GetJointIndexForName(string jointName)
            {
                int jointNameIndex = builder.stringTable.GetStringIndex(jointName);
                int jointIndex = (jointNameIndex >= 0) ? builder.Binary.animationRig.GetJointIndexForNameIndex(jointNameIndex) : -1;

                return jointIndex;
            }

            public Interval DestinationInterval => destinationInterval;

            public float SourceToTargetScale => sourceToTargetScale;

            public AffineTransform GetRootTransform()
            {
                return GetRootTransform(destinationInterval.FirstFrame);
            }

            public AffineTransform GetJointTransformCharacterSpace(int jointIndex)
            {
                return GetJointTransformCharacterSpace(destinationInterval.FirstFrame, jointIndex);
            }

            public AffineTransform GetRootTransform(int frameIndex)
            {
                return builder.Binary.GetTrajectoryTransform(frameIndex);
            }

            public AffineTransform GetJointTransformCharacterSpace(int frameIndex, int jointIndex)
            {
                return builder.Binary.GetJointTransform(jointIndex, frameIndex);
            }
        }
    }
}
