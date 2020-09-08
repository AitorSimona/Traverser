using System;
using System.Collections.Generic;

using UnityEngine.UIElements;
using UnityEngine.Assertions;

using UnityEditor;
using UnityEditor.Experimental.GraphView;

using Unity.SnapshotDebugger;
using Unity.Mathematics;

namespace Unity.Kinematica.Editor
{
    internal class GraphNodeView : GraphView
    {
        internal EditorWindow window;

        internal IFrameDebugProvider FrameDebugProvider => frameDebugProvider;

        List<GraphNodeGroup> graphGroups = new List<GraphNodeGroup>();

        HashSet<GraphNodeGroup> graphGroupsCache;

        IMotionSynthesizerProvider synthesizerProvider;

        IFrameDebugProvider frameDebugProvider;

        DebugMemory? debugMemory;

        public MemoryRef<MotionSynthesizer> Synthesizer => (synthesizerProvider != null && synthesizerProvider.IsSynthesizerInitialized) ? synthesizerProvider.Synthesizer : MemoryRef<MotionSynthesizer>.Null;

        public DebugMemory GetDebugMemory() => debugMemory.Value;

        int updateDelay;

        internal GraphNodeView(EditorWindow window)
        {
            this.window = window;

            InitializeManipulators();

            SetupZoom(0.05f, 2f);

            this.StretchToParentSize();
        }

        internal void Update(IMotionSynthesizerProvider synthesizerProvider)
        {
            if (synthesizerProvider != null)
            {
                this.synthesizerProvider = synthesizerProvider;
                debugMemory = synthesizerProvider.Synthesizer.Ref.ReadDebugMemory;
                frameDebugProvider = synthesizerProvider as IFrameDebugProvider;
            }
            else
            {
                this.synthesizerProvider = null;
                debugMemory = null;
                frameDebugProvider = null;
            }

            if (Debugger.instance.rewind || ReadyForUpdate())
            {
                UpdateLayout();
            }
        }

        bool ReadyForUpdate()
        {
            if (updateDelay > 0)
            {
                updateDelay--;

                return false;
            }

            if (!HasValidLayout())
            {
                updateDelay = 2;

                return false;
            }

            return true;
        }

        bool HasValidLayout()
        {
            foreach (var graphGroup in graphGroups)
            {
                if (!graphGroup.HasValidLayout())
                {
                    return false;
                }
            }

            return true;
        }

        internal void GeometryChangededCallback()
        {
            const float verticalGroupSpacing = 100.0f;

            float2 anchorPosition = float2.zero;

            foreach (GraphNodeGroup group in graphGroups)
            {
                anchorPosition.y += group.GeometryChangededCallback(anchorPosition).height + verticalGroupSpacing;
            }
        }

        void UpdateLayout()
        {
            graphGroupsCache = new HashSet<GraphNodeGroup>(graphGroups);
            graphGroups.Clear();

            InitializeNodes();

            foreach (var graphGroup in graphGroupsCache)
            {
                var graphNodes = graphGroup.RemoveAll();

                foreach (var graphNode in graphNodes)
                {
                    RemoveElement(graphNode);
                }

                RemoveElement(graphGroup);
            }

            graphGroupsCache = null;
        }

        void InitializeNodes()
        {
            ExecutionGraphModel graph = ExecutionGraphModel.Create(debugMemory);

            foreach (ExecutionGroupModel groupModel in graph.groups)
            {
                GraphNodeGroup group = GetGroupFromCache(groupModel.references[0]);
                if (group == null)
                {
                    group = GraphNodeGroup.Create(this, groupModel.references[0]);
                    AddElement(group);
                }

                graphGroups.Add(group);

                group.UpdateLayout(groupModel);
            }
        }

        GraphNodeGroup GetGroupFromCache(DebugReference root)
        {
            Assert.IsTrue(graphGroupsCache != null);

            foreach (var group in graphGroupsCache)
            {
                if (group.root.Equals(root.identifier))
                {
                    graphGroupsCache.Remove(group);

                    return group;
                }
            }

            return null;
        }

        protected virtual void InitializeManipulators()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
        }
    }
}
