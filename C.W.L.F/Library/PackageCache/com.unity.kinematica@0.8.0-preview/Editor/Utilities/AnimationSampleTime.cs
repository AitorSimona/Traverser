using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal struct AnimationSampleTime
    {
        public static AnimationSampleTime CreateInvalid()
        {
            return new AnimationSampleTime()
            {
                clip = null,
                sampleTimeInSeconds = 0.0f
            };
        }

        public static AnimationSampleTime CreateFromTimeIndex(Asset asset, ref Binary binary, TimeIndex timeIndex)
        {
            AnimationSampleTimeIndex animSampleTime = binary.GetAnimationSampleTimeIndex(timeIndex);
            if (animSampleTime.IsValid)
            {
                foreach (TaggedAnimationClip clip in asset.AnimationLibrary)
                {
                    if (!clip.Valid)
                    {
                        continue;
                    }

                    if (clip.AnimationClipGuid == animSampleTime.clipGuid)
                    {
                        var inverseSampleRate = math.rcp(clip.SampleRate);
                        var sampleTimeInSeconds = animSampleTime.animFrameIndex * inverseSampleRate;

                        return new AnimationSampleTime
                        {
                            clip = clip,
                            sampleTimeInSeconds = sampleTimeInSeconds
                        };
                    }
                }
            }

            return CreateInvalid();
        }

        public TimeIndex GetTimeIndex(ref Binary binary)
        {
            int animFrameIndex = Missing.truncToInt(sampleTimeInSeconds * clip.SampleRate);

            return binary.GetTimeIndexFromAnimSampleTime(new AnimationSampleTimeIndex()
            {
                clipGuid = clip.AnimationClipGuid,
                clipName = clip.ClipName,
                animFrameIndex = animFrameIndex
            });
        }

        public bool IsValid => clip != null && clip.Valid;

        public TaggedAnimationClip clip;
        public float            sampleTimeInSeconds;
    }
}
