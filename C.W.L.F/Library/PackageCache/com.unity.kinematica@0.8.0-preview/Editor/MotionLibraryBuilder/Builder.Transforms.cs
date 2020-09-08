using System;

using Unity.Mathematics;

using UnityEngine;
using Unity.Collections;
using static AnimationCurveBake;
using System.Collections;

namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        public IEnumerator BuildTransforms()
        {
            //
            // Now re-sample all animations according to the target
            // sample rate and adjust the segment information to
            // reference the transforms array.
            //

            //
            // Calculate total number of poses to be generated
            // (based on previously generated segments)
            //

            int numJoints = rig.NumJoints;
            if (numJoints < 2)
            {
                throw new ArgumentException($"Rig does not have enough joints. Only {numJoints} present.");
            }

            if (numFrames == 0)
            {
                throw new Exception("No animation frame to process, please make sure there is at least one single non-empty tag in your Kinematica asset.");
            }

            int numTransforms = numFrames * numJoints;

            using (NativeArray<AffineTransform> transforms = new NativeArray<AffineTransform>(numTransforms, Allocator.Persistent))
            {
                int globalSegmentIndex = 0;
                int destinationFrameIndex = 0;
                for (int clipIndex = 0; clipIndex < clipSegments.Count; ++clipIndex)
                {
                    ClipSegments segments = clipSegments[clipIndex];
                    AnimationClip clip = segments.Clip.GetOrLoadClipSync();

                    using (AnimationSampler animSampler = new AnimationSampler(rig, clip))
                    {
                        float sourceSampleRate = clip.frameRate;
                        float targetSampleRate = asset.SampleRate;
                        float sampleRateRatio = sourceSampleRate / targetSampleRate;

                        int numFrameResampledClip = (int)math.ceil(targetSampleRate * segments.Clip.DurationInSeconds);

                        for (int segmentIndex = 0; segmentIndex < segments.NumSegments; ++segmentIndex, ++globalSegmentIndex)
                        {
                            Segment segment = segments[segmentIndex];

                            int firstFrame = Missing.roundToInt(segment.source.FirstFrame / sampleRateRatio);
                            firstFrame = math.min(firstFrame, numFrameResampledClip - 1);

                            SampleRange sampleRange = new SampleRange()
                            {
                                startFrameIndex = firstFrame,
                                numFrames = segment.destination.NumFrames
                            };

                            using (AnimationSampler.RangeSampler rangeSampler = animSampler.PrepareRangeSampler(asset.SampleRate, sampleRange, destinationFrameIndex, transforms))
                            {
                                rangeSampler.Schedule();

                                progressFeedback?.Invoke(new ProgressInfo()
                                {
                                    title = $"Sample clip {clip.name}",
                                    progress = (float)globalSegmentIndex / numSegments
                                });

                                while (!rangeSampler.IsComplete)
                                {
                                    yield return null;
                                    if (bCancel)
                                    {
                                        rangeSampler.Complete();
                                        yield break;
                                    }
                                }

                                rangeSampler.Complete();
                            }

                            destinationFrameIndex += segment.destination.NumFrames;
                        }
                    }
                }

                WriteTransformsToBinary(transforms);
            }
        }

        void WriteTransformsToBinary(NativeArray<AffineTransform> transforms)
        {
            int numTransforms = transforms.Length;

            ref Binary binary = ref Binary;

            allocator.Allocate(numTransforms, ref binary.transforms);

            for (int i = 0; i < numTransforms; ++i)
            {
                binary.transforms[i] = transforms[i];
            }
        }
    }
}
