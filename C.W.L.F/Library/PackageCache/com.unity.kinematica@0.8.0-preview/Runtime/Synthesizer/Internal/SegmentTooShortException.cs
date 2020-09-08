using System;

namespace Unity.Kinematica
{
    class SegmentTooShortException : Exception
    {
        public SegmentTooShortException(ref MotionSynthesizer synthesizer, int segmentIndex)
        {
            m_Synthesizer = MemoryRef<MotionSynthesizer>.Create(ref synthesizer);
            m_SegmentIndex = segmentIndex;
        }

        public override string Message
        {
            get
            {
                ref Binary binary = ref m_Synthesizer.Ref.Binary;

                ref var segment = ref binary.GetSegment(m_SegmentIndex);
                string clipName = binary.GetString(segment.nameIndex);

                return $"Segment from clip {clipName} at frames [{segment.source.FirstFrame},{segment.source.OnePastLastFrame - 1}] is too short";
            }
        }

        MemoryRef<MotionSynthesizer> m_Synthesizer;
        int m_SegmentIndex;
    }
}
