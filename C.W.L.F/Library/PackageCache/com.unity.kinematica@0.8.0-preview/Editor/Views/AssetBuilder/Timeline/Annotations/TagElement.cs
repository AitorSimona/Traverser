using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Random = System.Random;
using ColorUtility = Unity.SnapshotDebugger.ColorUtility;

namespace Unity.Kinematica.Editor
{
    class TagElement : SnappingElement
    {
        public readonly TagAnnotation m_Tag;

        Label m_ManipulateStartLabel;
        Label m_ManipulateEndLabel;

        readonly Color m_BackgroundColor;
        const float k_BackgroundAlpha = .75f;

        public TagElement(Track track, TaggedAnimationClip clip, TagAnnotation tag) : base(track)
        {
            m_Tag = tag;
            focusable = true;

            AddToClassList("clipTagRoot");

            m_ManipulateStartLabel = new Label();
            m_ManipulateStartLabel.AddToClassList("tagManipulateStartLabel");
            m_ManipulateStartLabel.AddToClassList("tagManipulateLabel");

            m_ManipulateEndLabel = new Label();
            m_ManipulateEndLabel.AddToClassList("tagManipulateEndLabel");
            m_ManipulateEndLabel.AddToClassList("tagManipulateLabel");

            m_BackgroundColor = AnnotationAttribute.GetColor(m_Tag.Type);
            var background = m_BackgroundColor;
            background.r *= k_BackgroundAlpha;
            background.g *= k_BackgroundAlpha;
            background.b *= k_BackgroundAlpha;
            style.backgroundColor = background;
            int hash = new Random(m_Tag.payload.GetHashedData()).Next();
            Color colorFromValueHash = ColorUtility.FromHtmlString("#" + Convert.ToString(hash, 16));
            Color borderColor = background;
            borderColor.r = (borderColor.r + colorFromValueHash.r) / 2;
            borderColor.g = (borderColor.g + colorFromValueHash.g) / 2;
            borderColor.b = (borderColor.b + colorFromValueHash.b) / 2;
            style.borderLeftColor = borderColor;
            style.borderBottomColor = borderColor;
            style.borderRightColor = borderColor;

            VisualElement startHandle = CreateHandle(TagManipulator.Mode.StartTime);
            startHandle.style.left = -4;
            Insert(0, startHandle);
            startHandle.Add(m_ManipulateStartLabel);

            m_LabelContainer.AddManipulator(new TagManipulator(this, TagManipulator.Mode.Body));
            m_Label.text = string.IsNullOrEmpty(m_Tag.name) ? m_Tag.Name : m_Tag.name;

            VisualElement endHandle = CreateHandle(TagManipulator.Mode.Duration);
            endHandle.style.left = 4;
            Add(endHandle);
            endHandle.Add(m_ManipulateEndLabel);

            var contextMenuManipulator = new ContextualMenuManipulator(OpenTagRemoveMenu);
            this.AddManipulator(contextMenuManipulator);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_Tag.Changed += Resize;
            m_Tag.Changed += Timeline.ReloadMetrics;

            Resize();
        }

        public void Select(IMouseEvent evt)
        {
            Select(Utility.CheckMultiSelectModifier(evt));
        }

        void Select(bool multi)
        {
            bool increment = Selection.activeObject != Timeline.SelectionContainer;

            Timeline.Select(this, multi);
            style.backgroundColor = m_BackgroundColor;

            if (increment)
            {
                // This prevents an Undo of a tag value change from also changing the selection
                Undo.IncrementCurrentGroup();
            }
        }

        public override void Unselect()
        {
            Color background = m_BackgroundColor;
            background.r *= k_BackgroundAlpha;
            background.g *= k_BackgroundAlpha;
            background.b *= k_BackgroundAlpha;
            style.backgroundColor = background;
        }

        public override System.Object Object
        {
            get { return m_Tag; }
        }

        public override float StartTime
        {
            get { return m_Tag.startTime; }
        }

        public override float EndTime
        {
            get { return m_Tag.EndTime; }
        }

        void OpenTagRemoveMenu(ContextualMenuPopulateEvent evt)
        {
            var clip = Track.Clip;
            evt.menu.AppendAction($"Remove : {m_Tag.Name}",
                action => { clip.RemoveTag(m_Tag); },
                EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

            evt.StopPropagation();
        }

        protected override void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            base.OnDetachFromPanel(evt);

            m_Tag.Changed -= Resize;
            m_Tag.Changed -= Timeline.ReloadMetrics;
        }

        VisualElement CreateHandle(TagManipulator.Mode m)
        {
            var handle = new VisualElement();
            handle.AddManipulator(new TagManipulator(this, m));
            handle.AddToClassList("clipTagDragHandle");
            return handle;
        }

        internal float PositionToTime(float x)
        {
            return Timeline.WorldPositionToTime(x);
        }

        public override void Resize()
        {
            float left = Timeline.TimeToLocalPos(m_Tag.startTime, Track);
            style.left = left;
            float right = Timeline.TimeToLocalPos(m_Tag.startTime + m_Tag.duration, Track);
            style.width = right - left;

            m_Label.text = string.IsNullOrEmpty(m_Tag.name) ? m_Tag.Name : m_Tag.name;
        }

        internal float MinTagDuration
        {
            get { return 2 * TagManipulator.k_TagHandleMinWidth / Timeline.WidthMultiplier; }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent keyDownEvt)
            {
                if (keyDownEvt.keyCode == KeyCode.Delete && !EditorApplication.isPlaying)
                {
                    Timeline.DeleteSelection();
                }

                if (keyDownEvt.keyCode == KeyCode.F)
                {
                    Timeline.SetTimeRange(m_Tag.startTime, m_Tag.startTime + m_Tag.duration);
                }
            }
        }

        public override float GetSnapPosition(float targetPosition)
        {
            float left = worldBound.x;
            float right = left + layout.width;
            float fromStart = Mathf.Abs(targetPosition - left);
            float fromEnd = Mathf.Abs(targetPosition - right);

            if (fromStart < fromEnd)
            {
                return left;
            }

            return right;
        }

        public override void HideManipulationLabel()
        {
            m_ManipulateStartLabel.style.visibility = Visibility.Hidden;
            m_ManipulateEndLabel.style.visibility = Visibility.Hidden;
            Timeline.HideGuidelines();
        }

        public override void ShowManipulationLabel()
        {
            TaggedAnimationClip clip = Track.Clip;
            float sampleRate = clip.SampleRate;
            string start = TimelineUtility.GetTimeString(Timeline.ViewMode, m_Tag.startTime, (int)sampleRate);
            m_ManipulateStartLabel.text = start;
            string end = TimelineUtility.GetTimeString(Timeline.ViewMode, m_Tag.EndTime, (int)sampleRate);
            float estimatedTextSize = TimelineUtility.EstimateTextSize(m_ManipulateStartLabel);
            float controlWidth = Math.Max(float.IsNaN(estimatedTextSize) ? m_ManipulateStartLabel.layout.width : estimatedTextSize, 8) + 6;
            m_ManipulateStartLabel.style.left = -controlWidth;
            m_ManipulateEndLabel.text = end;

            m_ManipulateStartLabel.style.visibility = Visibility.Visible;
            m_ManipulateEndLabel.style.visibility = Visibility.Visible;
        }
    }
}
