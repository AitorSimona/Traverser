using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    public static partial class DebugDraw
    {
        public static float DrawFragment(ref MotionSynthesizer synthesizer, SamplingTime samplingTime, Color color, float timeOffset, AffineTransform anchorTransform)
        {
            Assert.IsTrue(samplingTime.IsValid);

            AnimationSampleTimeIndex animSampleTime = synthesizer.Binary.GetAnimationSampleTimeIndex(samplingTime.timeIndex);
            string animFrameTxt = $"{animSampleTime.clipName} frame {animSampleTime.animFrameIndex}";

            ref Binary binary = ref synthesizer.Binary;

            Binary.PoseFragment poseFragment = binary.ReconstructPoseFragment(samplingTime);
            if (poseFragment.IsValid)
            {
                Binary.PoseFragmentDisplay.Options drawOptions = Binary.PoseFragmentDisplay.Options.Create();
                drawOptions.timeOffset = timeOffset;
                drawOptions.poseColor = color;

                Binary.PoseFragmentDisplay.Create(poseFragment).Display(
                    ref binary, anchorTransform,
                    drawOptions);

                poseFragment.Dispose();
            }

            float linearSpeedInMetersPerSecond = -1.0f;

            Binary.TrajectoryFragment trajectoryFragment = binary.ReconstructTrajectoryFragment(samplingTime);
            if (trajectoryFragment.IsValid)
            {
                Binary.TrajectoryFragmentDisplay.Options drawOptions = Binary.TrajectoryFragmentDisplay.Options.Create();

                drawOptions.baseColor = color;

                linearSpeedInMetersPerSecond =
                    Binary.TrajectoryFragmentDisplay.Create(
                        trajectoryFragment).Display(
                            ref binary, anchorTransform,
                            drawOptions);

                trajectoryFragment.Dispose();
            }

            return linearSpeedInMetersPerSecond;
        }
    }
}
