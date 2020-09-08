using System;
using Unity.SnapshotDebugger;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    partial class Timeline
    {
        const string k_Template = "Timeline.uxml";
        public const string k_Stylesheet = "Timeline.uss";
        public const string k_timelineCellStyleKey = "timelineCell";

        public VisualElement m_TimelineScrollableArea;
        public VisualElement m_ScrollViewContainer;

        TaggedAnimationClip m_TaggedClip;

        public TaggedAnimationClip TaggedClip
        {
            get { return m_TaggedClip; }
            set
            {
                if (m_TaggedClip != value)
                {
                    if (m_TaggedClip != null)
                    {
                        UnsubFromClip();

                        if (value != null)
                        {
                            if (m_TaggedClip.Asset != value.Asset && m_SelectionContainer != null)
                            {
                                UnityEngine.Object.DestroyImmediate(m_SelectionContainer);
                                m_SelectionContainer = null;
                            }
                        }
                    }

                    m_TaggedClip = value;
                }
            }
        }

        public float ActiveTime
        {
            get
            {
                if (m_TimeRange != null)
                {
                    return m_TimeRange.ActiveTime;
                }

                return -1;
            }
            set
            {
                if (m_TimeRange != null)
                {
                    m_TimeRange.SetActiveTime(value, m_Mode == TimelineViewMode.frames);
                }
            }
        }

        public float DebugTime
        {
            get
            {
                if (m_TimeRange != null)
                {
                    return m_TimeRange.DebugTime;
                }

                return -1;
            }
            set
            {
                if (m_TimeRange != null)
                {
                    m_TimeRange.SetDebugTime(value, m_Mode == TimelineViewMode.frames);
                }
            }
        }

        public event Action<GutterTrack> GutterTrackAdded;
        void AddGutterTrack(GutterTrack t)
        {
            m_GutterTracks.Add(t);
            m_TrackElements.Add(t);
            GutterTrackAdded?.Invoke(t);
        }

        public void AddTrack(Track t)
        {
            m_Tracks.Add(t);
            m_TrackElements.Add(t);
        }

        public bool CanAddTag()
        {
            return TaggedClip != null && TaggedClip.Valid;
        }

        public void Reset()
        {
            SetClip(null);
        }

        public void SetClip(TaggedAnimationClip taggedClip, bool updateTimeRange = true)
        {
            ClearSelection();

            m_ClipLengthBar.style.display = DisplayStyle.None;
            m_ActiveTimeField.SetEnabled(false);

            TaggedClip = taggedClip;

            foreach (var tt in m_TrackElements)
            {
                tt.SetClip(TaggedClip);
            }

            if (TaggedClip == null || TargetAsset == null)
            {
                SetFPSLabelText();

                m_TimeRuler.SetIMGUIVisible(false);
                m_ClipArea.style.visibility = Visibility.Hidden;
                m_ActiveTick.style.visibility = Visibility.Hidden;

                return;
            }

            m_TimeRuler.SetIMGUIVisible(true);
            m_ActiveTick.style.visibility = Visibility.Visible;
            m_ClipArea.style.visibility = Visibility.Visible;
            TaggedClip.MarkerAdded += ShowMarkerTrack;
            TaggedClip.DataChanged += AdjustTagBackgrounds;

            if (updateTimeRange)
            {
                UpdateTimeRange();
            }

            SelectionContainer.Select(TaggedClip); // display specific information from the clip in the inspector, like the retarget source avatar, even if no tags/markers have been selected yet
            Selection.activeObject = SelectionContainer;

            if (!TaggedClip.Valid)
            {
                SetFPSLabelText();
                return;
            }

            PreviewActiveTime();

            SetFPSLabelText();

            AdjustTicks();

            m_ClipLengthBar.style.display = DisplayStyle.Flex;

            m_ActiveTimeField.SetEnabled(true);

            ResetTimeRuler();
        }

        internal void ReSelectCurrentClip()
        {
            SetClip(m_TaggedClip, false);
        }

        public bool CanPreview()
        {
            if (TargetAsset == null || TargetAsset.DestinationAvatar == null || PreviewTarget == null || TaggedClip == null || !TaggedClip.Valid || EditorApplication.isPlaying)
            {
                return false;
            }

            return true;
        }

        public static event Action<TimelineViewMode> TimelineViewModeChange;
        public TimelineViewMode TimelineUnits
        {
            get { return m_Mode; }
            private set
            {
                if (m_Mode != value)
                {
                    m_Mode = value;
                    ResizeContents();
                    UpdatePlayheadPositions();
                    AdjustTicks();

                    EditorPrefs.SetString(k_TimelineUnitsPreferenceKey, ((int)TimelineUnits).ToString());

                    TimelineViewModeChange?.Invoke(value);
                }
            }
        }

        public void OnAddTagSelection(Type tagType, float startTime, float duration = -1f)
        {
            if (m_Mode == TimelineViewMode.frames)
            {
                startTime = (float)TimelineUtility.RoundToFrame(startTime, TaggedClip.SampleRate);
                if (duration >= 0)
                {
                    duration = (float)TimelineUtility.RoundToFrame(duration, TaggedClip.SampleRate);
                }
            }

            TaggedClip.AddTag(tagType, startTime, duration);
            TargetAsset.MarkDirty();
        }

        public Asset TargetAsset
        {
            get { return m_Target; }
            set
            {
                if (m_Target != value)
                {
                    PreviewEnabled = false;

                    if (m_Target != null)
                    {
                        m_Target.AssetWasDeserialized -= OnAssetDeserialized;
                        m_Target.MarkedDirty -= OnAssetModified;
                    }

                    m_Target = null;
                    Reset();
                    m_Target = value;
                    if (m_Target != null)
                    {
                        m_Target.AssetWasDeserialized += OnAssetDeserialized;
                        m_Target.MarkedDirty += OnAssetModified;
                    }
                }
            }
        }

        public void SetTimelineEditingEnabled(bool enabled)
        {
            if (enabled)
            {
                HideDebugPlayhead();
                ActiveTick.ShowHandle = true;
                m_TrackElements.ForEach(t => t.EnableEdit());
            }
            else
            {
                ActiveTick.ShowHandle = false;
                m_TrackElements.ForEach(t => t.DisableEdit());
            }
        }

        public event Action<GameObject> PreviewTargetChanged;
        public GameObject PreviewTarget
        {
            get { return m_PreviewTarget; }
            internal set
            {
                if (m_PreviewTarget != value)
                {
                    m_PreviewTarget = value;
                    if (m_PreviewTarget != null)
                    {
                        EditorApplication.update += CheckPreviewTarget;
                    }
                    else
                    {
                        EditorApplication.update -= CheckPreviewTarget;
                    }

                    ManipulatorGizmo.Instance.SetPreviewTarget(m_PreviewTarget);

                    if (m_Preview != null)
                    {
                        if (m_PreviewTarget != null)
                        {
                            m_Preview.SetPreviewTarget(m_PreviewTarget);
                        }
                    }
                    if (m_PreviewTarget == null)
                    {
                        DisposePreviews();
                        SetDebugTime(-1);
                    }

                    PreviewTargetChanged?.Invoke(m_PreviewTarget);
                    OnPreviewSettingChanged();
                }

                UpdatePreviewWarningLabel();
            }
        }

        void CheckPreviewTarget()
        {
            if (PreviewTarget == null)
            {
                PreviewTargetInvalidated();
                PreviewTarget = null;
            }
        }

        public TimelineSelectionContainer SelectionContainer
        {
            get
            {
                if (m_SelectionContainer == null)
                {
                    m_SelectionContainer = ScriptableObject.CreateInstance<TimelineSelectionContainer>();
                    m_SelectionContainer.Clip = TaggedClip;
                }

                return m_SelectionContainer;
            }
        }

        public float SecondsBeforeZero
        {
            get
            {
                if (TaggedClip == null)
                {
                    return 0;
                }

                return TicksBeforeZero / TaggedClip.SampleRate;
            }
        }

        public int TicksBeforeZero
        {
            get
            {
                if (TaggedClip != null)
                {
                    return TaggedClip.NumFrames;
                }

                return 10;
            }
        }

        public TimelineViewMode ViewMode
        {
            get { return m_Mode; }
        }

        public float WorldPositionToTime(float x)
        {
            return (x - m_TimelineScrollableArea.worldBound.x) / WidthMultiplier - SecondsBeforeZero;
        }

        public void SendTagModified()
        {
            TargetAsset.MarkDirty();
            ReloadMetrics();
        }

        public void ReloadMetrics()
        {
            m_MetricsTrack.ReloadElements();
        }

        public void SetTimeRange(float startTime, float endTime)
        {
            m_TimeRange.SetTimeRange(new Vector2(startTime - 1, endTime + 1));
        }

        public void SetActiveTime(float time, bool propagateToLabel = true)
        {
            SetActiveTickVisible(true);

            ActiveTime = time;

            UpdatePlayheadPositions(propagateToLabel);
            PreviewActiveTime();
        }

        public void SetDebugTime(float time)
        {
            if (Debugger.instance.rewind && TaggedClip != null && EditorApplication.isPlaying)
            {
                float duration = TaggedClip.DurationInSeconds;

                time = Mathf.Clamp(time, 0f, duration);

                ShowDebugPlayhead();

                DebugTime = time;

                UpdatePlayheadPositions();
            }
            else
            {
                HideDebugPlayhead();
            }
        }

        public void ClearSelection()
        {
            SelectionContainer.Clear();
        }

        public void Select(ITimelineElement selection, bool multi)
        {
            if (!multi)
            {
                ClearSelection();
                SelectionContainer.Clear();
            }

            SelectionContainer.Select(selection, TaggedClip);
            Selection.activeObject = SelectionContainer;
        }

        public void SetActiveTickVisible(bool visible)
        {
            ActiveTick.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public float GetFreeVerticalHeight()
        {
            return m_AnnotationsTrack.EndOfTags + m_AnnotationsTrack.layout.y;
        }
    }
}
