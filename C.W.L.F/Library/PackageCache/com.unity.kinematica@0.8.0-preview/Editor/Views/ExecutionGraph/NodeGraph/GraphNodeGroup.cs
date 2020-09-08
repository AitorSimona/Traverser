using System;
using System.Collections.Generic;
using Unity.Kinematica.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;

using Unity.Mathematics;
using Unity.SnapshotDebugger;

using System.Linq;

namespace Unity.Kinematica.Editor
{
    internal class GraphNodeGroup : Group
    {
        GraphNodeView owner;

        HashSet<GraphNode> graphNodes = new HashSet<GraphNode>();

        HashSet<GraphNodeEdge> graphEdges = new HashSet<GraphNodeEdge>();

        HashSet<GraphNode> graphNodesCache;

        HashSet<GraphNodeEdge> graphEdgesCache;

        //internal GraphNodePort inputPort;

        public DebugIdentifier root { private set; get; }

        readonly string styleSheet = "GraphNodeGroup.uss";

        GraphNodeGroup(GraphNodeView graphView, DebugIdentifier root)
        {
            owner = graphView;
            this.root = root;

            UIElementsUtils.ApplyStyleSheet(styleSheet, this);

            RegisterCallback<GeometryChangedEvent>(GeometryChangededCallback);

            //inputPort = GraphNodePort.Create(
            //    Direction.Input, typeof(GraphNode), DebugIdentifier.Invalid/*, null*/);

            //inputPort.Initialize(this, string.Empty);

            // headerContainer.Add(inputPort);
        }

        public override bool IsSelectable()
        {
            return false;
        }

        internal static GraphNodeGroup Create(GraphNodeView graphView, DebugIdentifier root)
        {
            return new GraphNodeGroup(graphView, root);
        }

        internal bool HasValidLayout()
        {
            if (float.IsNaN(layout.width) || float.IsNaN(layout.height))
            {
                return false;
            }

            foreach (var graphNode in graphNodes)
            {
                if (float.IsNaN(graphNode.layout.width) || float.IsNaN(graphNode.layout.height))
                {
                    return false;
                }
            }

            return true;
        }

        Rect UpdateLayout(Vector2 anchorPosition)
        {
            GraphLayout graphLayout = GraphLayout.Create(graphNodes.ToList());
            return graphLayout.ComputeLayout(anchorPosition);
        }

        internal void UpdateLayout(ExecutionGroupModel group)
        {
            title = group.title;

            graphNodesCache = new HashSet<GraphNode>(graphNodes);
            graphEdgesCache = new HashSet<GraphNodeEdge>(graphEdges);

            graphNodes.Clear();
            graphEdges.Clear();

            CreateNodes(group);
            CreateEdges(group);

            foreach (var graphEdge in graphEdgesCache)
            {
                RemoveElement(graphEdge);
                owner.RemoveElement(graphEdge);
            }

            foreach (var graphNode in graphNodesCache)
            {
                RemoveElement(graphNode);
                owner.RemoveElement(graphNode);
            }

            graphNodesCache = null;
            graphEdgesCache = null;
        }

        internal Rect GeometryChangededCallback(Vector2 anchorPosition)
        {
            Rect rect = UpdateLayout(anchorPosition);

            foreach (var graphNode in graphNodes)
            {
                if (!ContainsElement(graphNode))
                {
                    AddElement(graphNode);
                }
            }

            foreach (var graphEdge in graphEdges)
            {
                if (!ContainsElement(graphEdge))
                {
                    AddElement(graphEdge);
                }
            }

            UpdateGeometryFromContent();

            return rect;
        }

        internal GraphNode[] RemoveAll()
        {
            RemoveAllEdges();
            return RemoveAllNodes();
        }

        void GeometryChangededCallback(GeometryChangedEvent e)
        {
            if (math.abs(e.oldRect.width - e.newRect.width) <= 10)
                return;

            if (math.abs(e.oldRect.height - e.newRect.height) <= 10)
                return;

            owner.GeometryChangededCallback();
        }

        void CreateNodes(ExecutionGroupModel group)
        {
            foreach (DebugReference reference in group.references)
            {
                GraphNode node = GetFromCacheOrCreateNode(reference);
                node?.UpdateState();
            }
        }

        void CreateEdges(ExecutionGroupModel group)
        {
            foreach (GraphNode inputNode in graphNodes)
            {
                foreach (GraphNode outputNode in graphNodes)
                {
                    if (inputNode == outputNode)
                    {
                        continue;
                    }

                    foreach (GraphNodePort outputPort in inputNode.outputPorts)
                    {
                        foreach (GraphNodePort inputPort in outputNode.inputPorts)
                        {
                            if (outputPort.identifier.Equals(inputPort.identifier))
                            {
                                if (outputPort.selfNodeOnly && !outputNode.reference.identifier.Equals(inputPort.identifier))
                                {
                                    continue;
                                }

                                if (inputPort.selfNodeOnly && !inputNode.reference.identifier.Equals(outputPort.identifier))
                                {
                                    continue;
                                }

                                GetFromCacheOrCreateEdge(inputPort, outputPort);
                            }
                        }
                    }
                }
            }
        }

        GraphNode GetFromCacheOrCreateNode(DebugReference reference)
        {
            GraphNode cacheNode = graphNodesCache.FirstOrDefault(n => n.reference.identifier.typeHashCode.Equals(reference.identifier.typeHashCode));
            if (cacheNode != null)
            {
                cacheNode.Reinitialize(reference);

                graphNodesCache.Remove(cacheNode);
                graphNodes.Add(cacheNode);

                return cacheNode;
            }
            else
            {
                GraphNode node = GraphNode.Create(owner, null, reference);
                if (node == null)
                {
                    return null;
                }

                graphNodes.Add(node);
                owner.AddElement(node);
                return node;
            }
        }

        GraphNodeEdge GetFromCacheOrCreateEdge(GraphNodePort inputPort, GraphNodePort outputPort)
        {
            GraphNodeEdge cacheEdge = graphEdgesCache.FirstOrDefault(e => e.input == inputPort && e.output == outputPort);
            if (cacheEdge != null)
            {
                graphEdgesCache.Remove(cacheEdge);
                graphEdges.Add(cacheEdge);
                return cacheEdge;
            }
            else
            {
                var edge = new GraphNodeEdge()
                {
                    input = inputPort,
                    output = outputPort
                };

                Connect(edge);

                graphEdges.Add(edge);
                return edge;
            }
        }

        bool Connect(GraphNodeEdge edge)
        {
            if (edge.input == null || edge.output == null)
                return false;

            var inputPort = edge.input as GraphNodePort;
            var outputPort = edge.output as GraphNodePort;
            var inputNode = inputPort.node as GraphNode;
            var outputNode = outputPort.node as GraphNode;

            if (inputNode == null || outputNode == null)
            {
                return false;
            }

            owner.AddElement(edge);

            edge.input.Connect(edge);
            edge.output.Connect(edge);

            inputNode.RefreshPorts();
            outputNode.RefreshPorts();

            edge.isConnected = true;

            return true;
        }

        GraphNode[] RemoveAllNodes()
        {
            var result = new GraphNode[graphNodes.Count];

            int writeIndex = 0;

            foreach (var graphNode in graphNodes)
            {
                RemoveElement(graphNode);

                result[writeIndex++] = graphNode;
            }

            graphNodes.Clear();

            return result;
        }

        void RemoveAllEdges()
        {
            foreach (var graphEdge in graphEdges)
            {
                owner.RemoveElement(graphEdge);
            }

            graphEdges.Clear();
        }
    }
}
