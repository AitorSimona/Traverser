using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;

using Unity.Mathematics;
using System;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public class Segment
        {
            public int segmentIndex;

            public TaggedAnimationClip clip;

            public Interval source;
            public Interval destination;

            public int tagIndex;
            public int numTags;

            public int markerIndex;
            public int numMarkers;

            public int intervalIndex;
            public int numIntervals;

            public List<TagAnnotation> tags = new List<TagAnnotation>();

            public int Remap(int frameIndex)
            {
                Assert.IsTrue(frameIndex >= source.FirstFrame);
                Assert.IsTrue(frameIndex <= source.OnePastLastFrame);
                int sourceOffset = frameIndex - source.FirstFrame;
                float ratio = sourceOffset / (float)source.NumFrames;
                int destinationOffset = Missing.roundToInt(ratio * destination.NumFrames);
                Assert.IsTrue(destinationOffset <= destination.NumFrames);
                return destination.FirstFrame + destinationOffset;
            }

            public Avatar GetSourceAvatar(Asset asset)
            {
                return clip.GetSourceAvatar(asset.DestinationAvatar);
            }
        }

        public class ClipSegments
        {
            TaggedAnimationClip clip;

            List<Segment> segments = new List<Segment>();

            public static ClipSegments Create()
            {
                return new ClipSegments();
            }

            public int NumSegments => segments.Count;

            public TaggedAnimationClip Clip => clip;

            public Segment this[int index] => segments[index];

            public Segment[] ToArray() => segments.ToArray();

            public int FindFirstSegmentIndex()
            {
                int segmentIndex = Binary.SegmentIndex.Invalid;

                int frameIndex = int.MaxValue;

                for (int i = 0; i < NumSegments; ++i)
                {
                    if (segments[i].destination.FirstFrame < frameIndex)
                    {
                        frameIndex = segments[i].destination.FirstFrame;
                        segmentIndex = i;
                    }
                }

                return segmentIndex;
            }

            public int FindLastSegmentIndex()
            {
                int segmentIndex = Binary.SegmentIndex.Invalid;

                int frameIndex = -1;

                for (int i = 0; i < NumSegments; ++i)
                {
                    if (segments[i].destination.FirstFrame > frameIndex)
                    {
                        frameIndex = segments[i].destination.FirstFrame;
                        segmentIndex = i;
                    }
                }

                return segmentIndex;
            }

            public static List<ClipSegments> GenerateSegments(Asset asset, out int numSegments, out int numFrames)
            {
                List<ClipSegments> segments = new List<ClipSegments>();

                int destinationIndex = 0;
                numSegments = 0;
                numFrames = 0;

                foreach (var animationClip in asset.GetAnimationClips())
                {
                    ClipSegments clipSegments = new ClipSegments()
                    {
                        clip = animationClip,
                        segments = GenerateSegments(asset, animationClip)
                    };

                    numSegments += clipSegments.NumSegments;

                    foreach (var segment in clipSegments.segments)
                    {
                        Assert.IsTrue(segment.source.FirstFrame >= 0);
                        Assert.IsTrue(segment.source.OnePastLastFrame <= animationClip.NumFrames);

                        int numFramesSource = segment.source.NumFrames;
                        int numFramesDestination = animationClip.ClipFramesToAssetFrames(asset, numFramesSource);

                        int onePastLastFrame = destinationIndex + numFramesDestination;

                        numFrames += numFramesDestination;

                        segment.destination =
                            new Interval(destinationIndex, onePastLastFrame);

                        destinationIndex += numFramesDestination;
                    }

                    segments.Add(clipSegments);
                }

                return segments;
            }

            static List<Segment> GenerateSegments(Asset asset, TaggedAnimationClip animationClip)
            {
                var segments = new List<Segment>();

                var tags = animationClip.Tags.ToList();

                tags.Sort((x, y) => x.startTime.CompareTo(y.startTime));

                foreach (TagAnnotation tag in tags)
                {
                    float startTime = math.max(0.0f, tag.startTime);
                    float endTime = math.min(animationClip.DurationInSeconds, tag.EndTime);
                    float duration = endTime - startTime;

                    if (duration <= 0.0f)
                    {
                        Debug.LogWarning($"One '{tag.Name}' tag is laying entirely outside of '{animationClip.ClipName}' clip range, it will therefore be ignored. (Tag starting at {tag.startTime:0.00} seconds, whose duration is {tag.duration:0.00} seconds).");
                        continue;
                    }

                    int firstFrame = animationClip.ClampedTimeInSecondsToIndex(startTime);
                    int numFrames = animationClip.ClampedDurationInSecondsToFrames(duration);

                    int onePastLastFrame =
                        math.min(firstFrame + numFrames,
                            animationClip.NumFrames);

                    var interval = new Interval(firstFrame, onePastLastFrame);

                    var segment = new Segment
                    {
                        clip = animationClip,
                        source = interval,
                        destination = Interval.Empty
                    };

                    segment.tags.Add(tag);

                    segments.Add(segment);
                }

                Coalesce(segments);

                return segments;
            }

            static void Coalesce(List<Segment> segments)
            {
                int numSegmentsMinusOne = segments.Count - 1;

                for (int i = 0; i < numSegmentsMinusOne; ++i)
                {
                    if (segments[i].source.OverlapsOrAdjacent(segments[i + 1].source))
                    {
                        segments[i].tags.AddRange(segments[i + 1].tags);
                        segments[i].source.Union(segments[i + 1].source);

                        segments.RemoveAt(i + 1);

                        numSegmentsMinusOne--;
                        i--;
                    }
                }
            }
        }

        Segment GetSegment(int segmentIndex)
        {
            for (int i = 0; i < clipSegments.Count; ++i)
            {
                if (segmentIndex < clipSegments[i].NumSegments)
                {
                    return clipSegments[i][segmentIndex];
                }
                else
                {
                    segmentIndex -= clipSegments[i].NumSegments;
                }
            }

            return null;
        }

        bool DoesSegmentCoverMetric(Segment segment)
        {
            foreach (TagAnnotation tag in segment.tags)
            {
                string tagName = TagAnnotation.GetTagTypeFullName(tag);

                foreach (Asset.Metric metric in asset.Metrics)
                {
                    if (metric.TagTypes.Contains(tagName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        bool IsTagAssociatedToMetric(TagAnnotation tag)
        {
            string tagName = TagAnnotation.GetTagTypeFullName(tag);
            foreach (Asset.Metric metric in asset.Metrics)
            {
                if (metric.TagTypes.Contains(tagName))
                {
                    return true;
                }
            }
            return false;
        }

        void CheckTagsAreLongEnough()
        {
            ref Binary binary = ref Binary;

            int numBoundaryFrames = Missing.truncToInt(asset.TimeHorizon * asset.SampleRate);

            bool hasShortTag = false;

            foreach (ClipSegments segments in clipSegments)
            {
                for (int i = 0; i < segments.NumSegments; ++i)
                {
                    Segment segment = segments[i];

                    if (!DoesSegmentCoverMetric(segment))
                    {
                        continue;
                    }

                    int segmentFirstFrame = segment.clip.ClipFramesToAssetFrames(asset, segment.source.FirstFrame);
                    int segmentOnePastLastFrame = segment.clip.ClipFramesToAssetFrames(asset, segment.source.OnePastLastFrame);

                    int prevSegmentFrames = 0;
                    Binary.SegmentIndex prevSegmentIndex = binary.segments[i].previousSegment;
                    if (prevSegmentIndex.IsValid)
                    {
                        Segment prevSegment = GetSegment(prevSegmentIndex);
                        if (DoesSegmentCoverMetric(prevSegment))
                        {
                            prevSegmentFrames = segment.clip.ClipFramesToAssetFrames(asset, prevSegment.source.NumFrames);
                        }
                    }

                    int nextSegmentFrames = 0;
                    Binary.SegmentIndex nextSegmentIndex = binary.segments[i].nextSegment;
                    if (nextSegmentIndex.IsValid)
                    {
                        Segment nextSegment = GetSegment(nextSegmentIndex);
                        if (DoesSegmentCoverMetric(nextSegment))
                        {
                            nextSegmentFrames = segment.clip.ClipFramesToAssetFrames(asset, nextSegment.source.NumFrames);
                        }
                    }

                    segmentFirstFrame += math.max(numBoundaryFrames - prevSegmentFrames, 0);
                    segmentOnePastLastFrame -= math.max(numBoundaryFrames - nextSegmentFrames, 0);

                    for (int j = 0; j < segment.tags.Count; ++j)
                    {
                        TagAnnotation tag = segment.tags[j];

                        if (!IsTagAssociatedToMetric(tag))
                        {
                            continue;
                        }

                        int firstFrame = segment.clip.ClipFramesToAssetFrames(asset, segment.clip.ClampedTimeInSecondsToIndex(tag.startTime));
                        int onePastLastFrame = firstFrame + segment.clip.ClipFramesToAssetFrames(asset, segment.clip.ClampedDurationInSecondsToFrames(tag.duration));

                        int firstValidFrame = math.max(firstFrame, segmentFirstFrame);
                        int onePastLastValidFrame = math.min(onePastLastFrame, segmentOnePastLastFrame);

                        if (firstValidFrame >= onePastLastValidFrame)
                        {
                            int tagNumFrames = onePastLastFrame - firstFrame;
                            int missingFrames = firstValidFrame + 1 - onePastLastValidFrame;

                            Debug.LogError($"Tag [{firstFrame},{onePastLastFrame - 1}] from clip {segment.clip.ClipName} is too short. It should be at least {tagNumFrames + missingFrames} frames but is only {tagNumFrames}");

                            hasShortTag = true;
                        }
                    }
                }
            }

            if (hasShortTag)
            {
                throw new Exception($"One or more tags are too short.");
            }
        }

        public int FindFirstSegmentIndex(TaggedAnimationClip clip)
        {
            int segmentIndex = 0;
            for (int i = 0; i < clipSegments.Count; ++i)
            {
                if (clipSegments[i].Clip == clip)
                {
                    return segmentIndex + clipSegments[i].FindFirstSegmentIndex();
                }
                else
                {
                    segmentIndex += clipSegments[i].NumSegments;
                }
            }

            return -1;
        }

        public int FindLastSegmentIndex(TaggedAnimationClip clip)
        {
            int segmentIndex = 0;
            for (int i = 0; i < clipSegments.Count; ++i)
            {
                if (clipSegments[i].Clip == clip)
                {
                    return segmentIndex + clipSegments[i].FindLastSegmentIndex();
                }
                else
                {
                    segmentIndex += clipSegments[i].NumSegments;
                }
            }

            return -1;
        }

        ClipSegments FindClipSegments(TaggedAnimationClip clip)
        {
            for (int i = 0; i < clipSegments.Count; ++i)
            {
                if (clipSegments[i].Clip == clip)
                {
                    return clipSegments[i];
                }
            }

            return null;
        }

        public void BuildSegments()
        {
            clipSegments = ClipSegments.GenerateSegments(asset, out numSegments, out numFrames);

            ref Binary binary = ref Binary;

            allocator.Allocate(numSegments, ref binary.segments);

            int segmentIndex = 0;
            for (int i = 0; i < clipSegments.Count; ++i)
            {
                ClipSegments segments = clipSegments[i];

                var clip = segments.Clip;

                var nameIndex = stringTable.RegisterString(clip.ClipName);

                var guid = clip.AnimationClipGuid;

                for (int j = 0; j < segments.NumSegments; ++j)
                {
                    binary.segments[segmentIndex].nameIndex = nameIndex;
                    binary.segments[segmentIndex].guid = guid;
                    binary.segments[segmentIndex].source.firstFrame = segments[j].source.FirstFrame;
                    binary.segments[segmentIndex].source.numFrames = segments[j].source.NumFrames;
                    binary.segments[segmentIndex].destination.firstFrame = segments[j].destination.FirstFrame;
                    binary.segments[segmentIndex].destination.numFrames = segments[j].destination.NumFrames;

                    if (clip.TaggedPreBoundaryClip != null)
                    {
                        binary.segments[segmentIndex].previousSegment = FindLastSegmentIndex(clip.TaggedPreBoundaryClip);
                    }
                    else
                    {
                        binary.segments[segmentIndex].previousSegment = Binary.SegmentIndex.Invalid;
                    }

                    if (clip.TaggedPostBoundaryClip != null)
                    {
                        binary.segments[segmentIndex].nextSegment = FindFirstSegmentIndex(clip.TaggedPreBoundaryClip);
                    }
                    else
                    {
                        binary.segments[segmentIndex].nextSegment = Binary.SegmentIndex.Invalid;
                    }

                    segments[j].segmentIndex = segmentIndex;

                    ++segmentIndex;
                }
            }

            CheckTagsAreLongEnough();
        }
    }
}
