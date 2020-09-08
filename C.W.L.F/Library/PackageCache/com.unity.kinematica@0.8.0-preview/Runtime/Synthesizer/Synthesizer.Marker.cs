using System;

using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine.Assertions;

using TraitIndex = Unity.Kinematica.Binary.TraitIndex;
using SegmentIndex = Unity.Kinematica.Binary.SegmentIndex;

namespace Unity.Kinematica
{
    public partial struct MotionSynthesizer
    {
        internal struct Any{}

        delegate void TraitExecuteFunc(
            ref Any explicitThis, ref MotionSynthesizer synthesizer);

        void TriggerMarkers(TimeIndex from, TimeIndex to)
        {
            Assert.IsTrue(from.segmentIndex == to.segmentIndex);

            int firstFrame = from.frameIndex;
            int onePastLastFrame = to.frameIndex;

            if (lastSamplingTime.segmentIndex == samplingTime.segmentIndex)
            {
                firstFrame = math.max(firstFrame,
                    lastSamplingTime.frameIndex + 1);
            }

            if (onePastLastFrame >= firstFrame)
            {
                TriggerMarkers(
                    samplingTime.segmentIndex, Interval.Create(
                        firstFrame, onePastLastFrame));

                lastSamplingTime = to;
            }
        }

        void TriggerMarkers(SegmentIndex segmentIndex, Interval interval)
        {
            ref Binary binary = ref Binary;

            int numMarkers = binary.numMarkers;

            for (int i = 0; i < numMarkers; ++i)
            {
                ref var marker = ref binary.GetMarker(i);

                if (marker.segmentIndex == segmentIndex)
                {
                    if (interval.Contains(marker.frameIndex))
                    {
                        TriggerTrait(marker.traitIndex);
                    }
                }
            }
        }

        unsafe void TriggerTrait(TraitIndex traitIndex)
        {
            ref Binary binary = ref Binary;

            ref var trait = ref binary.GetTrait(traitIndex);

            int numTraitTypes = traitTypes.Length;

            for (int i = 0; i < numTraitTypes; ++i)
            {
                if (traitTypes[i].typeIndex == trait.typeIndex)
                {
                    var executeFunction =
                        traitTypes[i].executeFunction;

                    if (executeFunction != IntPtr.Zero)
                    {
                        var functionPointer =
                            new FunctionPointer<TraitExecuteFunc>(
                                executeFunction);

                        int numBytes = binary.GetType(trait.typeIndex).numBytes;

                        byte* payloadPtr =
                            (byte*)binary.payloads.GetUnsafePtr() +
                            trait.payload;

                        functionPointer.Invoke(
                            ref UnsafeUtilityEx.AsRef<Any>(
                                payloadPtr), ref this);
                    }
                }
            }
        }
    }
}
