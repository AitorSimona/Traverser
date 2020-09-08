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
    public partial struct MotionSynthesizer
    {
        public unsafe SamplingTime FindClosestPoseMatch(PoseSet candidates,
            SamplingTime samplingTime,
            float maxPoseDeviation = -1.0f)
        {
            if (!samplingTime.IsValid)
            {
                return SamplingTime.Create(SelectFirstPose(candidates.sequences));
            }

            ref Binary binary = ref Binary;

            var intervals = binary.GetIntervals();

            AdjustSequenceBoundaries(candidates.sequences);

            DeviationTable deviationTable = IsDebugging ? DeviationTable.Create(candidates) : DeviationTable.CreateInvalid();

            var query = MatchFragmentQuery.Create(ref binary);

            int numCodeBooks = binary.numCodeBooks;

            int numCodeValues = Binary.CodeBook.kNumCodeValues;

            for (int i = 0; i < numCodeBooks; ++i)
            {
                if (RefersToCodeBook(candidates.sequences, i))
                {
                    query.DecodePose(
                        ref binary, i, samplingTime.timeIndex);

                    query.GeneratePoseDistanceTable(ref binary, i);
                }
            }

            float minimumDeviation = float.MaxValue;

            TimeIndex closestMatch = TimeIndex.Invalid;

            var numSequences = candidates.sequences.Length;

            for (int i = 0; i < numSequences; ++i)
            {
                var intervalIndex = candidates.sequences[i].intervalIndex;

                ref var interval = ref binary.GetInterval(intervalIndex);

                Assert.IsTrue(interval.Contains(candidates.sequences[i].firstFrame));

                var codeBookIndex = interval.codeBookIndex;
                if (!codeBookIndex.IsValid)
                {
                    throw new NoMetricException(ref this, interval.segmentIndex);
                }

                var segmentIndex = (short)interval.segmentIndex;

                ref var codeBook = ref binary.GetCodeBook(codeBookIndex);

                var intervalFragmentIndex =
                    codeBook.GetFragmentIndex(
                        intervals, intervalIndex);

                Assert.IsTrue(intervalFragmentIndex >= 0);

                var fragmentIndex =
                    intervalFragmentIndex + candidates.sequences[i].firstFrame;

                Assert.IsTrue(fragmentIndex < codeBook.numFragments);

                var numFrames = candidates.sequences[i].numFrames;

                Assert.IsTrue(numFrames <= intervals[intervalIndex].numFrames);

                Assert.IsTrue((fragmentIndex + numFrames) <= codeBook.numFragments);

                float* poseDistances =
                    query.GetPoseDistances(codeBookIndex);

                short frameIndex = (short)(candidates.sequences[i].firstFrame);

                int numPoseFeatures = codeBook.poses.numFeaturesFlattened;

                byte* poseCodes =
                    (byte*)codeBook.poses.codes.GetUnsafePtr() +
                    numPoseFeatures * fragmentIndex;

                for (int j = 0; j < numFrames; ++j)
                {
                    float candidateDeviation = 0.0f;

                    float* ptr = poseDistances;

                    for (int k = 0; k < numPoseFeatures; ++k)
                    {
                        candidateDeviation += ptr[*poseCodes++];

                        ptr += numCodeValues;
                    }

                    if (IsDebugging)
                    {
                        deviationTable.SetDeviation(i, j, candidateDeviation, -1.0f);
                    }

                    if (candidateDeviation < minimumDeviation)
                    {
                        minimumDeviation = candidateDeviation;

                        closestMatch.segmentIndex = segmentIndex;
                        closestMatch.frameIndex = frameIndex;
                    }

                    fragmentIndex++;
                    frameIndex++;
                }
            }

            query.Dispose();

            SamplingTime closestTime = SamplingTime.Create(closestMatch);

            DebugWriteUnblittableObject(ref candidates);
            DebugWriteBlittableObject(ref samplingTime);
            DebugWriteBlittableObject(ref closestTime, true);

            if (IsDebugging)
            {
                DebugWriteUnblittableObject(ref deviationTable);
            }

            FindClosestPoseTrajectoryDebug debugObject = new FindClosestPoseTrajectoryDebug()
            {
                trajectory = DebugIdentifier.Invalid,
                candidates = candidates.debugIdentifier,
                samplingTime = samplingTime.debugIdentifier,
                closestMatch = closestTime.debugIdentifier,
                deviationTable = IsDebugging ? deviationTable.debugIdentifier : DebugIdentifier.Invalid,
                trajectoryWeight = -1.0f,
                maxDeviation = maxPoseDeviation,
                deviated = maxPoseDeviation >= 0.0f && minimumDeviation > maxPoseDeviation,
                debugName = candidates.debugName
            };
            writeDebugMemory.AddCostRecord(debugObject.DebugName, minimumDeviation, -1.0f);

            if (IsDebugging)
            {
                deviationTable.Dispose();
            }

            DebugWriteUnblittableObject(ref debugObject);

            if (maxPoseDeviation >= 0.0f && minimumDeviation > maxPoseDeviation)
            {
                closestTime = SamplingTime.Invalid;
            }

            return closestTime;
        }

        public unsafe SamplingTime FindClosestPoseAndTrajectoryMatch(PoseSet candidates,
            SamplingTime samplingTime,
            Trajectory trajectory,
            float trajectoryWeight = 0.5f,
            float maxDeviation = -1.0f)
        {
            if (!samplingTime.IsValid)
            {
                return SamplingTime.Create(SelectFirstPose(candidates.sequences));
            }

            ref Binary binary = ref Binary;

            var intervals = binary.GetIntervals();


            AdjustSequenceBoundaries(candidates.sequences);

            DeviationTable deviationTable = IsDebugging ? DeviationTable.Create(candidates) : DeviationTable.CreateInvalid();

            var query = MatchFragmentQuery.Create(ref binary);

            int numCodeBooks = binary.numCodeBooks;

            int numCodeValues = Binary.CodeBook.kNumCodeValues;

            var poseWeight = 1.0f - trajectoryWeight;

            for (int i = 0; i < numCodeBooks; ++i)
            {
                if (RefersToCodeBook(candidates.sequences, i))
                {
                    query.DecodePose(
                        ref binary, i, samplingTime.timeIndex);

                    query.GeneratePoseDistanceTable(
                        ref binary, i, poseWeight);

                    query.DecodeTrajectory(
                        ref binary, i, trajectory);

                    query.GenerateTrajectoryDistanceTable(
                        ref binary, i, trajectoryWeight);
                }
            }

            float minimumDeviation = float.MaxValue;
            float minPoseDeviation = -1.0f;
            float minTrajectoryDeviation = -1.0f;

            TimeIndex closestMatch = TimeIndex.Invalid;

            var numSequences = candidates.sequences.Length;

            for (int i = 0; i < numSequences; ++i)
            {
                var intervalIndex = candidates.sequences[i].intervalIndex;

                ref var interval = ref binary.GetInterval(intervalIndex);

                Assert.IsTrue(interval.Contains(candidates.sequences[i].firstFrame));

                var codeBookIndex = interval.codeBookIndex;
                if (codeBookIndex.value < 0)
                {
                    throw new NoMetricException(ref this, interval.segmentIndex);
                }

                var segmentIndex = (short)interval.segmentIndex;

                ref var codeBook = ref binary.GetCodeBook(codeBookIndex);

                var intervalFragmentIndex =
                    codeBook.GetFragmentIndex(
                        intervals, intervalIndex);

                Assert.IsTrue(intervalFragmentIndex >= 0);

                var fragmentIndex =
                    intervalFragmentIndex + candidates.sequences[i].firstFrame;

                Assert.IsTrue(fragmentIndex < codeBook.numFragments);

                var numFrames = candidates.sequences[i].numFrames;

                Assert.IsTrue(numFrames <= intervals[intervalIndex].numFrames);

                Assert.IsTrue((fragmentIndex + numFrames) <= codeBook.numFragments);

                float* poseDistances =
                    query.GetPoseDistances(codeBookIndex);

                float* trajectoryDistances =
                    query.GetTrajectoryDistances(codeBookIndex);

                short frameIndex = (short)(candidates.sequences[i].firstFrame);

                int numPoseFeatures = codeBook.poses.numFeaturesFlattened;
                int numTrajectoryFeatures = codeBook.trajectories.numFeaturesFlattened;

                byte* poseCodes =
                    (byte*)codeBook.poses.codes.GetUnsafePtr() +
                    numPoseFeatures * fragmentIndex;

                byte* trajectoryCodes =
                    (byte*)codeBook.trajectories.codes.GetUnsafePtr() +
                    numTrajectoryFeatures * fragmentIndex;

                float* ptr;

                for (int j = 0; j < numFrames; ++j)
                {
                    float poseDeviation = 0.0f;

                    ptr = poseDistances;

                    for (int k = 0; k < numPoseFeatures; ++k)
                    {
                        poseDeviation += ptr[*poseCodes++];

                        ptr += numCodeValues;
                    }

                    float trajectoryDeviation = 0.0f;

                    ptr = trajectoryDistances;

                    for (int k = 0; k < numTrajectoryFeatures; ++k)
                    {
                        trajectoryDeviation += ptr[*trajectoryCodes++];

                        ptr += numCodeValues;
                    }

                    if (IsDebugging)
                    {
                        deviationTable.SetDeviation(i, j, poseDeviation, trajectoryDeviation);
                    }

                    float candidateDeviation = poseDeviation + trajectoryDeviation;

                    if (candidateDeviation < minimumDeviation)
                    {
                        minimumDeviation = candidateDeviation;
                        minPoseDeviation = poseDeviation;
                        minTrajectoryDeviation = trajectoryDeviation;

                        closestMatch.segmentIndex = segmentIndex;
                        closestMatch.frameIndex = frameIndex;
                    }

                    fragmentIndex++;
                    frameIndex++;
                }
            }

            query.Dispose();

            SamplingTime closestTime = SamplingTime.Create(closestMatch);

            DebugWriteUnblittableObject(ref trajectory);
            DebugWriteUnblittableObject(ref candidates);
            DebugWriteBlittableObject(ref samplingTime);
            DebugWriteBlittableObject(ref closestTime, true);

            if (IsDebugging)
            {
                DebugWriteUnblittableObject(ref deviationTable);
            }

            FindClosestPoseTrajectoryDebug debugObject = new FindClosestPoseTrajectoryDebug()
            {
                trajectory = trajectory.debugIdentifier,
                candidates = candidates.debugIdentifier,
                samplingTime = samplingTime.debugIdentifier,
                closestMatch = closestTime.debugIdentifier,
                deviationTable = IsDebugging ? deviationTable.debugIdentifier : DebugIdentifier.Invalid,
                trajectoryWeight = trajectoryWeight,
                maxDeviation = maxDeviation,
                deviated = maxDeviation >= 0 && minimumDeviation > maxDeviation,
                debugName = candidates.debugName
            };
            writeDebugMemory.AddCostRecord(debugObject.DebugName, minPoseDeviation, minTrajectoryDeviation);

            if (IsDebugging)
            {
                deviationTable.Dispose();
            }

            DebugWriteUnblittableObject(ref debugObject);

            if (maxDeviation >= 0 && minimumDeviation > maxDeviation)
            {
                closestTime = SamplingTime.Invalid;
            }


            return closestTime;
        }

        public TimeIndex SelectFirstPose(NativeArray<PoseSequence> sequences)
        {
            if (sequences.Length > 0)
            {
                ref var interval = ref Binary.GetInterval(sequences[0].intervalIndex);

                return interval.GetTimeIndex(sequences[0].firstFrame);
            }

            return TimeIndex.Invalid;
        }

        void AdjustSequenceBoundaries(NativeArray<PoseSequence> sequences)
        {
            ref Binary binary = ref Binary;

            var numSequences = sequences.Length;

            var timeHorizon = binary.TimeHorizon;

            var sampleRate = binary.SampleRate;

            var numBoundaryFrames = Missing.truncToInt(timeHorizon * sampleRate);

            for (int i = 0; i < numSequences; ++i)
            {
                var sequence = sequences[i];

                var intervalIndex = sequence.intervalIndex;

                ref var interval = ref binary.GetInterval(intervalIndex);

                Assert.IsTrue(interval.Contains(sequence.firstFrame));

                ref var segment = ref binary.GetSegment(interval.segmentIndex);

                var onePastLastFrame = sequence.onePastLastFrame;

                var boundaryFrameLeft = numBoundaryFrames;
                if (segment.previousSegment.IsValid)
                {
                    boundaryFrameLeft -= binary.GetSegment(segment.previousSegment).destination.numFrames;
                }

                var boundaryFrameRight = segment.destination.numFrames - numBoundaryFrames;
                if (segment.nextSegment.IsValid)
                {
                    boundaryFrameRight += binary.GetSegment(segment.nextSegment).destination.numFrames;
                }

                sequence.firstFrame = math.max(boundaryFrameLeft, sequence.firstFrame);

                onePastLastFrame = math.min(boundaryFrameRight, onePastLastFrame);

                sequence.numFrames = onePastLastFrame - sequence.firstFrame;

                if (sequence.numFrames <= 0)
                {
                    bool intervalValid = false;

                    ref Binary.TagList tagList = ref binary.GetTagList(interval.tagListIndex);
                    for (int j = 0; j < tagList.numIndices; ++j)
                    {
                        Binary.TagIndex tagIndex = binary.GetTagIndex(ref tagList, j);
                        ref Binary.Tag tag = ref binary.GetTag(tagIndex);
                        int tagFirstFrame = math.max(boundaryFrameLeft, tag.FirstFrame);
                        int tagOnePastLastFrame = math.max(boundaryFrameRight, tag.OnePastLastFrame);

                        if (tagFirstFrame < tagOnePastLastFrame)
                        {
                            // intersection is too short, but one of the tags it contains is long enough, which is enough to make the intersection valid
                            intervalValid = true;
                            break;
                        }
                    }

                    if (!intervalValid)
                    {
                        throw new SegmentTooShortException(ref this, interval.segmentIndex);
                    }

                    continue;
                }

                sequences[i] = sequence;
            }
        }

        bool RefersToCodeBook(NativeArray<PoseSequence> sequences, int codeBookIndex)
        {
            ref Binary binary = ref Binary;

            var numSequences = sequences.Length;

            for (int i = 0; i < numSequences; ++i)
            {
                var intervalIndex = sequences[i].intervalIndex;

                ref var interval = ref binary.GetInterval(intervalIndex);

                if (interval.codeBookIndex == codeBookIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
