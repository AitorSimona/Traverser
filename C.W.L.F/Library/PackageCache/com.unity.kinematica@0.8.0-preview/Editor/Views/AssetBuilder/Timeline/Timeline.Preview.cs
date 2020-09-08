using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = System.Object;

namespace Unity.Kinematica.Editor
{
    partial class Timeline
    {
        bool m_IsPreviewing;

        bool m_PreviewEnabled;

        public event Action<bool> PreviewEnabledChangeEvent;

        public bool PreviewEnabled
        {
            get { return m_PreviewEnabled; }
            set
            {
                if (m_PreviewEnabled != value)
                {
                    m_PreviewEnabled = value;
                    OnPreviewSettingChanged();
                    PreviewEnabledChangeEvent?.Invoke(PreviewEnabled);
                }
            }
        }

        GameObject m_PreviewTarget;

        Preview m_Preview;

        public event Action PreviewDisposed;

        void PreviewTargetInvalidated()
        {
            if (m_Preview != null)
            {
                m_Preview.PreviewInvalidated -= PreviewTargetInvalidated;
            }

            DisposePreviews();
            PreviewDisposed?.Invoke();
        }

        void DisposePreviews()
        {
            if (m_Preview != null)
            {
                m_Preview.Dispose();
                m_Preview = null;
            }

            m_BoundaryClipTrack?.DisposePreviews();
            m_PlayControls.TogglePlayOff();
        }

        void OnPreviewSettingChanged()
        {
            if (PreviewEnabled && CanPreview())
            {
                UpdatePreview();
            }
            else
            {
                DisposePreviews();
            }
        }

        void OnPreviewTargetSelectorChanged(ChangeEvent<GameObject> evt)
        {
            PreviewTarget = evt.newValue;
        }

        void UpdatePreview()
        {
            if (PreviewEnabled && CanPreview())
            {
                if (m_Preview == null)
                {
                    try
                    {
                        m_Preview = Preview.CreatePreview(TargetAsset, m_PreviewTarget);
                        m_Preview.PreviewInvalidated += PreviewTargetInvalidated;
                        PreviewActiveTime();
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                }
                else
                {
                    PreviewActiveTime();
                }
            }
        }

        internal void PreviewActiveTime()
        {
            if (CanPreview())
            {
                PreviewEnabled = true;
                m_BoundaryClipTrack.ValidateBoundaryPreviews();

                m_Preview?.DisableDisplayTrajectory();

                if (!m_BoundaryClipTrack.Preview(ActiveTime))
                {
                    m_Preview?.PreviewTime(TaggedClip, ActiveTime);
                }
            }
        }

        void ConnectToPlayControls(PlayControls controls)
        {
            m_PlayControls = controls;
            m_PlayControls.TogglePlay += OnPlayStateChanged;
            m_PlayControls.StepFrame += OnStepFrame;
            m_PlayControls.JumpToFirst += OnJumpToFirst;
            m_PlayControls.JumpToLast += OnJumpToLast;
        }

        void OnPlayStateChanged(bool play)
        {
            if (CanPreview())
            {
                LivePreview = play;
            }
            else
            {
                LivePreview = false;
                m_PlayControls.TogglePlayOffWithoutNotify();
            }
        }

        void OnStepFrame(int direction)
        {
            if (TaggedClip == null)
            {
                return;
            }
            if (direction != 0)
            {
                int frame = ActiveFrame + direction;
                if (direction > 0)
                {
                    if (frame < 0)
                    {
                        frame = 0;
                    }
                    else if (frame > TaggedClip.NumFrames)
                    {
                        frame = 0;
                    }
                }
                else
                {
                    if (frame < 0)
                    {
                        frame = TaggedClip.NumFrames;
                    }
                    else if (frame > TaggedClip.NumFrames)
                    {
                        frame = TaggedClip.NumFrames;
                    }
                }

                SetActiveTime(frame / TaggedClip.SampleRate);
            }
        }

        void OnJumpToFirst()
        {
            if (TaggedClip == null)
            {
                return;
            }

            SetActiveTime(0);
        }

        void OnJumpToLast()
        {
            if (TaggedClip == null)
            {
                return;
            }

            SetActiveTime((TaggedClip.NumFrames) / TaggedClip.SampleRate);
        }

        double m_NextPreviewTick = -1f;
        bool m_LivePreview;

        bool LivePreview
        {
            set
            {
                if (m_LivePreview != value)
                {
                    m_LivePreview = value;

                    if (m_LivePreview)
                    {
                        m_NextPreviewTick = -1f;
                        EditorApplication.update += UpdateLivePreview;
                    }
                    else
                    {
                        EditorApplication.update -= UpdateLivePreview;
                    }
                }
            }
        }

        int ActiveFrame
        {
            get
            {
                float time = ActiveTime;
                if (time < 0)
                {
                    return -1;
                }

                return Mathf.RoundToInt(time * TaggedClip.SampleRate);
            }
        }

        void UpdateLivePreview()
        {
            double currentTime = EditorApplication.timeSinceStartup;

            if (currentTime > m_NextPreviewTick)
            {
                int frame = ActiveFrame + 1;
                if (frame >= TaggedClip.NumFrames)
                {
                    frame = 0;
                }

                SetActiveTime(frame / TaggedClip.SampleRate);
                m_NextPreviewTick = currentTime + 1 / TaggedClip.SampleRate;
            }
        }
    }
}
