using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.SnapshotDebugger;
using Buffer = Unity.SnapshotDebugger.Buffer;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    internal struct DeviationScore
    {
        public float poseDeviation;
        public float trajectoryDeviation;

        public float TotalDeviation => math.max(poseDeviation, 0.0f) + math.max(trajectoryDeviation, 0.0f);

        public bool IsValid => poseDeviation >= 0.0f;

        public static DeviationScore CreateInvalid() => new DeviationScore() { poseDeviation = -1.0f, trajectoryDeviation = -1.0f };
    }

    internal struct DeviationTable : IDebugObject, Serializable, IDisposable
    {
        internal struct PoseSequenceInfo
        {
            public PoseSequence sequence;
            public int firstDeviationIndex;
        }

        Allocator allocator;
        NativeArray<PoseSequenceInfo> sequences;
        NativeArray<DeviationScore> deviations;

        public DebugIdentifier debugIdentifier { get; set; }

        public bool IsValid => allocator != Allocator.Invalid;

        internal static DeviationTable CreateInvalid()
        {
            return new DeviationTable()
            {
                allocator = Allocator.Invalid
            };
        }

        internal static DeviationTable Create(PoseSet candidates)
        {
            int numDeviations = 0;
            for (int i = 0; i < candidates.sequences.Length; ++i)
            {
                numDeviations += candidates.sequences[i].numFrames;
            }

            DeviationTable table = new DeviationTable()
            {
                allocator = Allocator.Temp,
                sequences = new NativeArray<PoseSequenceInfo>(candidates.sequences.Length, Allocator.Temp),
                deviations = new NativeArray<DeviationScore>(numDeviations, Allocator.Temp)
            };

            int deviationIndex = 0;
            for (int i = 0; i < candidates.sequences.Length; ++i)
            {
                table.sequences[i] = new PoseSequenceInfo()
                {
                    sequence = candidates.sequences[i],
                    firstDeviationIndex = deviationIndex
                };

                deviationIndex += candidates.sequences[i].numFrames;
            }

            for (int i = 0; i < numDeviations; ++i)
            {
                table.deviations[i] = DeviationScore.CreateInvalid();
            }

            return table;
        }

        public void Dispose()
        {
            if (IsValid)
            {
                sequences.Dispose();
                deviations.Dispose();
            }
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.WriteNativeArray(sequences, allocator);
            buffer.WriteNativeArray(deviations, allocator);
        }

        public void ReadFromStream(Buffer buffer)
        {
            sequences = buffer.ReadNativeArray<PoseSequenceInfo>(out allocator);
            deviations = buffer.ReadNativeArray<DeviationScore>(out _);
        }

        internal void SetDeviation(int sequenceIndex, int frameIndex, float poseDeviation, float trajectoryDeviation)
        {
            Assert.IsTrue(sequenceIndex < sequences.Length);
            Assert.IsTrue(frameIndex < sequences[sequenceIndex].sequence.numFrames);

            int deviationIndex = sequences[sequenceIndex].firstDeviationIndex + frameIndex;
            deviations[deviationIndex] = new DeviationScore()
            {
                poseDeviation = poseDeviation,
                trajectoryDeviation = trajectoryDeviation
            };
        }

        internal DeviationScore GetDeviation(ref Binary binary, TimeIndex timeIndex)
        {
            for (int i = 0; i < sequences.Length; ++i)
            {
                PoseSequenceInfo sequenceInfo = sequences[i];
                ref Binary.Interval interval = ref binary.GetInterval(sequenceInfo.sequence.intervalIndex);
                if (interval.Contains(timeIndex))
                {
                    if (timeIndex.frameIndex >= sequenceInfo.sequence.firstFrame && timeIndex.frameIndex < sequenceInfo.sequence.onePastLastFrame)
                    {
                        int deviationIndex = sequenceInfo.firstDeviationIndex + timeIndex.frameIndex - sequenceInfo.sequence.firstFrame;
                        return deviations[deviationIndex];
                    }
                }
            }

            return DeviationScore.CreateInvalid();
        }

        internal List<Tuple<TimeIndex, DeviationScore>> SortCandidatesByDeviation(ref Binary binary)
        {
            List<Tuple<TimeIndex, DeviationScore>> candidates = new List<Tuple<TimeIndex, DeviationScore>>(deviations.Length);

            for (int i = 0; i < sequences.Length; ++i)
            {
                PoseSequence sequence = sequences[i].sequence;
                ref Binary.Interval interval = ref binary.GetInterval(sequence.intervalIndex);
                for (int frame = 0; frame < sequence.numFrames; ++frame)
                {
                    DeviationScore deviation = deviations[sequences[i].firstDeviationIndex + frame];
                    if (deviation.IsValid)
                    {
                        TimeIndex timeIndex = TimeIndex.Create(interval.segmentIndex, sequence.firstFrame + frame);

                        Tuple<TimeIndex, DeviationScore> candidate = new Tuple<TimeIndex, DeviationScore>(timeIndex, deviation);
                        candidates.Add(candidate);
                    }
                }
            }

            candidates.Sort((x, y) => x.Item2.TotalDeviation.CompareTo(y.Item2.TotalDeviation));

            return candidates;
        }
    }
}
