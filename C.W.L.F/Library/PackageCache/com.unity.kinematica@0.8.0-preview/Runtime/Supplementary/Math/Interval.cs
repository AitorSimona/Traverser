using UnityEngine.Assertions;
using Unity.Mathematics;

namespace Unity.Kinematica
{
    /// <summary>
    /// Structure representing a contiguous interval of frames.
    /// </summary>
    public struct Interval
    {
        int firstFrame;
        int onePastLastFrame;

        public static Interval Create(int firstFrame, int onePastLastFrame)
        {
            return new Interval(firstFrame, onePastLastFrame);
        }

        public Interval(int firstFrame, int onePastLastFrame)
        {
            this.firstFrame = firstFrame;
            this.onePastLastFrame = onePastLastFrame;

            Assert.IsTrue(onePastLastFrame >= firstFrame);
        }

        public Interval(int firstFrame)
        {
            this.firstFrame = firstFrame;
            onePastLastFrame = firstFrame + 1;

            Assert.IsTrue(onePastLastFrame >= firstFrame);
        }

        public static Interval Empty
        {
            get { return new Interval(0, 0); }
        }

        public bool IsEmpty()
        {
            return firstFrame == onePastLastFrame;
        }

        public bool Contains(int frame)
        {
            return (frame >= firstFrame && frame <= onePastLastFrame);
        }

        public bool Overlaps(int firstFrame_, int onePastLastFrame_)
        {
            // The key to a better approach is inverting the sense of the question:
            // instead of asking whether two intervals overlap, try to find out when they don’t.
            // Now, intervals don’t have holes. So if two intervals I_a = [a_0, a_1] and I_b = [b_0, b_1]
            // don’t overlap, that means that I_b must be either fully to the left or fully to the right
            // of I_a on the real number line. Now, if I_b is fully to the left of I_a, that means in
            // particular that b’s rightmost point b_1 must be to the left of a – that is, smaller than a_0.
            // And again, vice versa for the right side. So the two intervals don’t overlap if either
            // b_1 < a_0 or a_1 < b_0. Applying that to our original problem (which involves negating
            // the whole expression using de Morgan's laws), this gives the following version of the interval overlap check:
            return firstFrame < onePastLastFrame_ && firstFrame_ < onePastLastFrame;
        }

        public bool Overlaps(Interval rhs)
        {
            return Overlaps(rhs.firstFrame, rhs.onePastLastFrame);
        }

        public bool Adjacent(Interval other)
        {
            return (OnePastLastFrame == other.FirstFrame) || (FirstFrame == other.OnePastLastFrame);
        }

        public bool OverlapsOrAdjacent(Interval other)
        {
            return Overlaps(other) || Adjacent(other);
        }

        public bool Adjacent(int firstFrame_, int onePastLastFrame_)
        {
            return (OnePastLastFrame == firstFrame_) || (FirstFrame == onePastLastFrame_);
        }

        public bool OverlapsOrAdjacent(int firstFrame_, int onePastLastFrame_)
        {
            return Overlaps(firstFrame_, onePastLastFrame_) || Adjacent(firstFrame_, onePastLastFrame_);
        }

        public bool Contains(Interval rhs)
        {
            return Contains(rhs.firstFrame) && Contains(rhs.onePastLastFrame);
        }

        public bool Equals(Interval rhs)
        {
            return (FirstFrame == rhs.FirstFrame &&
                OnePastLastFrame == rhs.OnePastLastFrame);
        }

        public Interval Intersection(Interval rhs)
        {
            Assert.IsTrue(rhs.onePastLastFrame >= rhs.firstFrame);
            Assert.IsTrue(OverlapsOrAdjacent(rhs.firstFrame, rhs.onePastLastFrame));

            return new Interval(
                math.max(rhs.firstFrame, firstFrame),
                math.min(rhs.onePastLastFrame, onePastLastFrame));
        }

        public Interval Union(int start, int end)
        {
            Assert.IsTrue(end >= start);
            Assert.IsTrue(OverlapsOrAdjacent(start, end));

            return new Interval(
                math.min(start, firstFrame),
                math.max(end, onePastLastFrame));
        }

        public void Union(Interval rhs)
        {
            this = Union(rhs.firstFrame, rhs.onePastLastFrame);
        }

        public static Interval Union(Interval lhs, Interval rhs)
        {
            return lhs.Union(rhs.firstFrame, rhs.onePastLastFrame);
        }

        public int FirstFrame
        {
            get
            {
                return firstFrame;
            }
        }

        public int NumFrames
        {
            get
            {
                Assert.IsTrue(onePastLastFrame >= firstFrame);
                return onePastLastFrame - firstFrame;
            }

            set
            {
                Assert.IsTrue(value >= 0);
                onePastLastFrame = firstFrame + value;
            }
        }

        public int OnePastLastFrame
        {
            get
            {
                return onePastLastFrame;
            }
        }
    }
}
