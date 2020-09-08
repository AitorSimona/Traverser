using UnityEngine;

namespace Unity.Kinematica
{
    [Data("Play At Time")]
    internal struct PlayAtTimeDebug : IDebugObject, IDebugDrawable
    {
        public DebugIdentifier debugIdentifier { get; set; }

        [Input("Play Time")]
        public DebugIdentifier playTime;

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            if (!playTime.IsValid)
            {
                return;
            }

            SamplingTime samplingTime = debugMemory.ReadObjectFromIdentifier<SamplingTime>(playTime);
            string text = synthesizer.Binary.GetFragmentDebugText(samplingTime, "Play Clip", options.timeOffset);

            DebugDraw.SetMovableTextTitle(options.textWindowIdentifier, "Play At Time");
            DebugDraw.AddMovableTextLine(options.textWindowIdentifier, text, options.inputOutputFragTextColor);

            DebugDraw.DrawFragment(ref synthesizer, samplingTime, options.inputOutputFragmentColor, options.timeOffset, synthesizer.WorldRootTransform);
        }
    }
}
