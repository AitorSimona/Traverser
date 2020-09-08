using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Kinematica.Temporary;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace Unity.Kinematica.Editor
{
    partial class Timeline : VisualElement, IDisposable
    {
        public new class UxmlFactory : UxmlFactory<Timeline , UxmlTraits> {}
        public new class UxmlTraits : BindableElement.UxmlTraits {}

        public const string k_TimelineUnitsPreferenceKey = "Unity.Kinematica.Editor.Timeline.ViewMode";
        const float k_TimelineLengthMultiplier = 2f;
        internal const string k_TimelineWorkAreaName = "timelineWorkArea";
        internal const string k_ScrollableTimeAreaName = "scrollableTimeArea";

        TimelineViewMode m_Mode = TimelineViewMode.frames;

        internal VisualElement m_TimelineWorkArea;
        TimeRuler m_TimeRuler;
        FloatField m_ActiveTimeField;
        Playhead m_ActiveTick;

        VisualElement m_StartGuideline;
        VisualElement m_EndGuideline;
        VisualElement m_SnapGuideline;

        VisualElement StartGuideline
        {
            get
            {
                if (m_StartGuideline == null)
                {
                    m_StartGuideline = new VisualElement();
                    m_StartGuideline.AddToClassList("manipulationGuideline");
                    m_TimelineWorkArea.Add(m_StartGuideline);
                }

                return m_StartGuideline;
            }
        }

        VisualElement EndGuideline
        {
            get
            {
                if (m_EndGuideline == null)
                {
                    m_EndGuideline = new VisualElement();
                    m_EndGuideline.AddToClassList("manipulationGuideline");
                    m_TimelineWorkArea.Add(m_EndGuideline);
                }

                return m_EndGuideline;
            }
        }

        VisualElement SnapGuideline
        {
            get
            {
                if (m_SnapGuideline == null)
                {
                    m_SnapGuideline      = new VisualElement();
                    m_SnapGuideline.AddToClassList("snapLine");
                    m_TimelineWorkArea.Add(m_SnapGuideline);
                }

                return m_SnapGuideline;
            }
        }

        Playhead ActiveTick
        {
            get
            {
                if (m_ActiveTick == null)
                {
                    m_ActiveTick = new Playhead(false) { name = "activeTick" };
                    m_ActiveTick.AddManipulator(new PlayheadManipulator(this));
                }

                return m_ActiveTick;
            }
        }

        Playhead m_ActiveDebugTick;

        Playhead ActiveDebugTick
        {
            get
            {
                if (m_ActiveDebugTick == null)
                {
                    m_ActiveDebugTick = new Playhead(true) { name = "activeDebugTick" };
                    m_ActiveDebugTick.AddToClassList("activeDebugTickElement");
                    m_ActiveDebugTick.Q<VisualElement>("handle").AddToClassList("activeDebugTickHandle");
                    m_ActiveDebugTick.AddManipulator(new PlayheadManipulator(this));
                }

                return m_ActiveDebugTick;
            }
        }

        VisualElement m_ClipLengthBar;
        VisualElement m_ClipArea;
        VisualElement m_GutterTracks;
        VisualElement m_Tracks;
        List<Track> m_TrackElements;
        MetricsTrack m_MetricsTrack;
        AnnotationsTrack m_AnnotationsTrack;
        MarkerTrack m_MarkerTrack;
        BoundaryClipTrack m_BoundaryClipTrack;
        PlayControls m_PlayControls;

        Image m_PreviewWarning;

        TimelineSelectionContainer m_SelectionContainer;

        public void LoadTemplate(VisualElement parentElement)
        {
            UIElements.UIElementsUtils.CloneTemplateInto(k_Template, this);
            UIElements.UIElementsUtils.ApplyStyleSheet(k_Stylesheet, this);
            AddToClassList("flexGrowClass");
            Button button = parentElement.Q<Button>(classes: "viewMode");
            button.clickable = null;
            var manipulator = new ContextualMenuManipulator(ViewToggleMenu);
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            button.AddManipulator(manipulator);

            m_PreviewWarning = parentElement.Q<Image>("warningImage");
            m_PreviewWarning.style.display = DisplayStyle.None;
            m_PreviewWarning.tooltip = "Debugging using the Asset Builder requires setting a Preview Target before entering Play Mode";

            var previewTargetSelector = parentElement.Q<PreviewSelector>();
            previewTargetSelector.RegisterValueChangedCallback(OnPreviewTargetSelectorChanged);
            PreviewDisposed += () => previewTargetSelector.SetValueWithoutNotify(null);
            PreviewTargetChanged += (newTarget) => previewTargetSelector.SetValueWithoutNotify(newTarget);

            ConnectToPlayControls(parentElement.Q<PlayControls>());

            m_TimelineWorkArea = this.Q<VisualElement>(k_TimelineWorkAreaName);

            m_TimelineScrollableArea = this.Q<VisualElement>(k_ScrollableTimeAreaName);
            m_TimelineScrollableArea.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            ZoomManipulator zoomManipulator = new ZoomManipulator(m_TimelineScrollableArea);
            zoomManipulator.ScaleChangeEvent += OnTimelineMouseScroll;
            var panManipulator = new PanManipulator(m_TimelineScrollableArea);
            panManipulator.HorizontalOnly = true;
            panManipulator.Panned += OnTimelinePanned;

            m_ScrollViewContainer = this.Q<VisualElement>("trackScrollViewContainer");
            m_ScrollViewContainer.AddManipulator(panManipulator);
            m_ScrollViewContainer.AddManipulator(zoomManipulator);
            m_ScrollViewContainer.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_ActiveTimeField = parentElement.Q<FloatField>("frameField");
            m_ActiveTimeField.isDelayed = true;
            m_ActiveTimeField.RegisterValueChangedCallback(OnActiveTimeFieldValueChanged);

            UpdateTimeRange();

            m_GutterTracks = this.Q<VisualElement>(classes: "gutter");
            ZoomManipulator gutterZoom = new ZoomManipulator(m_GutterTracks);
            gutterZoom.ScaleChangeEvent += OnTimelineMouseScroll;
            m_GutterTracks.AddManipulator(gutterZoom);
            var gutterPan = new PanManipulator(m_GutterTracks);
            gutterPan.HorizontalOnly = true;
            gutterPan.Panned += OnTimelinePanned;
            m_GutterTracks.AddManipulator(gutterPan);

            m_Tracks = m_TimelineScrollableArea.Q<VisualElement>("tracks");
            m_TrackElements = new List<Track>();
            CreateBuiltInTracks();

            if (!EditorApplication.isPlaying)
            {
                SetActiveTime(0f);
            }

            m_ClipArea = new VisualElement();
            m_ClipArea.AddToClassList("clipArea");

            ScrollView sv = m_ScrollViewContainer.Q<ScrollView>();
            sv.Insert(0, m_ClipArea);
            m_TimelineWorkArea.Add(ActiveTick);
            m_TimelineWorkArea.Add(ActiveDebugTick);

            m_ClipLengthBar = this.Q<VisualElement>("clipLength");

            m_TimeRuler = this.Q<TimeRuler>();
            m_TimeRuler.Init(this, panManipulator, zoomManipulator);
            m_TimeRuler.AddManipulator(new PlayheadManipulator(this));

            string storedViewMode = EditorPrefs.GetString(k_TimelineUnitsPreferenceKey);

            if (storedViewMode == string.Empty)
            {
                TimelineUnits = TimelineViewMode.frames;
            }
            else
            {
                int intVal;
                if (int.TryParse(storedViewMode, out intVal))
                {
                    TimelineUnits = (TimelineViewMode)intVal;
                }
                else
                {
                    TimelineUnits = TimelineViewMode.frames;
                }
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            focusable = true;

            RegisterCallback<KeyDownEvent>(OnKeyDownEvent);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void Dispose()
        {
            UnsubFromClip();
            UnregisterCallback<KeyDownEvent>(OnKeyDownEvent);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            m_TimelineScrollableArea.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            DisposePreviews();

            if (m_SelectionContainer != null)
            {
                if (Selection.activeObject == m_SelectionContainer)
                {
                    Selection.activeObject = TargetAsset;
                }

                Object.DestroyImmediate(m_SelectionContainer);
                m_SelectionContainer = null;
            }

            TargetAsset = null;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.update -= CheckPreviewTarget;
        }

        void CreateBuiltInTracks()
        {
            m_MetricsTrack = new MetricsTrack(this);
            AddGutterTrack(m_MetricsTrack);

            m_AnnotationsTrack = new AnnotationsTrack(this);
            AddTrack(m_AnnotationsTrack);

            m_ScrollViewContainer.AddManipulator(new TagCreationManipulator(this, m_AnnotationsTrack));

            m_BoundaryClipTrack = new BoundaryClipTrack(this);
            AddGutterTrack(m_BoundaryClipTrack);

            m_MarkerTrack = new MarkerTrack(this);
            AddGutterTrack(m_MarkerTrack);
        }

        void OnAddSelection(DropdownMenuAction a)
        {
            OnAddSelection(a.userData as Type);
        }

        void OnAddSelection(Type type)
        {
            if (type != null && TaggedClip != null)
            {
                if (TagAttribute.IsTagType(type))
                {
                    OnAddTagSelection(type, ActiveTime);
                }
                else if (MarkerAttribute.IsMarkerType(type))
                {
                    TaggedClip.AddMarker(type, ActiveTime);
                    TargetAsset.MarkDirty();
                }

                TaggedClip.NotifyChanged();
            }
        }

        void ViewToggleMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Frames", a => { TimelineUnits = TimelineViewMode.frames; },
                a => TimelineUnits == TimelineViewMode.frames ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            evt.menu.AppendAction("Seconds", a => { TimelineUnits = TimelineViewMode.seconds; },
                a => TimelineUnits == TimelineViewMode.seconds ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            evt.menu.AppendAction("Seconds : Frames", a => { TimelineUnits = TimelineViewMode.secondsFrames; },
                a => TimelineUnits == TimelineViewMode.secondsFrames ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        void OnAssetDeserialized(Asset asset)
        {
            EditorApplication.delayCall += OnAssetModified;
        }

        void UnsubFromClip()
        {
            SelectionContainer.Clear();
            if (TaggedClip != null)
            {
                TaggedClip.DataChanged -= AdjustTagBackgrounds;
                TaggedClip.MarkerAdded -= ShowMarkerTrack;
            }
        }

        public event Action<GutterTrack> ForceGutterTrackDisplay;

        void ShowMarkerTrack(MarkerAnnotation ma)
        {
            ForceGutterTrackDisplay?.Invoke(m_MarkerTrack);
        }

        void OnTimelineMouseScroll(float scaleChange, Vector2 focalPoint)
        {
            if (TaggedClip == null)
            {
                return;
            }

            float currentMaximumRange = m_TimeRange.MaximumTime - m_TimeRange.MinimumTime;
            float currentRange = m_TimeRange.EndTime - m_TimeRange.StartTime;
            float currentScale = currentMaximumRange / currentRange;

            float focalPointToWidth = focalPoint.x / m_TimelineScrollableArea.layout.width;
            float focalTime = focalPointToWidth * currentMaximumRange + m_TimeRange.MinimumTime;
            float focalRatio = (focalTime - m_TimeRange.StartTime) / currentRange;

            float newRange = currentRange / scaleChange;
            float newStartTime = m_TimeRange.Limit(focalTime - (newRange * focalRatio));
            float newEndTime = m_TimeRange.Limit(newStartTime + newRange);

            float actualNewRange = newEndTime - newStartTime;

            m_TimeRange.SetTimeRange(new Vector2(newStartTime, newEndTime));
        }

        void OnTimelinePanned(Vector2 from, Vector2 to)
        {
            if (float.IsNaN(m_TimelineScrollableArea.layout.x))
            {
                return;
            }

            if (TaggedClip == null)
            {
                return;
            }

            float timeChange = WorldPositionToTime(from.x) - WorldPositionToTime(to.x);
            if (m_TimeRange.PanTimeRange(timeChange))
            {
                UpdatePlayheadPositions();
                AdjustTicks();
                ResetTimeRuler();
            }
        }

        void ResetWidthToTimelineArea()
        {
            Vector3 newPosition = m_TimelineScrollableArea.transform.position;
            m_TimelineScrollableArea.style.width = m_TimelineWorkArea.layout.width;
            m_GutterTracks.style.width = m_TimelineScrollableArea.style.width;
            newPosition.x = 0f;
            m_TimelineScrollableArea.transform.position = newPosition; // Reset the panning position
            m_GutterTracks.transform.position = new Vector3(newPosition.x, m_GutterTracks.transform.position.y);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_TimelineScrollableArea.layout.width < m_TimelineWorkArea.layout.width)
            {
                ResetWidthToTimelineArea();
            }
            else
            {
                Vector3 newTimelinePosition = m_TimelineScrollableArea.transform.position;
                Vector3 gutterPosition = m_GutterTracks.transform.position;
                if (newTimelinePosition.x + m_TimelineScrollableArea.layout.width < m_TimelineWorkArea.layout.width)
                {
                    newTimelinePosition.x = m_TimelineWorkArea.layout.width - m_TimelineScrollableArea.layout.width;
                    gutterPosition.x = newTimelinePosition.x;
                    m_TimelineScrollableArea.transform.position = newTimelinePosition;
                    m_GutterTracks.transform.position = gutterPosition;
                }

                ResizeContents();
            }

            ResetTimeRuler();

            m_TimelineScrollableArea.style.minHeight = m_ScrollViewContainer.layout.height;
        }

        void OnActiveTimeFieldValueChanged(ChangeEvent<float> evt)
        {
            if (TaggedClip == null)
            {
                return;
            }

            if (m_Mode == TimelineViewMode.frames)
            {
                SetActiveTime(evt.newValue / TaggedClip.SampleRate, false);
            }
            else
            {
                SetActiveTime(evt.newValue, false);
            }
        }

        void ResetTimeRuler()
        {
            float containerStart = m_TimelineWorkArea.worldBound.x;
            m_TimeRuler.m_TimelineWidget.RangeStart = WorldPositionToTime(containerStart);
            m_TimeRuler.m_TimelineWidget.RangeWidth = WorldPositionToTime(containerStart + m_TimelineWorkArea.layout.width) - m_TimeRuler.m_TimelineWidget.RangeStart;
            m_TimeRuler.SampleRate = TaggedClip != null ? (int)TaggedClip.SampleRate : 60;
        }

        Asset m_Target;

        public float TimeToWorldPos(float t)
        {
            int horizontalResolution = Screen.currentResolution.width;
            return (((t + SecondsBeforeZero) * WidthMultiplier + m_TimelineScrollableArea.worldBound.x) * horizontalResolution + 0.5f) /
                horizontalResolution;
        }

        float TimeToLocalPos(float time)
        {
            return TimeToLocalPos(time, m_TimelineWorkArea);
        }

        internal float TimeToLocalPos(float time, VisualElement localElement)
        {
            float zeroWorldPos = TimeToWorldPos(time);
            return localElement.WorldToLocal(new Vector2(zeroWorldPos, 0f)).x;
        }

        internal void DeleteSelection()
        {
            m_SelectionContainer.DeleteSelectionFromClip();
        }

        internal void OnAssetModified()
        {
            foreach (var track in m_TrackElements)
            {
                track.OnAssetModified();
            }
        }

        internal void ShowStartGuideline(float time)
        {
            float left = TimeToLocalPos(time);
            StartGuideline.style.left = left;
            StartGuideline.style.visibility = Visibility.Visible;
        }

        internal void ShowEndGuideline(float time)
        {
            float left = TimeToLocalPos(time);
            EndGuideline.style.left = left;
            EndGuideline.style.visibility = Visibility.Visible;
        }

        internal void ShowGuidelines(float start, float end)
        {
            ShowStartGuideline(start);
            ShowEndGuideline(end);
        }

        internal void HideGuidelines()
        {
            StartGuideline.style.visibility = Visibility.Hidden;
            EndGuideline.style.visibility = Visibility.Hidden;
        }

        internal void ShowSnap(float time)
        {
            float left = TimeToLocalPos(time);
            SnapGuideline.style.left = left;
            SnapGuideline.style.visibility = Visibility.Visible;
            SnapGuideline.BringToFront();
        }

        internal void HideSnap()
        {
            SnapGuideline.style.visibility = Visibility.Hidden;
        }

        public bool ReorderTimelineElements(ITimelineElement element, int direction)
        {
            if (m_AnnotationsTrack.ReorderTagElement(element, direction))
            {
                SendTagModified();
                return true;
            }

            return false;
        }

        void UpdatePreviewWarningLabel()
        {
            m_PreviewWarning.style.display = DisplayStyle.None;
            // Delay checking if we are now in play mode to show warning about preview target
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying)
                {
                    IMotionSynthesizerProvider synthesizerProvider = null;
                    if (PreviewTarget != null)
                    {
                        synthesizerProvider = PreviewTarget.GetComponent<IMotionSynthesizerProvider>();

                        if (synthesizerProvider == null)
                        {
                            synthesizerProvider = PreviewTarget.GetComponentInChildren<IMotionSynthesizerProvider>();
                        }
                    }

                    if (synthesizerProvider == null)
                    {
                        m_PreviewWarning.style.display = DisplayStyle.Flex;
                    }
                }
            };
        }

        void SetFPSLabelText()
        {
            if (TaggedClip != null && TaggedClip.Valid)
            {
                if (ActiveTick.style.display == DisplayStyle.Flex)
                {
                    if (TimelineUnits == TimelineViewMode.frames)
                    {
                        m_ActiveTimeField.SetValueWithoutNotify(ActiveTime * TaggedClip.SampleRate);
                    }
                    else
                    {
                        m_ActiveTimeField.SetValueWithoutNotify(ActiveTime);
                    }
                }
            }
        }

        void OnUndoRedoPerformed()
        {
            if (Selection.activeObject != SelectionContainer)
            {
                ClearSelection();
            }

            if (SelectionContainer != null)
            {
                SelectionContainer.SyncWithTargetClip();
            }

            PreviewActiveTime();
            AdjustTagBackgrounds();
        }

        void OnKeyDownEvent(KeyDownEvent keyDownEvt)
        {
            if (m_TaggedClip != null)
            {
                if (keyDownEvt.keyCode == KeyCode.F)
                {
                    if (m_SelectionContainer.m_FullClipSelection)
                    {
                        SetTimeRange(0f, m_TaggedClip.DurationInSeconds);
                    }
                    else
                    {
                        var tag = m_SelectionContainer.Tags.FirstOrDefault();
                        if (tag != null)
                        {
                            SetTimeRange(tag.startTime, tag.startTime + tag.duration);
                        }
                        else
                        {
                            SetTimeRange(0f, m_TaggedClip.DurationInSeconds);
                        }
                    }

                    keyDownEvt.StopPropagation();
                    keyDownEvt.PreventDefault();
                }
                else if (keyDownEvt.keyCode == KeyCode.A)
                {
                    SetTimeRange(0f, m_TaggedClip.DurationInSeconds);
                    keyDownEvt.StopPropagation();
                    keyDownEvt.PreventDefault();
                }
            }
        }

        void HideDebugPlayhead()
        {
            ActiveDebugTick.style.display = DisplayStyle.None;
        }

        void ShowDebugPlayhead()
        {
            ActiveDebugTick.style.display = DisplayStyle.Flex;
        }

        internal IEnumerable<SnappingElement> TimelineElements()
        {
            List<SnappingElement> query = this.Query<SnappingElement>().Build().ToList();
            foreach (SnappingElement child in query)
            {
                yield return child;
            }
        }
    }
}
