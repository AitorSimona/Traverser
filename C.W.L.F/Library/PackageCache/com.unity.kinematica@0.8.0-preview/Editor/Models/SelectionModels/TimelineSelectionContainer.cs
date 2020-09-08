using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    class TimelineSelectionContainer : ScriptableObject
    {
        public TaggedAnimationClip Clip
        {
            get => m_Clip;
            set
            {
                if (m_Clip != null)
                {
                    m_Clip.DataChanged -= OnClipDataChanged;
                }

                m_Clip = value;
                if (m_Clip != null)
                {
                    m_Clip.DataChanged += OnClipDataChanged;
                }
            }
        }

        public int Count => Tags.Count + Markers.Count();

        public readonly List<TagAnnotation> m_Tags = new List<TagAnnotation>();

        public List<TagAnnotation> Tags
        {
            get { return m_Tags; }
        }

        public readonly List<MarkerAnnotation> m_Markers = new List<MarkerAnnotation>();

        public List<MarkerAnnotation> Markers
        {
            get { return m_Markers; }
        }

        readonly List<ITimelineElement> m_Elements = new List<ITimelineElement>();
        TaggedAnimationClip m_Clip;
        internal bool m_FullClipSelection;

        public event Action AnnotationsChanged;

        public void Select(TaggedAnimationClip clip)
        {
            Clip = clip;
            ReloadFromClip();
            m_FullClipSelection = true;
            AnnotationsChanged?.Invoke();
        }

        public void ReloadFromClip()
        {
            m_Markers.Clear();
            m_Tags.Clear();

            foreach (var tag in Clip.Tags)
            {
                m_Tags.Add(tag);
            }

            foreach (var marker in Clip.Markers)
            {
                m_Markers.Add(marker);
            }
        }

        public void Select(ITimelineElement selectedElement, TaggedAnimationClip clip)
        {
            if (clip != Clip)
            {
                Clear();
                Clip = clip;
            }

            var selection = selectedElement.Object;
            if (selection is TagAnnotation tag)
            {
                Select(tag);
            }
            else if (selection is MarkerAnnotation marker)
            {
                Select(marker);
            }

            m_Elements.Add(selectedElement);
        }

        void Select(TagAnnotation selection)
        {
            if (!m_Tags.Contains(selection))
            {
                m_Tags.Add(selection);
                AnnotationsChanged?.Invoke();
            }
        }

        void Select(MarkerAnnotation selection)
        {
            if (!m_Markers.Contains(selection))
            {
                m_Markers.Add(selection);
                AnnotationsChanged?.Invoke();
            }
        }

        public void Remove(TagAnnotation selection)
        {
            if (m_Tags.Contains(selection))
            {
                m_Tags.Remove(selection);
                AnnotationsChanged?.Invoke();
            }
        }

        public void Remove(MarkerAnnotation selection)
        {
            if (m_Markers.Contains(selection))
            {
                m_Markers.Remove(selection);
                AnnotationsChanged?.Invoke();
            }
        }

        public void DeleteSelectionFromClip()
        {
            m_Elements.ForEach(e => e.Unselect());
            m_Elements.Clear();

            m_Clip.RemoveMarkers(m_Markers);
            m_Clip.RemoveTags(m_Tags);
            m_Markers.Clear();
            m_Tags.Clear();

            m_FullClipSelection = true;
            m_Clip.NotifyChanged();
        }

        public void Clear()
        {
            m_Markers.Clear();
            m_Tags.Clear();
            m_Elements.ForEach(e => e.Unselect());
            m_Elements.Clear();
            Clip = null;
            m_FullClipSelection = false;
            AnnotationsChanged?.Invoke();
        }

        void OnClipDataChanged()
        {
            SyncWithTargetClip();
        }

        internal void SyncWithTargetClip()
        {
            if (Clip == null)
            {
                return;
            }

            if (m_FullClipSelection)
            {
                if (m_Markers.Count != Clip.Markers.Count ||
                    m_Tags.Count != Clip.Tags.Count)
                {
                    ReloadFromClip();
                    AnnotationsChanged?.Invoke();
                    return;
                }
            }

            bool annotationsChanged = false;

            List<MarkerAnnotation> markersToRemove = new List<MarkerAnnotation>();
            List<TagAnnotation> tagsToRemove = new List<TagAnnotation>();
            foreach (var marker in m_Markers)
            {
                if (!Clip.Markers.Contains(marker))
                {
                    markersToRemove.Add(marker);
                    annotationsChanged = true;
                }
            }

            markersToRemove.ForEach(m => m_Markers.Remove(m));

            foreach (var tag in m_Tags)
            {
                if (!Clip.Tags.Contains(tag))
                {
                    tagsToRemove.Add(tag);
                    annotationsChanged = true;
                }
            }

            tagsToRemove.ForEach(m => m_Tags.Remove(m));

            if (annotationsChanged)
            {
                if (m_Markers.Count == 0 && m_Tags.Count == 0)
                {
                    Select(Clip);
                }
                else
                {
                    AnnotationsChanged?.Invoke();
                }
            }
        }
    }
}
