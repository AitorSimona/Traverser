using System;
using System.Reflection;
using System.Collections.Generic;
using Unity.Kinematica.UIElements;
using UnityEditor.Experimental.GraphView;

namespace Unity.Kinematica.Editor
{
    internal class GraphNodePort : Port
    {
        public GraphElement owner { get; private set; }

        // identifer of the debug object travelling through the port
        // input and output port having same identifier means they are connected
        public DebugIdentifier identifier;
        public bool selfNodeOnly;

        List<GraphNodeEdge> edges = new List<GraphNodeEdge>();

        public int numEdges => edges.Count;

        readonly string styleSheet = "GraphNodePort.uss";

        public GraphNodePort(Orientation orientation, Direction direction, Type type, DebugIdentifier identifier, bool selfNodeOnly)
            : base(orientation, direction, Capacity.Multi, type)
        {
            this.identifier = identifier;
            this.selfNodeOnly = selfNodeOnly;
            UIElementsUtils.ApplyStyleSheet(styleSheet, this);
        }

        public static GraphNodePort Create(Direction direction, Type type, DebugIdentifier identifier, bool selfNodeOnly)
        {
            var port = new GraphNodePort(Orientation.Horizontal, direction, type, identifier, selfNodeOnly);

            return port;
        }

        public virtual void Initialize(GraphElement graphElement, string name)
        {
            owner = graphElement;

            if (name != null)
            {
                portName = name;
            }

            visualClass = "Port_" + portType.Name;
        }

        public override void Connect(Edge edge)
        {
            base.Connect(edge);

            var inputNode = (edge.input as GraphNodePort).owner;
            var outputNode = (edge.output as GraphNodePort).owner;

            edges.Add(edge as GraphNodeEdge);
        }

        public override void Disconnect(Edge edge)
        {
            base.Disconnect(edge);

            if (!(edge as GraphNodeEdge).isConnected)
            {
                return;
            }

            edges.Remove(edge as GraphNodeEdge);
        }

        public void Update(string displayName, Type displayType)
        {
            if (displayType != null)
            {
                portType = displayType;
                visualClass = "Port_" + portType.Name;
            }

            if (!string.IsNullOrEmpty(displayName))
            {
                portName = displayName;
            }
        }

        public List<GraphNodeEdge> GetEdges()
        {
            return edges;
        }
    }
}
