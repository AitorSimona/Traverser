using Unity.Kinematica.UIElements;
using UnityEditor.Experimental.GraphView;

namespace Unity.Kinematica.Editor
{
    internal class GraphNodeEdge : Edge
    {
        public bool isConnected = false;

        readonly string styleSheet = "GraphNodeEdge.uss";

        public GraphNodeEdge()
        {
            UIElementsUtils.ApplyStyleSheet(styleSheet, this);
        }

        public GraphNode source
        {
            get
            {
                var port = input as GraphNodePort;

                if (port != null)
                {
                    return port.node as GraphNode;
                }

                return null;
            }
        }

        public GraphNode target
        {
            get
            {
                var port = output as GraphNodePort;

                if (port != null)
                {
                    return port.node as GraphNode;
                }

                return null;
            }
        }

        public override bool IsMovable()
        {
            return false;
        }

        public override bool IsResizable()
        {
            return false;
        }

        public override bool IsSelectable()
        {
            return false;
        }
    }
}
