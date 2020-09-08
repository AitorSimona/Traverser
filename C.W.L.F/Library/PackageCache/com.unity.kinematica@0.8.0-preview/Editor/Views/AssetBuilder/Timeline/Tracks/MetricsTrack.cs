using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MetricsTrack : GutterTrack
    {
        Asset m_TargetAsset;

        Asset TargetAsset
        {
            get { return m_TargetAsset;}
            set
            {
                if (m_TargetAsset != value)
                {
                    if (m_TargetAsset != null)
                    {
                        m_TargetAsset.MetricsModified -= ReloadElements;
                    }

                    m_TargetAsset = value;
                    if (m_TargetAsset != null)
                    {
                        m_TargetAsset.MetricsModified += ReloadElements;
                    }
                }
            }
        }

        public MetricsTrack(Timeline owner) : base(owner)
        {
            name = "Metrics";
            AddToClassList("metricsTrack");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public override void OnAssetModified()
        {
            ReloadElements();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += ReloadElements;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= ReloadElements;
        }

        public override void SetClip(TaggedAnimationClip clip)
        {
            base.SetClip(clip);
            ReloadElements();
        }

        class MetricRange : IInterval
        {
            public string name;
            public float intervalStart { get; set; }
            public float intervalEnd { get; set; }
            public int priority;
        }

        struct TagInterval
        {
            public int firstFrame;
            public int lastFrame;
        }

        struct Segment
        {
            public Segment(TaggedAnimationClip clip, Asset asset, float startTime, float endTime)
            {
                this.clip = clip;
                this.asset = asset;
                this.startTime = startTime;
                this.endTime = endTime;

                tags = new List<TagInterval>();
                tags.Add(new TagInterval(){ firstFrame = FirstFrame, lastFrame = LastFrame });
            }

            public TaggedAnimationClip clip;
            public Asset asset;

            public List<TagInterval> tags;

            public float startTime;
            public float endTime;

            public float Duration => endTime - startTime;

            public int FirstFrame => GetFrameIndexFromClipTime(startTime);
            public int LastFrame => GetFrameIndexFromClipTime(endTime);

            public bool IsValid => clip != null && asset != null;

            public static Segment Invalid => new Segment()
            {
                clip = null,
                asset = null,
                startTime = 0.0f,
                endTime = 0.0f
            };

            public int GetFrameIndexFromClipTime(float time)
            {
                return clip.ClipFramesToAssetFrames(asset, clip.ClampedTimeInSecondsToIndex(time));
            }

            public float GetClipTimeFromFrameIndex(int frameIndex)
            {
                return clip.IndexToTimeInSeconds(clip.AssetFramesToClipFrames(asset, frameIndex));
            }

            public bool DoesCoverTime(float time)
            {
                int frameIndex = GetFrameIndexFromClipTime(time);
                return frameIndex >= FirstFrame && frameIndex <= LastFrame;
            }

            public bool DoesCoverInterval(IInterval interval)
            {
                return DoesCoverTime(interval.intervalStart) && DoesCoverTime(interval.intervalEnd);
            }
        }

        public override void ReloadElements()
        {
            Profiler.BeginSample("MetricsTrack.ReloadElements");
            Clear();

            TargetAsset = m_Owner.TargetAsset;
            if (TargetAsset == null)
            {
                Profiler.EndSample();
                return;
            }

            if (m_TaggedClip == null)
            {
                Profiler.EndSample();
                return;
            }

            IntervalTree<MetricRange> intervalTree = BuildIntervalTree(m_TaggedClip);
            List<MetricRange> results = new List<MetricRange>();
            intervalTree.IntersectsWithRange(0f, m_TaggedClip.DurationInSeconds, results);
            ProcessIntervalTreeResults(results);

            List<Segment> segments = ComputeSegments(m_TaggedClip, TargetAsset);

            int numBoundaryFrames = Missing.truncToInt(TargetAsset.TimeHorizon * TargetAsset.SampleRate);
            float boundaryDuration = numBoundaryFrames / TargetAsset.SampleRate;

            float prevSegmentDuration = 0.0f;
            if (m_TaggedClip.TaggedPreBoundaryClip != null)
            {
                List<Segment> prevSegments = ComputeSegments(m_TaggedClip.TaggedPreBoundaryClip, m_TargetAsset);
                if (prevSegments.Count > 0)
                {
                    prevSegmentDuration = prevSegments[prevSegments.Count - 1].Duration;
                }
            }

            float nextSegmentDuration = 0.0f;
            if (m_TaggedClip.TaggedPostBoundaryClip != null)
            {
                List<Segment> nextSegments = ComputeSegments(m_TaggedClip.TaggedPostBoundaryClip, m_TargetAsset);
                if (nextSegments.Count > 0)
                {
                    nextSegmentDuration = nextSegments[0].Duration;
                }
            }


            foreach (var metricRange in results)
            {
                Segment segment = Segment.Invalid;
                foreach (Segment s in segments)
                {
                    if (s.DoesCoverInterval(metricRange))
                    {
                        segment = s;
                        break;
                    }
                }

                Assert.IsTrue(segment.IsValid);
                if (!segment.IsValid)
                {
                    continue;
                }

                segment.startTime += Mathf.Max(boundaryDuration - prevSegmentDuration, 0.0f);
                segment.endTime -= Mathf.Max(boundaryDuration - nextSegmentDuration, 0.0f);

                bool emptySegment = segment.FirstFrame >= segment.LastFrame;

                var metricElement = new MetricElement(
                    metricRange.name,
                    Mathf.Clamp(metricRange.intervalStart, 0.0f, m_TaggedClip.DurationInSeconds),
                    Mathf.Clamp(metricRange.intervalEnd, 0.0f, m_TaggedClip.DurationInSeconds),
                    segment.startTime,
                    segment.endTime,
                    emptySegment,
                    this);

                AddElement(metricElement);

                if (!emptySegment)
                {
                    // segment itself is long enough, but some included tags might be too short nonetheless.
                    // we must warn the user in this case
                    foreach (TagInterval tag in segment.tags)
                    {
                        int firstValidFrame = Mathf.Max(tag.firstFrame, segment.FirstFrame);
                        int lastValidFrame = Mathf.Min(tag.lastFrame, segment.LastFrame);

                        if (firstValidFrame >= lastValidFrame)
                        {
                            float startTime = segment.GetClipTimeFromFrameIndex(tag.firstFrame);
                            float endTime = segment.GetClipTimeFromFrameIndex(tag.lastFrame);

                            var tagTooShortElement = new MetricElement(
                                "tag",
                                startTime,
                                endTime,
                                startTime,
                                endTime,
                                true,
                                this);

                            AddElement(tagTooShortElement);
                        }
                    }
                }
            }

            ResizeContents();

            Profiler.EndSample();
        }

        /// <summary>
        /// Takes a result list from an IntervalTree and merges adjacent intervals with the same name and adjust for priority levels
        /// </summary>
        /// <param name="source"></param>
        void ProcessIntervalTreeResults(List<MetricRange> source)
        {
            /*
             * A second level of processing is done to merge intervals that share the same name and return intervals with higher priority
             */
            source.Sort((left, right) => Comparer<int>.Default.Compare(left.priority, right.priority));
            // start from 2nd to last result hiding and splitting lower priority results and proceeding backwards in the list
            for (int i = source.Count - 2; i >= 0; --i)
            {
                MetricRange higherPriority = source[i];

                for (int j = i + 1; j < source.Count;)
                {
                    MetricRange lowerPriority = source[j];
                    if (lowerPriority.intervalStart >= higherPriority.intervalStart &&
                        lowerPriority.intervalEnd <= higherPriority.intervalEnd)
                    {
                        source.RemoveAt(j);
                        continue;
                    }

                    bool insertNew = false;

                    if (lowerPriority.intervalStart <= higherPriority.intervalStart)
                    {
                        if (lowerPriority.intervalEnd <= higherPriority.intervalStart)
                        {
                            ++j;
                            continue;
                        }

                        // merge with higher priority
                        if (lowerPriority.name.Equals(higherPriority.name))
                        {
                            higherPriority = new MetricRange
                            {
                                intervalStart = lowerPriority.intervalStart,
                                intervalEnd = Mathf.Max(lowerPriority.intervalEnd, higherPriority.intervalEnd),
                                priority = higherPriority.priority,
                                name = lowerPriority.name
                            };

                            source[i] = higherPriority;
                            source.RemoveAt(j);
                            continue;
                        }

                        if (lowerPriority.intervalEnd > higherPriority.intervalEnd)
                        {
                            // we need to split the lowerPriority interval
                            insertNew = true;
                        }

                        // shrink
                        source[j] = new MetricRange
                        { intervalStart = lowerPriority.intervalStart, intervalEnd = higherPriority.intervalStart, priority = lowerPriority.priority, name = lowerPriority.name };
                    }

                    if (lowerPriority.intervalEnd >= higherPriority.intervalEnd)
                    {
                        if (lowerPriority.intervalStart < higherPriority.intervalEnd)
                        {
                            // merge with higher priority
                            if (lowerPriority.name.Equals(higherPriority.name))
                            {
                                higherPriority = new MetricRange
                                {
                                    intervalStart = higherPriority.intervalStart,
                                    intervalEnd = lowerPriority.intervalEnd,
                                    priority = higherPriority.priority,
                                    name = lowerPriority.name
                                };

                                source[i] = higherPriority;
                                source.RemoveAt(j);
                                continue;
                            }

                            var newResult = new MetricRange
                            {
                                intervalStart = higherPriority.intervalEnd, intervalEnd = lowerPriority.intervalEnd, priority = lowerPriority.priority, name = lowerPriority.name
                            };

                            // if this lower priority interval surrounded the higher priority one we will need to add a second lower priority interval, otherwise we can just replace the original
                            if (insertNew)
                            {
                                source.Insert(j + 1, newResult);
                            }
                            else
                            {
                                source[j] = newResult;
                            }
                        }
                    }

                    ++j;
                }
            }
        }

        IntervalTree<MetricRange> BuildIntervalTree(TaggedAnimationClip clip)
        {
            List<TagAnnotation> tags = clip.Tags;
            Profiler.BeginSample("MetricsTrack.BuildIntervalTree");
            IntervalTree<MetricRange> intervalTree = new IntervalTree<MetricRange>();

            for (var i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                int metricIndex = clip.GetMetricIndexCoveringTimeOnTag(tag);

                if (metricIndex < 0 || metricIndex >= TargetAsset.Metrics.Count)
                {
                    continue;
                }

                string metricName = TargetAsset.GetMetric(metricIndex).name;
                intervalTree.Add(new MetricRange { intervalStart = tag.startTime, intervalEnd = tag.EndTime, name = metricName, priority = i });
            }

            Profiler.EndSample();
            return intervalTree;
        }

        public override void ResizeContents()
        {
            foreach (MetricElement me in Children().OfType<MetricElement>())
            {
                me.Resize();
            }
        }

        List<Segment>   ComputeSegments(TaggedAnimationClip clip, Asset asset)
        {
            List<Segment> segments = new List<Segment>();

            foreach (TagAnnotation tag in clip.Tags)
            {
                if (clip.GetMetricIndexCoveringTimeOnTag(tag) < 0)
                {
                    continue;
                }

                segments.Add(new Segment
                    (
                        clip,
                        asset,
                        Mathf.Max(0.0f, tag.startTime),
                        Mathf.Min(clip.DurationInSeconds, tag.EndTime)
                    )
                );
            }

            segments.Sort((x, y) => x.startTime.CompareTo(y.startTime));


            // merge overlapping segments
            for (int i = 0; i < segments.Count - 1;)
            {
                if (segments[i + 1].startTime > segments[i].endTime)
                {
                    ++i;
                    continue;
                }

                segments[i] = new Segment
                {
                    clip = clip,
                    asset = asset,
                    startTime = segments[i].startTime,
                    endTime = Mathf.Max(segments[i].endTime, segments[i + 1].endTime),
                    tags = segments[i].tags
                };
                segments[i].tags.AddRange(segments[i + 1].tags);

                segments.RemoveAt(i + 1);
            }

            return segments;
        }
    }
}
