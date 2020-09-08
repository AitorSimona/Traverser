using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.Assertions.Comparers;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MarkerTrack : GutterTrack
    {
        static float k_MarkerPositionEpsilon = 5f;
        static FloatComparer k_MarkerTimelinePositionComparer = new FloatComparer(k_MarkerPositionEpsilon);

        List<MarkerOverlapIndicator> m_MarkerOverlapIndicators;

        public MarkerTrack(Timeline owner) : base(owner)
        {
            name = "Markers";
            AddToClassList("markerTrack");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_MarkerOverlapIndicators = new List<MarkerOverlapIndicator>();
            var manipulator = new ContextualMenuManipulator(evt =>
            {
                DropdownMenu menu = evt.menu;
                float time = m_Owner.WorldPositionToTime(evt.mousePosition.x);
                if (m_Owner.ViewMode == TimelineViewMode.frames)
                {
                    time = (float)TimelineUtility.RoundToFrame(time, Clip.SampleRate);
                }

                string timeStr = TimelineUtility.GetTimeString(m_Owner.ViewMode, time, (int)Clip.SampleRate);

                menu.AppendAction($"Add Marker at {timeStr}", null, DropdownMenuAction.Status.Disabled);
                menu.AppendSeparator();
                var menuStatus = EditorApplication.isPlaying ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
                foreach (Type markerType in MarkerAttribute.GetMarkerTypes())
                {
                    evt.menu.AppendAction(MarkerAttribute.GetDescription(markerType), action => OnAddAnnotationSelection(markerType, time), a => menuStatus, markerType);
                }
            });

            this.AddManipulator(manipulator);
        }

        void OnAddAnnotationSelection(Type type, float time)
        {
            m_TaggedClip.AddMarker(type, time);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += ReloadElements;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= ReloadElements;
        }

        public override void SetClip(TaggedAnimationClip taggedClip)
        {
            if (m_TaggedClip != null)
            {
                m_TaggedClip.MarkerAdded -= OnMarkerAdded;
                m_TaggedClip.MarkerRemoved -= OnMarkerRemoved;
            }

            List<MarkerElement> markers = GetMarkerElements().ToList();
            foreach (MarkerElement markerElement in markers)
            {
                markerElement.MarkerElementDragged -= OnMarkerElementDragged;
                markerElement.Selected -= OnMarkerSelected;
                markerElement.RemoveFromHierarchy();
            }

            base.SetClip(taggedClip);

            if (m_TaggedClip != null)
            {
                m_TaggedClip.MarkerAdded += OnMarkerAdded;
                m_TaggedClip.MarkerRemoved += OnMarkerRemoved;
            }

            ReloadElements();
        }

        public override void ReloadElements()
        {
            Profiler.BeginSample("MarkerTrack.ReloadElements");
            Clear();

            if (m_TaggedClip != null)
            {
                for (var index = 0; index < m_TaggedClip.Markers.Count; index++)
                {
                    var me = CreateMarkerElement(m_TaggedClip.Markers[index]);
                    if (index == 0)
                    {
                        me.RegisterCallback<GeometryChangedEvent>(OnAttachToPanelCreateOverlapIndicators);
                    }
                }
            }

            SetDisplay(style.display.value);

            ResizeContents();

            Profiler.EndSample();
        }

        public override void ResizeContents()
        {
            Profiler.BeginSample("MarkerTrack::ResizeContents");

            foreach (var visualElement in GetMarkerElements())
            {
                visualElement.Reposition(false);
            }

            CreateMarkerOverlapIndicators();
            Profiler.EndSample();
        }

        // We assume that `elements` are already sorted by position left to right
        static float Mid(List<MarkerElement> elements)
        {
            var sameTime = true;
            for (int i = 0, j = 1; j < elements.Count; ++i, ++j)
            {
                if (!FloatComparer.s_ComparerWithDefaultTolerance.Equals(elements[i].XPos, elements[j].XPos))
                {
                    sameTime = false;
                    break;
                }
            }

            if (sameTime)
            {
                return elements.First().XPos;
            }

            List<float> positions = elements.Select(me => me.XPos).ToList();
            if (!positions.Any())
            {
                return 0f;
            }

            float min = positions.First();
            float max = positions.Last();
            if (FloatComparer.s_ComparerWithDefaultTolerance.Equals(min, max))
            {
                return min; //reduces flickering with overlapping elements
            }

            return min + (max - min) / 2f;
        }

        void OnAttachToPanelCreateOverlapIndicators(GeometryChangedEvent evt)
        {
            (evt.target as MarkerElement).UnregisterCallback<GeometryChangedEvent>(OnAttachToPanelCreateOverlapIndicators);
            CreateMarkerOverlapIndicators();
        }

        void CreateMarkerOverlapIndicators()
        {
            Profiler.BeginSample("MarkerTrack::ComputeInitialOverlaps");
            foreach (var indicator in m_MarkerOverlapIndicators)
            {
                indicator.RemoveFromHierarchy();
            }

            m_MarkerOverlapIndicators.Clear();

            if (Clip != null)
            {
                List<MarkerElement> elements = GetMarkerElements().OrderBy(me => me.style.left.value.value).ToList();
                List<Tuple<float, List<MarkerElement>>> positions = new List<Tuple<float, List<MarkerElement>>>();
                if (elements.Any())
                {
                    MarkerElement currentElement = elements[0];
                    if (elements.Count == 1)
                    {
                        positions.Add(new Tuple<float, List<MarkerElement>>(currentElement.XPos, elements));
                    }
                    else
                    {
                        List<MarkerElement> grouped = new List<MarkerElement>();
                        grouped.Add(currentElement);
                        for (var index = 1; index < elements.Count; index++)
                        {
                            MarkerElement marker = elements[index];
                            float pos = marker.XPos;

                            if (pos - currentElement.XPos < k_MarkerPositionEpsilon)
                            {
                                //add nearby position
                                grouped.Add(marker);
                            }
                            else
                            {
                                //close group
                                positions.Add(new Tuple<float, List<MarkerElement>>(Mid(grouped), grouped));
                                currentElement = marker;
                                grouped = new List<MarkerElement>();
                                grouped.Add(currentElement);
                            }
                        }

                        //close final group
                        positions.Add(new Tuple<float, List<MarkerElement>>(Mid(grouped), grouped));
                    }
                }

                foreach ((float overlapPosition, List<MarkerElement> markerElements) in positions)
                {
                    if (markerElements.Count > 1)
                    {
                        var indicator = new MarkerOverlapIndicator(this, overlapPosition);
                        m_MarkerOverlapIndicators.Add(indicator);
                        Add(indicator);
                        indicator.Reposition();
                    }
                }
            }

            Profiler.EndSample();
        }

        void OnMarkerElementDragged(MarkerElement marker, float previousTime, float newTime, float previousX, float newX)
        {
            CreateMarkerOverlapIndicators();
        }

        MarkerElement CreateMarkerElement(MarkerAnnotation marker)
        {
            var me = new MarkerElement(marker, this);

            AddElement(me);

            me.Reposition();

            if (m_Owner.SelectionContainer.m_Markers.Contains(marker) && !m_Owner.SelectionContainer.m_FullClipSelection)
            {
                MarkerElement.SelectMarkerElement(me, m_Owner.SelectionContainer.Count > 1);
            }

            me.MarkerElementDragged += OnMarkerElementDragged;
            me.Selected += OnMarkerSelected;

            return me;
        }

        void OnMarkerAdded(MarkerAnnotation marker)
        {
            var markerElement = GetMarkerElements().FirstOrDefault(me => me.marker == marker);
            if (markerElement != null)
            {
                return;
            }

            CreateMarkerElement(marker);
        }

        void OnMarkerRemoved(MarkerAnnotation marker)
        {
            var markerTimelineElement = GetMarkerElements().FirstOrDefault(me => me.marker == marker);
            if (markerTimelineElement != null)
            {
                markerTimelineElement.RemoveFromHierarchy();
            }

            m_Owner.SelectionContainer.Remove(marker);
        }

        void OnMarkerSelected()
        {
            // When Markers are selected they are brought to the front (to show in case of overlap)
            // However, this puts them in front of overlap indicators
            var overlapIndicators = Children().OfType<MarkerOverlapIndicator>().ToList();
            foreach (var multipleMarker in overlapIndicators)
            {
                multipleMarker.BringToFront();
            }
        }

        internal IEnumerable<MarkerElement> GetMarkerElements()
        {
            return Children().OfType<MarkerElement>();
        }

        internal IEnumerable<MarkerElement> GetMarkersNearX(float x)
        {
            foreach (var e in GetMarkerElements())
            {
                float left = e.XPos;
                if (float.IsNaN(left))
                {
                    continue;
                }

                if (k_MarkerTimelinePositionComparer.Equals(left, x))
                {
                    yield return e;
                }
            }
        }
    }
}
