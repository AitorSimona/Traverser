using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.SnapshotDebugger;

namespace Unity.SnapshotDebugger.Editor
{
    internal class DebuggerWindow : EditorWindow
    {
        [MenuItem(("Window/Analysis/Snapshot Debugger"))]
        public static void ShowWindow()
        {
            GetWindow<DebuggerWindow>("Debug recorder");
        }

        TimelineWidget m_DebuggerTimeline;
        Dictionary<Type, ITimelineDebugDrawer> m_CustomDrawers;
        List<IFrameDebuggerSelectionProcessor> m_SelectionProcessors;
        int? m_RequestSelectProviderIdentifier;

        public void OnEnable()
        {
            m_DebuggerTimeline = new TimelineWidget();
            m_CustomDrawers = new Dictionary<Type, ITimelineDebugDrawer>();
            m_SelectionProcessors = new List<IFrameDebuggerSelectionProcessor>();
            m_RequestSelectProviderIdentifier = null;

            InitializeCustomDrawers();
            InitializeSelectionProcessors();

            Camera.onPostRender -= OnRender;
            Camera.onPostRender += OnRender;
        }

        void InitializeCustomDrawers()
        {
            var drawerTypes = TypeCache.GetTypesDerivedFrom<ITimelineDebugDrawer>();
            foreach (var type in drawerTypes)
            {
                var drawer = Activator.CreateInstance(type);
                m_CustomDrawers.Add((drawer as ITimelineDebugDrawer).AggregateType, drawer as ITimelineDebugDrawer);
            }
        }

        void InitializeSelectionProcessors()
        {
            var processorTypes = TypeCache.GetTypesDerivedFrom<IFrameDebuggerSelectionProcessor>();
            foreach (var type in processorTypes)
            {
                var processor = Activator.CreateInstance(type);
                m_SelectionProcessors.Add(processor as IFrameDebuggerSelectionProcessor);
            }
        }

        public void OnDisable()
        {
            m_CustomDrawers.Clear();

            m_RequestSelectProviderIdentifier = null;

            Camera.onPostRender -= OnRender;
        }

        void OnRender(Camera camera)
        {
            if (EditorApplication.isPlaying)
            {
                foreach (IFrameDebuggerSelectionProcessor processor in m_SelectionProcessors)
                {
                    processor.DrawSelection(camera);
                }
            }
        }

        public void Update()
        {
            if (m_RequestSelectProviderIdentifier.HasValue)
            {
                Debugger.frameDebugger.ClearSelection();

                foreach (FrameDebugProviderInfo providerInfo in Debugger.frameDebugger.ProviderInfos)
                {
                    if (providerInfo.uniqueIdentifier == m_RequestSelectProviderIdentifier.Value)
                    {
                        Debugger.frameDebugger.TrySelect(providerInfo.provider);
                        break;
                    }
                }

                m_RequestSelectProviderIdentifier = null;
            }

            foreach (IFrameDebuggerSelectionProcessor processor in m_SelectionProcessors)
            {
                processor.UpdateSelection();
            }
        }

        public void OnGUI()
        {
            var debugger = Debugger.instance;

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();

                if (debugger.IsState(Debugger.State.Inactive))
                {
                    if (GUILayout.Button(new GUIContent("Start Recording"), new GUILayoutOption[] { GUILayout.Width(300.0f) }))
                    {
                        debugger.state = Debugger.State.Record;
                    }
                }
                else if (debugger.IsState(Debugger.State.Record))
                {
                    if (GUILayout.Button(new GUIContent("Stop Recording"), new GUILayoutOption[] { GUILayout.Width(300.0f) }))
                    {
                        debugger.state = Debugger.State.Inactive;
                    }
                    if (GUILayout.Button(new GUIContent("Pause Recording"), new GUILayoutOption[] { GUILayout.Width(300.0f) }))
                    {
                        debugger.state = Debugger.State.Rewind;
                    }
                }
                else if (debugger.IsState(Debugger.State.Rewind))
                {
                    if (GUILayout.Button(new GUIContent("Stop Recording"), new GUILayoutOption[] { GUILayout.Width(300.0f) }))
                    {
                        debugger.state = Debugger.State.Inactive;
                    }
                    if (GUILayout.Button(new GUIContent("Resume Recording"), new GUILayoutOption[] { GUILayout.Width(300.0f) }))
                    {
                        debugger.state = Debugger.State.Record;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginVertical();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            debugger.capacityInSeconds =
                EditorGUILayout.FloatField(new GUIContent("Capacity in seconds", "Maximum time recorder will keep."),
                    debugger.capacityInSeconds, new GUILayoutOption[] { GUILayout.Width(250.0f) });

            GUILayout.Space(80);

            int memorySize = debugger.memorySize;

            EditorGUILayout.LabelField(
                "Memory size", MemorySize.ToString(memorySize));


            m_DebuggerTimeline.SelectedRangeStart = debugger.startTimeInSeconds;
            m_DebuggerTimeline.SelectedRangeEnd = debugger.endTimeInSeconds;

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (!EditorApplication.isPlaying)
            {
                DrawDebuggerTimeline();

                GUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();

                List<FrameDebugProviderInfo> providers = new List<FrameDebugProviderInfo>(Debugger.frameDebugger.ProviderInfos);
                providers.Insert(0, new FrameDebugProviderInfo()
                {
                    displayName = "All",
                    uniqueIdentifier = -1,
                    provider = null
                });

                int selectedProviderIdentifier = Debugger.frameDebugger.NumSelected > 0 ? Debugger.frameDebugger.GetSelected(0).providerInfo.uniqueIdentifier : -1;
                int selectedIndex = providers.FindIndex(provider => provider.uniqueIdentifier == selectedProviderIdentifier);
                int newSelectedIndex = EditorGUILayout.Popup(new GUIContent("Target"), selectedIndex, providers.Select(p => new GUIContent(p.displayName)).ToArray(), new GUILayoutOption[] { GUILayout.Width(400.0f) });
                if (selectedIndex != newSelectedIndex)
                {
                    m_RequestSelectProviderIdentifier = providers[newSelectedIndex].uniqueIdentifier;
                }


                foreach (IFrameDebuggerSelectionProcessor processor in m_SelectionProcessors)
                {
                    processor.DrawInspector();
                }


                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                DrawDebuggerTimeline();

                GUILayout.Space(5);

                foreach (var aggregate in Debugger.registry.aggregates)
                {
                    var gameObject = aggregate.gameObject;

                    var label = string.Empty;

                    foreach (var provider in aggregate.providers)
                    {
                        label += $" {provider.GetType().Name} [{(int)provider.identifier}]";
                    }

                    EditorGUILayout.LabelField(
                        $"{gameObject.name} [{(int)aggregate.identifier}]", label);
                }
            }


            EditorGUILayout.EndVertical();

            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        void DrawDebuggerTimeline()
        {
            IEnumerable<FrameDebugProviderInfo> providers = Debugger.frameDebugger.NumSelected == 0 ? Debugger.frameDebugger.ProviderInfos : Debugger.frameDebugger.Selection.Select(p => p.providerInfo);

            var lastRect = GUILayoutUtility.GetLastRect();
            Rect rt = GUILayoutUtility.GetRect(lastRect.width, 54);

            List<float> timelineHeights = new List<float>();

            foreach (FrameDebugProviderInfo provider in providers)
            {
                timelineHeights.Add(kCharacterNameOffset);
                rt.height += kCharacterNameOffset;

                List<IFrameAggregate> frameAggregates = Debugger.frameDebugger.GetFrameAggregates(provider.uniqueIdentifier);
                foreach (IFrameAggregate frameAggregate in frameAggregates)
                {
                    float timelineHeight = m_CustomDrawers[frameAggregate.GetType()].GetDrawHeight(frameAggregate);
                    timelineHeights.Add(timelineHeight);
                    rt.height += timelineHeight;
                }
            }

            GUI.BeginGroup(rt);
            {
                Rect rect = new Rect(0, 0, rt.width, rt.height);

                if (m_BackgroundColor == Color.white)
                {
                    m_BackgroundColor = EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 0.78f) : new Color(0.66f, 0.66f, 0.66f, 0.78f);
                }

                GUI.color = m_BackgroundColor;
                GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);

                TimelineWidget.DrawInfo drawInfo = TimelineWidget.DrawInfo.Create(rect, m_DebuggerTimeline.RangeStart, m_DebuggerTimeline.RangeEnd, m_DebuggerTimeline.SelectedRangeStart, m_DebuggerTimeline.SelectedRangeEnd);
                drawInfo.layout = new TimelineWidget.DrawRangeInfo()
                {
                    drawRect = rect,
                    startTime = m_DebuggerTimeline.RangeStart,
                    endTime = m_DebuggerTimeline.RangeEnd
                };

                drawInfo.timeline.drawRect.y += kBeginOffset;

                int timelineIndex = 0;

                Color highlightColor = Debugger.instance.rewind ? new Color(0.0f, 0.25f, 0.5f, 0.1f) : new Color(0.8f, 0.0f, 0.0f, 0.2f);

                if (providers.Count() > 0)
                {
                    foreach (FrameDebugProviderInfo provider in providers)
                    {
                        List<IFrameAggregate> frameAggregates = Debugger.frameDebugger.GetFrameAggregates(provider.uniqueIdentifier);

                        // Display name
                        string displayName = provider.displayName;

                        drawInfo.timeline.drawRect.height = kCharacterNameRectangleHeight;

                        Vector2 labelSize = TimelineWidget.GetLabelSize(displayName);
                        Rect displayNameRect = drawInfo.timeline.drawRect;
                        displayNameRect.x = drawInfo.layout.drawRect.x;
                        displayNameRect.width = drawInfo.layout.drawRect.width;
                        TimelineWidget.DrawRectangle(displayNameRect, new Color(0.1f, 0.1f, 0.1f, 1.0f));
                        TimelineWidget.DrawLabel(new Rect(displayNameRect.x + 10.0f, displayNameRect.y, labelSize.x, kCharacterNameRectangleHeight), displayName, Color.white);

                        drawInfo.timeline.drawRect.y += timelineHeights[timelineIndex++];

                        drawInfo.timeline.drawRect.height = 0.0f;

                        int providerTimelineIndex = timelineIndex;
                        foreach (IFrameAggregate frameAggregate in frameAggregates)
                        {
                            drawInfo.timeline.drawRect.height += timelineHeights[providerTimelineIndex++];
                        }

                        Rect highlightRect = new Rect(drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeStart) - 2.0f,
                            drawInfo.timeline.drawRect.y - 2.0f,
                            drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeEnd) - drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeStart) + 4.0f,
                            drawInfo.timeline.drawRect.height + 2.0f);

                        TimelineWidget.DrawRange(highlightRect, highlightColor);

                        foreach (IFrameAggregate frameAggregate in frameAggregates)
                        {
                            float timelineHeight = timelineHeights[timelineIndex++];
                            drawInfo.timeline.drawRect.height = timelineHeight;

                            m_CustomDrawers[frameAggregate.GetType()].Draw(provider, frameAggregate, drawInfo);

                            drawInfo.timeline.drawRect.y += timelineHeight;
                        }


                        if (Debugger.instance.IsState(Debugger.State.Record))
                        {
                            TimelineWidget.DrawRectangleDetour(highlightRect, Color.red, 2.0f);
                        }
                    }
                }
                else
                {
                    Rect highlightRect = new Rect(drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeStart) - 2.0f,
                        drawInfo.layout.drawRect.y,
                        drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeEnd) - drawInfo.GetPixelPosition(m_DebuggerTimeline.SelectedRangeStart) + 4.0f,
                        drawInfo.layout.drawRect.height);

                    TimelineWidget.DrawRange(highlightRect, highlightColor);

                    if (Debugger.instance.IsState(Debugger.State.Record))
                    {
                        TimelineWidget.DrawRectangleDetour(highlightRect, Color.red, 2.0f);
                    }
                }

                TimelineWidget.DrawNotations(drawInfo);

                m_DebuggerTimeline.Update(rect, Debugger.instance.rewind ? -1.0f : Debugger.instance.time);

                var debugger = Debugger.instance;

                if (Application.isPlaying)
                {
                    Color cursorColor = new Color(0.5f, 0.5f, 0.0f, 1.0f);

                    TimelineWidget.DrawLineAtTime(
                        drawInfo, debugger.rewindTime,
                        cursorColor);
                }

                foreach (var drawer in m_CustomDrawers)
                {
                    drawer.Value.OnPostDraw();
                }

                if (debugger.isActive)
                {
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        var e = Event.current;

                        if (e.button == 0 && !e.alt)
                        {
                            if (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp)
                            {
                                e.Use();

                                m_DebuggerTimeline.Repaint = true;
                                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Arrow);

                                float currentTime =
                                    m_DebuggerTimeline.GetCurrentPositionFromMouse(
                                        rect, Event.current.mousePosition);

                                float start = m_DebuggerTimeline.SelectedRangeStart;
                                float end = m_DebuggerTimeline.SelectedRangeEnd;

                                debugger.rewindTime = Mathf.Clamp(currentTime, start, end);

                                debugger.rewind = debugger.rewind || currentTime <= end;
                            }
                        }
                    }
                }

                GUI.EndGroup();
            }

            GUILayout.Space(5 + rt.height);
        }

        Color m_BackgroundColor = Color.white;
        static float kBeginOffset = 10.0f;
        static float kCharacterNameRectangleHeight = 25.0f;
        static float kCharacterNameOffset = 30.0f;
    }
}
