using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class AnnotationsTrack : Track
    {
        ContextualMenuManipulator m_TracksContextManipulator;

        public AnnotationsTrack(Timeline owner) : base(owner)
        {
            m_TracksContextManipulator = new ContextualMenuManipulator(evt =>
            {
                string timeStr = TimelineUtility.GetTimeString(m_Owner.ViewMode, m_Owner.ActiveTime, (int)Clip.SampleRate);
                evt.menu.AppendAction($"Add Annotation at {timeStr}", null, DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();

                AddTagMenu(evt, action => OnAddAnnotationSelection(action.userData as Type), EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
                AddMarkerMenu(evt, action => OnAddAnnotationSelection(action.userData as Type), EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            });
        }

        public override void OnAssetModified()
        {
            if (m_TaggedClip != null)
            {
                var clipTags = m_TaggedClip.Tags;
                var timelineElements = Children().OfType<ITimelineElement>().ToList();
                if (clipTags.Count != timelineElements.Count)
                {
                    ReloadElements();
                }
                else
                {
                    foreach (var timelineElement in timelineElements)
                    {
                        if (timelineElement is TagElement tagElement)
                        {
                            if (!clipTags.Contains(tagElement.m_Tag))
                            {
                                ReloadElements();
                                return;
                            }
                        }
                    }

                    foreach (var timelineElement in timelineElements)
                    {
                        timelineElement.Reposition();
                    }
                }
            }
        }

        public override void ReloadElements()
        {
            Profiler.BeginSample("AnnotationsTrack.ReloadElements");
            Clear();

            if (m_TaggedClip == null)
            {
                Profiler.EndSample();
                return;
            }

            foreach (var tag in m_TaggedClip.Tags)
            {
                Add(new TagElement(this, m_TaggedClip, tag));
            }

            Profiler.EndSample();
        }

        public override void ResizeContents()
        {
            foreach (var rangeElement in this.Query<TimeRangeElement>().Build().ToList())
            {
                rangeElement.Resize();
            }
        }

        public bool ReorderTagElement(ITimelineElement element, int direction)
        {
            if (direction == 0 || element == null)
            {
                return false;
            }

            TagAnnotation tag = element.Object as TagAnnotation;
            if (tag == null)
            {
                return false;
            }

            int currentIndex = m_TaggedClip.Tags.IndexOf(tag);
            int newIndex = currentIndex + direction;

            if (newIndex < 0 || newIndex > m_TaggedClip.Tags.Count - 1)
            {
                return false;
            }

            Undo.RecordObject(m_TaggedClip.Asset, "Reorder Tag");
            m_TaggedClip.Tags.Remove(tag);
            m_TaggedClip.Tags.Insert(newIndex, tag);
            Insert(newIndex, (element as VisualElement));

            return true;
        }

        public override void SetClip(TaggedAnimationClip taggedClip)
        {
            base.SetClip(taggedClip);

            if (m_TaggedClip != null)
            {
                this.AddManipulator(m_TracksContextManipulator);
            }
            else
            {
                this.RemoveManipulator(m_TracksContextManipulator);
            }

            ReloadElements();
        }

        internal float EndOfTags
        {
            get
            {
                float height = 0f;
                var tags = Children().OfType<TagElement>();
                foreach (var tag in tags)
                {
                    height = Mathf.Max(height, tag.layout.yMax);
                }

                return height;
            }
        }

        void OnAddAnnotationSelection(Type type)
        {
            if (type != null && m_TaggedClip != null)
            {
                if (TagAttribute.IsTagType(type))
                {
                    m_TaggedClip.AddTag(type, m_Owner.ActiveTime);
                }
                else if (MarkerAttribute.IsMarkerType(type))
                {
                    m_TaggedClip.AddMarker(type, m_Owner.ActiveTime);
                }
            }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent keyDownEvt)
            {
                if (keyDownEvt.keyCode == KeyCode.Delete)
                {
                    m_Owner.DeleteSelection();
                }
            }
        }

        static void AddTagMenu(ContextualMenuPopulateEvent evt, Action<DropdownMenuAction> action, DropdownMenuAction.Status menuStatus)
        {
            const string prefix = "Add Tag/";
            foreach (var tagType in TagAttribute.GetVisibleTypesInInspector())
            {
                evt.menu.AppendAction($"{prefix}{TagAttribute.GetDescription(tagType)}", action, a => menuStatus, tagType);
            }
        }

        static void AddMarkerMenu(ContextualMenuPopulateEvent evt, Action<DropdownMenuAction> action, DropdownMenuAction.Status menuStatus)
        {
            const string prefix = "Add Marker /";
            foreach (var markerType in MarkerAttribute.GetMarkerTypes())
            {
                evt.menu.AppendAction($"{prefix}{MarkerAttribute.GetDescription(markerType)}", action, a => menuStatus, markerType);
            }
        }
    }
}
