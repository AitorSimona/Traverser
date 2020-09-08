using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;

using Unity.Mathematics;

using CodeBookIndex = Unity.Kinematica.Binary.CodeBookIndex;

namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(FindClosestPoseTrajectoryDebug))]
    internal class FindClosestPoseTrajectoryNode : GraphNode
    {
        Label bestFragTitleLabel = new Label();
        Label bestFragClipLabel = new Label();
        Label bestFragCostLabel = new Label();
        Label deviationLabel = new Label();

        public override string Title
        {
            get
            {
                FindClosestPoseTrajectoryDebug debugObject = GetDebugObject<FindClosestPoseTrajectoryDebug>();

                if (debugObject.trajectory.IsValid)
                {
                    return "Find Closest Pose & Trajectory";
                }
                else
                {
                    return "Find Closest Pose";
                }
            }
        }

        public override void UpdateState()
        {
            bestFragTitleLabel.text = string.Empty;
            bestFragClipLabel.text = string.Empty;
            bestFragCostLabel.text = string.Empty;;
            deviationLabel.text = string.Empty;

            MemoryRef<MotionSynthesizer> synthesizerRef = owner.Synthesizer;
            if (!synthesizerRef.IsValid)
            {
                return;
            }

            ref var synthesizer = ref synthesizerRef.Ref;

            ref var binary = ref synthesizer.Binary;

            FindClosestPoseTrajectoryDebug debugObject = GetDebugObject<FindClosestPoseTrajectoryDebug>();

            SamplingTime closestMatch = GetDebugObjectField<SamplingTime>(debugObject.closestMatch);

            if (closestMatch.IsValid)
            {
                bestFragTitleLabel.text = "Best Matching Fragment";

                AnimationSampleTimeIndex animSampleTime = binary.GetAnimationSampleTimeIndex(closestMatch.timeIndex);
                bestFragClipLabel.text = $"Clip: {animSampleTime.clipName}, Frame: {animSampleTime.animFrameIndex}";


                if (debugObject.deviationTable.IsValid)
                {
                    DeviationTable deviationTable = GetDebugObjectField<DeviationTable>(debugObject.deviationTable);

                    DeviationScore deviation = deviationTable.GetDeviation(ref binary, closestMatch.timeIndex);

                    if (deviation.trajectoryDeviation >= 0.0f)
                    {
                        bestFragCostLabel.text = $"Pose cost: {deviation.poseDeviation:0.000}, Trajectory: {deviation.trajectoryDeviation:0.000}, Total: {deviation.poseDeviation + deviation.trajectoryDeviation:0.000}";
                    }
                    else
                    {
                        bestFragCostLabel.text = $"Pose cost: {deviation.poseDeviation:0.000}";
                    }

                    deviationTable.Dispose();
                }

                if (debugObject.maxDeviation >= 0.0f)
                {
                    deviationLabel.text = debugObject.GetDeviationText();
                }
            }
        }

        public override void DrawDefaultInspector()
        {
            DebugDrawOptions options = DebugDrawOptions.Create();

            bestFragTitleLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            bestFragTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            controlsContainer.Add(bestFragTitleLabel);

            bestFragClipLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            controlsContainer.Add(bestFragClipLabel);

            bestFragCostLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            controlsContainer.Add(bestFragCostLabel);

            deviationLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            controlsContainer.Add(deviationLabel);
        }
    }
}
