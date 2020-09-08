using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;

using Unity.Mathematics;


namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(PlayAtTimeDebug))]
    internal class PlayAtTimeNode : GraphNode
    {
        Label timeLabel = new Label();

        public override void UpdateState()
        {
            timeLabel.Clear();

            MemoryRef<MotionSynthesizer> synthesizerRef = owner.Synthesizer;
            if (!synthesizerRef.IsValid)
            {
                return;
            }

            ref var synthesizer = ref synthesizerRef.Ref;

            ref var binary = ref synthesizer.Binary;

            PlayAtTimeDebug debugObject = GetDebugObject<PlayAtTimeDebug>();

            SamplingTime samplingTime = GetDebugObjectField<SamplingTime>(debugObject.playTime);
            if (samplingTime.IsValid)
            {
                AnimationSampleTimeIndex animSampleTime = binary.GetAnimationSampleTimeIndex(samplingTime.timeIndex);
                timeLabel.text = $"Clip: {animSampleTime.clipName}, Frame: {animSampleTime.animFrameIndex}";
            }
        }

        public override void DrawDefaultInspector()
        {
            timeLabel.style.color = Color.white;
            controlsContainer.Add(timeLabel);
        }
    }
}
