using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;

using Unity.Mathematics;


namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(SamplingTime))]
    internal class SamplingTimeNode : GraphNode
    {
        Label timeLabel = new Label();

        public override void UpdateState()
        {
            timeLabel.text = string.Empty;

            MemoryRef<MotionSynthesizer> synthesizerRef = owner.Synthesizer;
            if (!synthesizerRef.IsValid)
            {
                return;
            }

            ref var synthesizer = ref synthesizerRef.Ref;

            ref var binary = ref synthesizer.Binary;

            SamplingTime samplingTime = GetDebugObject<SamplingTime>();
            if (samplingTime.IsValid)
            {
                AnimationSampleTimeIndex animSampleTime = binary.GetAnimationSampleTimeIndex(samplingTime.timeIndex);
                timeLabel.text = $"Clip: {animSampleTime.clipName}, Frame: {animSampleTime.animFrameIndex}";
            }
        }

        public override void DrawDefaultInspector()
        {
            DebugDrawOptions options = DebugDrawOptions.Create();

            timeLabel.style.color = options.inputOutputFragTextColor * options.graphColorModifier;
            controlsContainer.Add(timeLabel);
        }
    }
}
