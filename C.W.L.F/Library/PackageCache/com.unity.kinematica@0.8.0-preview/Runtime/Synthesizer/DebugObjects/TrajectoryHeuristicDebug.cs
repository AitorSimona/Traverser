using UnityEngine;
using Unity.Mathematics;

namespace Unity.Kinematica
{
    [Data("Trajectory Heuristic")]
    internal struct TrajectoryHeuristicDebug : IDebugObject, IDebugDrawable
    {
        public DebugIdentifier debugIdentifier { get; set; }

        [Input("Desired trajectory")]
        public DebugIdentifier desiredTrajectory;

        [Input("Closest Match")]
        public DebugIdentifier closestMatch;

        [Input("Candidate")]
        public DebugIdentifier candidate;

        [Output("Output time")]
        public DebugIdentifier outputTime;

        public float minTrajectoryDeviation;

        public float trajectoryDeviation;

        public void Draw(Camera camera, ref MotionSynthesizer synthesizer, DebugMemory debugMemory, SamplingTime debugSamplingTime, ref DebugDrawOptions options)
        {
            ref var binary = ref synthesizer.Binary;

            SamplingTime candidate = debugMemory.ReadObjectFromIdentifier<SamplingTime>(this.candidate);

            DebugDraw.SetMovableTextTitle(options.textWindowIdentifier, "Trajectory Heuristic");
            DebugDraw.AddMovableTextLine(options.textWindowIdentifier, binary.GetFragmentDebugText(synthesizer.Time, "Current Clip", options.timeOffset), options.inputOutputFragTextColor);
            DebugDraw.AddMovableTextLine(options.textWindowIdentifier, binary.GetFragmentDebugText(candidate, "Candidate Clip", options.timeOffset), options.bestFragTextColor);
            DrawDebugText(ref options);

            var worldRootTransform = synthesizer.WorldRootTransform;

            binary.DebugDrawTrajectory(
                worldRootTransform, candidate,
                binary.TimeHorizon, options.bestFragmentColor);

            binary.DebugDrawTrajectory(
                worldRootTransform, synthesizer.Time,
                binary.TimeHorizon, options.inputOutputFragmentColor);

            using (Trajectory desiredTrajectory = debugMemory.ReadObjectFromIdentifier<Trajectory>(this.desiredTrajectory))
            {
                DebugExtensions.DebugDrawTrajectory(worldRootTransform,
                    desiredTrajectory,
                    binary.SampleRate,
                    options.inputTrajectoryColor,
                    options.inputTrajectoryColor);
            }
        }

        public void DrawDebugText(ref DebugDrawOptions options)
        {
            float deviation = math.max(trajectoryDeviation, 0.0f);
            float minDeviation = minTrajectoryDeviation;
            string comparison = deviation <= minDeviation ? "<=" : ">";
            string discarded = deviation <= minDeviation ? ", Fragment discarded" : ", Fragment picked";

            DebugDraw.AddMovableTextLine(options.textWindowIdentifier, $"<b>Trajectory deviation:</b> {deviation:0.000} {comparison} {minDeviation:0.000}{discarded}", options.bestFragTextColor);
        }
    }
}
