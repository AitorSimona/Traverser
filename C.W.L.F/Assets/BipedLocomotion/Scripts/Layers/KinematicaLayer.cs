using Unity.Kinematica;

// --- Wrapper for ability-kinematica interactions ---

namespace CWLF
{
    public static class KinematicaLayer
    {

        // --- Utilities ---
        public static bool UpdateAnchoredTransition(ref AnchoredTransitionTask anchoredTransition, ref Kinematica kinematica)
        {
            // --- Update a valid anchored transition until it is completed or failed ---
            bool ret = false;

            if (anchoredTransition.isValid)
            {
                ref MotionSynthesizer synthesizer = ref kinematica.Synthesizer.Ref;

                if (!anchoredTransition.IsState(AnchoredTransitionTask.State.Complete) && !anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
                {
                    anchoredTransition.synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);

                    // --- Finally tell kinematica to wait for this job to finish before executing other stuff ---
                    kinematica.AddJobDependency(AnchoredTransitionJob.Schedule(ref anchoredTransition));

                    ret = true; // calling ability will keep control
                }
                else
                {
                    anchoredTransition.Dispose();
                    anchoredTransition = AnchoredTransitionTask.Invalid;
                }
            }

            return ret;
        }

        public static bool IsAnchoredTransitionComplete(ref AnchoredTransitionTask anchoredTransition, out bool TransitionSuccess)
        {
            bool ret = true;
            TransitionSuccess = false;
            bool active = anchoredTransition.isValid;

            // --- Check if current transition has been completed ---

            if (active)
            {
                if (anchoredTransition.IsState(AnchoredTransitionTask.State.Complete))
                {
                    anchoredTransition.Dispose();
                    anchoredTransition = AnchoredTransitionTask.Invalid;
                    TransitionSuccess = true;
                    ret = true;
                }
                else if (anchoredTransition.IsState(AnchoredTransitionTask.State.Failed))
                {
                    anchoredTransition.Dispose();
                    anchoredTransition = AnchoredTransitionTask.Invalid;
                    ret = true;
                }
                else
                {
                    ret = false;
                }
            }

            return ret;
        }

        public static Binary.TypeIndex GetCurrentAnimationInfo(ref MotionSynthesizer synthesizer, out bool hasReachedEndOfSegment)
        {
            // --- Retrieve current animation type and whether is has finished ---
            ref Binary binary = ref synthesizer.Binary;
            hasReachedEndOfSegment = false;

            SamplingTime samplingTime = synthesizer.Time;

            ref Binary.Segment segment = ref binary.GetSegment(samplingTime.timeIndex.segmentIndex);
            ref Binary.Tag tag = ref binary.GetTag(segment.tagIndex);
            ref Binary.Trait trait = ref binary.GetTrait(tag.traitIndex);

            if (samplingTime.timeIndex.frameIndex >= segment.destination.numFrames - 1)
                hasReachedEndOfSegment = true;

            return trait.typeIndex;
        }

        // --------------------------------
    }
}
