using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MarkerElement : SnappingElement, INotifyValueChanged<float>
    {
        const string k_MarkerStyleClass = "marker";

        public readonly MarkerAnnotation marker;

        MarkerTrack m_Track;

        VisualElement m_TimelineGuideline;
        Label m_ManipulateLabel;

        VisualElement m_Content;
        readonly Color m_BackgroundColor;
        const float k_BackgroundAlpha = .7f;

        public MarkerElement(MarkerAnnotation marker, MarkerTrack track) : base(track)
        {
            m_Track = track;
            this.marker = marker;
            focusable = true;

            m_Content = new VisualElement();
            m_Content.AddToClassList(k_MarkerStyleClass);
            m_BackgroundColor = AnnotationAttribute.GetColor(marker.payload.Type);
            var background = m_BackgroundColor;
            background.a = k_BackgroundAlpha;
            m_Content.style.backgroundColor = background;
            Add(m_Content);

            SetupManipulators(m_Content);

            m_ManipulateLabel = new Label();
            m_ManipulateLabel.AddToClassList("markerManipulateLabel");
            m_ManipulateLabel.style.visibility = Visibility.Hidden;
            Add(m_ManipulateLabel);

            style.position = Position.Absolute;
            style.minHeight = 22;
            style.maxWidth = 1;

            marker.Changed += Reposition;

            if (panel != null)
            {
                SetupTimelineGuideline(panel.visualTree);
            }
            else
            {
                RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            }

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public float XPos
        {
            get
            {
                float l = style.left.value.value;
                if (float.IsNaN(l))
                {
                    return 0f;
                }

                return l;
            }
        }

        void SetupManipulators(VisualElement element)
        {
            element.AddManipulator(new MarkerManipulator(this));
            var contextManipulator = new ContextualMenuManipulator((obj) =>
            {
                SelectMarkerMenu(obj);
                obj.menu.AppendSeparator();
            });

            element.AddManipulator(contextManipulator);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (m_TimelineGuideline == null)
            {
                SetupTimelineGuideline(evt.destinationPanel.visualTree);
            }
        }

        void SetupTimelineGuideline()
        {
            if (panel != null)
            {
                SetupTimelineGuideline(panel.visualTree);
            }
        }

        void SetupTimelineGuideline(VisualElement rootElement)
        {
            VisualElement timelineArea = rootElement.Q<VisualElement>(Timeline.k_TimelineWorkAreaName);
            VisualElement scrollableArea = timelineArea.Q<VisualElement>(Timeline.k_ScrollableTimeAreaName);
            m_TimelineGuideline = new VisualElement();
            m_TimelineGuideline.AddToClassList(k_MarkerStyleClass + "Guideline");
            m_TimelineGuideline.focusable = true;
            var background = m_BackgroundColor;
            background.a = k_BackgroundAlpha;
            m_TimelineGuideline.style.backgroundColor = background;
            m_TimelineGuideline.style.width = 1;
            m_TimelineGuideline.style.visibility = Visibility.Hidden;
            SetupManipulators(m_TimelineGuideline);
            scrollableArea.Add(m_TimelineGuideline);
        }

        public override void ShowManipulationLabel()
        {
            m_TimelineGuideline.style.visibility = Visibility.Visible;

            m_ManipulateLabel.style.visibility = Visibility.Visible;
            m_ManipulateLabel.text = TimelineUtility.GetTimeString(m_Track.m_Owner.ViewMode, marker.timeInSeconds, (int)m_Track.Clip.SampleRate);
        }

        public override void HideManipulationLabel()
        {
            m_TimelineGuideline.style.visibility = Visibility.Hidden;
            m_ManipulateLabel.style.visibility = Visibility.Hidden;
        }

        void SelectMarkerMenu(ContextualMenuPopulateEvent evt)
        {
            List<MarkerElement> markersAtTime = m_Track.GetMarkersNearX(XPos).ToList();
            foreach (var m in markersAtTime)
            {
                string text = markersAtTime.Count > 1 ? $"Select/{MarkerAttribute.GetDescription(m.marker.payload.Type)}" : $"Select {MarkerAttribute.GetDescription(m.marker.payload.Type)}";
                Action<DropdownMenuAction> action = a => SelectMarkerElement(m, false);
                if (m == this && markersAtTime.Count > 1)
                {
                    text += "*";
                }

                if (m.m_Selected)
                {
                    evt.menu.AppendAction(text, action, DropdownMenuAction.AlwaysDisabled);
                }
                else
                {
                    evt.menu.AppendAction(text, action, DropdownMenuAction.AlwaysEnabled);
                }
            }
        }

        public override void Resize()
        {
            Reposition();
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            marker.Changed -= Reposition;
            if (m_TimelineGuideline != null)
            {
                m_TimelineGuideline.RemoveFromHierarchy();
                m_TimelineGuideline = null;
            }
        }

        internal event Action<MarkerElement, float, float, float, float> MarkerElementDragged;

        float m_PreviousTime;

        public override void Reposition()
        {
            Reposition(true);
        }

        public void Reposition(bool notify)
        {
            TaggedAnimationClip clip = m_Track.Clip;
            if (clip == null)
            {
                return;
            }

            float leftBefore = style.left.value.value;
            float timeLeftValue = Timeline.TimeToLocalPos(value, m_Track);
            style.left = timeLeftValue;
            if (m_TimelineGuideline == null)
            {
                SetupTimelineGuideline();
            }

            m_TimelineGuideline.style.left = timeLeftValue;

            if (notify && !FloatComparer.s_ComparerWithDefaultTolerance.Equals(leftBefore, timeLeftValue))
            {
                MarkerElementDragged?.Invoke(this, m_PreviousTime, value, leftBefore, timeLeftValue);
                m_PreviousTime = value;
            }
        }

        public void SetValueWithoutNotify(float value)
        {
            Undo.RecordObject(m_Track.Clip.Asset, "Drag Marker time");
            marker.timeInSeconds = value;
            Reposition();
        }

        public float value
        {
            get { return marker.timeInSeconds; }
            set
            {
                if (!EqualityComparer<float>.Default.Equals(marker.timeInSeconds, value))
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<float> evt = ChangeEvent<float>.GetPooled(marker.timeInSeconds, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }

                    marker.NotifyChanged();
                    m_Track.m_Owner.TargetAsset.MarkDirty();
                }
            }
        }

        public override void Unselect()
        {
            Color backgroundColor = m_BackgroundColor;
            backgroundColor.a = k_BackgroundAlpha;
            m_Content.style.backgroundColor = backgroundColor;
            if (m_TimelineGuideline != null)
            {
                m_TimelineGuideline.style.backgroundColor = backgroundColor;
            }

            m_Selected = false;
            m_Content.MarkDirtyRepaint();
        }

        public override System.Object Object
        {
            get { return marker; }
        }
        public override float StartTime
        {
            get { return marker.timeInSeconds; }
        }

        public override float EndTime
        {
            get { return marker.timeInSeconds; }
        }

        public override float GetSnapPosition(float targetPosition)
        {
            return worldBound.x;
        }

        internal bool m_Selected = false;

        public event Action Selected;

        internal static void SelectMarkerElement(MarkerElement markerElement, bool multi)
        {
            // cycle selection
            // if already selected and no ctrl key then we should find other marker elements underneath, bring them to the top..
            if (markerElement.m_Selected && !multi)
            {
                List<MarkerElement> markers = markerElement.m_Track.GetMarkersNearX(markerElement.XPos).ToList();
                foreach (MarkerElement marker in markers)
                {
                    if (marker != markerElement)
                    {
                        SelectMarkerElement(marker, false);
                        return;
                    }
                }
            }

            markerElement.BringToFront();
            markerElement.m_Track.m_Owner.Select(markerElement, multi);
            markerElement.m_Content.style.backgroundColor = markerElement.m_BackgroundColor;
            markerElement.m_TimelineGuideline.style.backgroundColor = markerElement.m_BackgroundColor;
            markerElement.style.display = DisplayStyle.Flex;

            markerElement.m_Selected = true;
            markerElement.m_Content.MarkDirtyRepaint();
            markerElement.Selected?.Invoke();
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent keyDownEvt)
            {
                if (keyDownEvt.keyCode == KeyCode.Delete)
                {
                    m_Track.m_Owner.DeleteSelection();
                }
            }
        }

        void OnTwinKeyDownEvent(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Delete)
            {
                m_Track.m_Owner.DeleteSelection();
            }
        }
    }
}
