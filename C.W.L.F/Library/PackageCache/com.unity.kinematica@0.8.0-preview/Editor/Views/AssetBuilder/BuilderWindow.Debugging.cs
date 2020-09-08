using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.SnapshotDebugger;

namespace Unity.Kinematica.Editor
{
    partial class BuilderWindow
    {
        void PlayModeUpdate()
        {
            //
            // It would probably be a more symmetric architecture
            // to push the current time from the synthesizer to the
            // builder window.
            //
            // The two other use cases (highlight current task time
            // and retrieving the override time index) use a pull
            // model.
            //

            if (Application.isPlaying && m_PreviewTarget != null)
            {
                var synthesizerProvider = m_PreviewTarget.GetComponent<IMotionSynthesizerProvider>();

                //Look below in the hierarchy as well to help ease of use.
                if (synthesizerProvider != null)
                {
                    synthesizerProvider = m_PreviewTarget.GetComponentInChildren<IMotionSynthesizerProvider>();
                }

                var synthesizer = MemoryRef<MotionSynthesizer>.Null;
                if (synthesizerProvider != null)
                {
                    synthesizer = synthesizerProvider.Synthesizer;
                }

                //The synthesizer may or may not be valid right away and GetSynthesizer can return null
                if (synthesizer.IsValid)
                {
                    var samplingTime = synthesizer.Ref.Time;

                    HighlightTimeIndex(ref synthesizer.Ref, samplingTime.timeIndex);
                }
                else
                {
                    HighlightAnimationClip(null);
                    HighlightCurrentSamplingTime(null, 0.0f);
                }
            }
        }

        public TimeIndex RetrieveDebugTimeIndex(ref Binary binary)
        {
            if (Debugger.instance.rewind)
            {
                TaggedAnimationClip taggedClip = m_Timeline.TaggedClip;

                if (taggedClip != null)
                {
                    float sampleTimeInSeconds = m_Timeline.DebugTime;

                    if (sampleTimeInSeconds >= 0.0f)
                    {
                        AnimationSampleTime animSampleTime = new AnimationSampleTime()
                        {
                            clip = taggedClip,
                            sampleTimeInSeconds = sampleTimeInSeconds
                        };

                        return animSampleTime.GetTimeIndex(ref binary);
                    }
                }
            }

            return TimeIndex.Invalid;
        }

        public void HighlightTimeIndex(ref MotionSynthesizer synthesizer, TimeIndex timeIndex, bool debug = false)
        {
            AnimationSampleTime animSampleTime = AnimationSampleTime.CreateFromTimeIndex(Asset, ref synthesizer.Binary, timeIndex);

            if (animSampleTime.IsValid)
            {
                HighlightAnimationClip(animSampleTime.clip);
                HighlightCurrentSamplingTime(animSampleTime.clip, animSampleTime.sampleTimeInSeconds, debug);
            }
        }

        void HighlightCurrentSamplingTime(TaggedAnimationClip animationClip, float sampleTimeInSeconds, bool debug = false)
        {
            TaggedAnimationClip taggedClip = m_Timeline.TaggedClip;

            if (animationClip != null && taggedClip != null && taggedClip.AnimationClipGuid == animationClip.AnimationClipGuid)
            {
                if (debug)
                {
                    m_Timeline.SetActiveTickVisible(false);
                    m_Timeline.SetDebugTime(sampleTimeInSeconds);
                }
                else
                {
                    m_Timeline.SetActiveTime(sampleTimeInSeconds);
                    m_Timeline.SetActiveTickVisible(true);
                }
            }
            else
            {
                m_Timeline.SetActiveTickVisible(false);
            }
        }

        void HighlightAnimationClip(TaggedAnimationClip animationClip)
        {
            foreach (VisualElement clipElement in m_AnimationLibraryListView.Children())
            {
                if (!(clipElement.userData is TaggedAnimationClip taggedClip))
                {
                    continue;
                }

                IStyle clipStyle = clipElement.ElementAt(k_ClipHighlight).style;

                if (taggedClip.AnimationClipGuid == animationClip.AnimationClipGuid)
                {
                    clipStyle.visibility = Visibility.Visible;
                    clipStyle.opacity = new StyleFloat(1f);
                }
                else
                {
                    clipStyle.visibility = Visibility.Hidden;
                }
            }
        }
    }
}
