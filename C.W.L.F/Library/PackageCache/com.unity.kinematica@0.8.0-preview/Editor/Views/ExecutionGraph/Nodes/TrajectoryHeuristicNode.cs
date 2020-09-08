using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;

using Unity.Mathematics;

using CodeBookIndex = Unity.Kinematica.Binary.CodeBookIndex;

namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(TrajectoryHeuristicDebug))]
    internal class TrajectoryHeuristicNode : GraphNode
    {
        Label currentLabel = new Label();
        Label candidateLabel = new Label();
        Label pickLabel = new Label();


        CodeBookIndex GetCodeBookIndex(ref Binary binary, TimeIndex timeIndex)
        {
            int numCodeBooks = binary.numCodeBooks;

            for (int i = 0; i < numCodeBooks; ++i)
            {
                ref var codeBook = ref binary.GetCodeBook(i);

                int numIntervals = codeBook.intervals.Length;

                for (int j = 0; j < numIntervals; ++j)
                {
                    var intervalIndex = codeBook.intervals[j];

                    ref var interval = ref binary.GetInterval(intervalIndex);

                    if (interval.segmentIndex == timeIndex.segmentIndex)
                    {
                        return i;
                    }
                }
            }

            return CodeBookIndex.Invalid;
        }

        public override void UpdateState()
        {
            currentLabel.text = string.Empty;
            candidateLabel.text = string.Empty;
            pickLabel.text = string.Empty;

            MemoryRef<MotionSynthesizer> synthesizerRef = owner.Synthesizer;
            if (!synthesizerRef.IsValid)
            {
                return;
            }

            ref var synthesizer = ref synthesizerRef.Ref;

            ref var binary = ref synthesizer.Binary;

            TrajectoryHeuristicDebug heuristic = GetDebugObject<TrajectoryHeuristicDebug>();
            TimeIndex candidate = GetDebugObjectField<SamplingTime>(heuristic.candidate).timeIndex;
            Trajectory desiredTrajectory = GetDebugObjectField<Trajectory>(heuristic.desiredTrajectory);

            var worldRootTransform = synthesizer.WorldRootTransform;

            var codeBookIndex = GetCodeBookIndex(ref binary, candidate);

            if (codeBookIndex.IsValid)
            {
                ref var codeBook = ref binary.GetCodeBook(codeBookIndex);

                var metricIndex = codeBook.metricIndex;

                var candidateFragment =
                    binary.CreateTrajectoryFragment(
                        metricIndex, SamplingTime.Create(candidate));

                var currentFragment =
                    binary.CreateTrajectoryFragment(
                        metricIndex, synthesizer.Time);

                var desiredFragment =
                    binary.CreateTrajectoryFragment(
                        metricIndex, desiredTrajectory);

                codeBook.trajectories.Normalize(candidateFragment.array);
                codeBook.trajectories.Normalize(currentFragment.array);
                codeBook.trajectories.Normalize(desiredFragment.array);

                var current2Desired =
                    codeBook.trajectories.FeatureDeviation(
                        currentFragment.array, desiredFragment.array);

                var candidate2Desired =
                    codeBook.trajectories.FeatureDeviation(
                        candidateFragment.array, desiredFragment.array);

                currentLabel.text = string.Format(
                    "Current deviation: {0:0.000}", current2Desired);

                candidateLabel.text = string.Format(
                    "Candidate deviation: {0:0.000}", candidate2Desired);

                float deviation = math.max(heuristic.trajectoryDeviation, 0.0f);
                float minDeviation = heuristic.minTrajectoryDeviation;
                string comparison = deviation <= minDeviation ? "<=" : ">";
                string discarded = deviation <= minDeviation ? ", fragment discarded" : ", fragment picked";

                pickLabel.text = $"Deviation difference: {deviation:0.000} {comparison} {minDeviation:0.000}{discarded}";

                desiredFragment.Dispose();
                currentFragment.Dispose();
                candidateFragment.Dispose();
            }

            desiredTrajectory.Dispose();
        }

        public override void DrawDefaultInspector()
        {
            DebugDrawOptions options = DebugDrawOptions.Create();

            currentLabel.style.color = options.inputOutputFragTextColor * options.graphColorModifier;
            controlsContainer.Add(currentLabel);

            candidateLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            controlsContainer.Add(candidateLabel);

            pickLabel.style.color = options.bestFragTextColor * options.graphColorModifier;
            controlsContainer.Add(pickLabel);
        }
    }
}
