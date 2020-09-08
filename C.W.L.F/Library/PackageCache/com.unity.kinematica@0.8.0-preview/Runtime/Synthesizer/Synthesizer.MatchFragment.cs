using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using UnityEngine.Assertions;

using Buffer = Unity.SnapshotDebugger.Buffer;

namespace Unity.Kinematica
{
    [Flags]
    public enum MatchOptions
    {
        None = 0,
        DontMatchIfCandidateIsPlaying = 1 << 0,
        LoopSegment = 1 << 1
    };

    public partial struct MotionSynthesizer
    {
        public bool MatchPose(PoseSet candidates,
            SamplingTime samplingTime,
            MatchOptions options = MatchOptions.None,
            float maxPoseDeviation = -1.0f)
        {
            if ((options & MatchOptions.DontMatchIfCandidateIsPlaying) > 0 && IsTimeInSequences(samplingTime, candidates.sequences))
            {
                if ((options & MatchOptions.LoopSegment) > 0)
                {
                    LoopSegmentIfEndReached(samplingTime);
                }

                return true;
            }

            DebugPushGroup();

            SamplingTime closestMatch = FindClosestPoseMatch(candidates, samplingTime, maxPoseDeviation);
            if (closestMatch.IsValid)
            {
                PlayAtTime(closestMatch);
                return true;
            }
            else
            {
                if ((options & MatchOptions.LoopSegment) > 0)
                {
                    LoopSegmentIfEndReached(samplingTime);
                }

                return false;
            }
        }

        public bool MatchPoseAndTrajectory(PoseSet candidates,
            SamplingTime samplingTime,
            Trajectory trajectory,
            MatchOptions options = MatchOptions.None,
            float trajectoryWeight = 0.6f,
            float minTrajectoryDeviation = 0.03f,
            float maxTotalDeviation = -1.0f)
        {
            if ((options & MatchOptions.DontMatchIfCandidateIsPlaying) > 0 && IsTimeInSequences(samplingTime, candidates.sequences))
            {
                if ((options & MatchOptions.LoopSegment) > 0)
                {
                    LoopSegmentIfEndReached(samplingTime);
                }

                return true;
            }


            if (!trajectory.debugIdentifier.IsValid)
            {
                DebugPushGroup();
                DebugWriteUnblittableObject(ref trajectory);
            }

            DebugWriteUnblittableObject(ref candidates);
            DebugWriteBlittableObject(ref samplingTime);


            SamplingTime closestMatch = FindClosestPoseAndTrajectoryMatch(candidates, samplingTime, trajectory, trajectoryWeight, maxTotalDeviation);
            if (closestMatch.IsValid)
            {
                bool validMatch = true;
                if (minTrajectoryDeviation >= 0.0f)
                {
                    float trajectoryDeviation = RelativeDeviation(closestMatch.timeIndex, trajectory);
                    validMatch = trajectoryDeviation >= minTrajectoryDeviation;

                    SamplingTime outputTime = closestMatch;
                    outputTime.debugIdentifier = DebugIdentifier.Invalid;
                    DebugWriteBlittableObject(ref outputTime, true);

                    TrajectoryHeuristicDebug heuristicDebug = new TrajectoryHeuristicDebug()
                    {
                        desiredTrajectory = trajectory.debugIdentifier,
                        candidate = samplingTime.debugIdentifier,
                        closestMatch = closestMatch.debugIdentifier,
                        outputTime = outputTime.debugIdentifier,
                        trajectoryDeviation = trajectoryDeviation,
                        minTrajectoryDeviation = minTrajectoryDeviation
                    };
                    DebugWriteBlittableObject(ref heuristicDebug);

                    closestMatch = outputTime;
                }

                if (validMatch)
                {
                    PlayAtTime(closestMatch);
                    return true;
                }
            }

            if ((options & MatchOptions.LoopSegment) > 0)
            {
                LoopSegmentIfEndReached(samplingTime);
            }

            return false;
        }

        public void LoopSegmentIfEndReached(SamplingTime samplingTime)
        {
            int numSegmentFrames = Binary.GetSegment(samplingTime.segmentIndex).destination.NumFrames;
            if (samplingTime.frameIndex == numSegmentFrames - 1)
            {
                PlayAtTime(SamplingTime.Create(TimeIndex.Create(samplingTime.segmentIndex)));
            }
        }

        bool IsTimeInSequences(SamplingTime samplingTime, NativeArray<PoseSequence> sequences)
        {
            var numSequences = sequences.Length;

            for (int i = 0; i < sequences.Length; ++i)
            {
                Binary.SegmentIndex segmentIndex = Binary.GetInterval(sequences[i].intervalIndex).segmentIndex;
                if (samplingTime.segmentIndex == segmentIndex)
                {
                    return true;
                }
            }

            return false;
        }

        float RelativeDeviation(TimeIndex candidate, Trajectory trajectory)
        {
            ref var binary = ref Binary;

            var codeBookIndex = GetCodeBookIndex(candidate);

            ref var codeBook = ref binary.GetCodeBook(codeBookIndex);

            var metricIndex = codeBook.metricIndex;

            var candidateFragment =
                binary.CreateTrajectoryFragment(
                    metricIndex, SamplingTime.Create(candidate));

            var currentFragment =
                binary.CreateTrajectoryFragment(
                    metricIndex, Time);

            var desiredFragment =
                binary.CreateTrajectoryFragment(
                    metricIndex, trajectory);

            codeBook.trajectories.Normalize(candidateFragment.array);
            codeBook.trajectories.Normalize(currentFragment.array);
            codeBook.trajectories.Normalize(desiredFragment.array);

            var current2Desired =
                codeBook.trajectories.FeatureDeviation(
                    currentFragment.array, desiredFragment.array);

            var candidate2Desired =
                codeBook.trajectories.FeatureDeviation(
                    candidateFragment.array, desiredFragment.array);

            desiredFragment.Dispose();
            currentFragment.Dispose();
            candidateFragment.Dispose();

            return current2Desired - candidate2Desired;
        }

        Binary.CodeBookIndex GetCodeBookIndex(TimeIndex timeIndex)
        {
            ref Binary binary = ref Binary;

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

            return Binary.CodeBookIndex.Invalid;
        }
    }
}
