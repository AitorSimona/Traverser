using Unity.Mathematics;

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity.Kinematica.Editor
{
    internal struct GraphLayout
    {
        (GraphNode, Rect)[] nodes;
        (int, int)[] edges;

        static readonly int kNumIterations = 100;
        static readonly float kSeparationPercentagePerIteration = 0.2f;
        static readonly float kHorizontalSpacing = 100.0f;
        static readonly float kVerticalSpacing = 20.0f;
        static readonly float kMinOverlappingEdgeLength = 200.0f;
        static readonly float kOverlappingEdgeMargin = 50.0f;

        static readonly float2 kDown = new float2(0.0f, 1.0f);
        static readonly float2 kRight = new float2(1.0f, 0.0f);

        internal static GraphLayout Create(List<GraphNode> nodesList)
        {
            List<(GraphNode, Rect)> nodes = new List<(GraphNode, Rect)>(nodesList.Count);
            List<(int, int)> edges = new List<(int, int)>();

            for (int i = 0; i < nodesList.Count; ++i)
            {
                GraphNode node = nodesList[i];

                nodes.Add((node, new Rect(float2.zero, node.GetPosition().size)));

                foreach (GraphNodePort port in node.inputPorts)
                {
                    foreach (GraphNodeEdge edge in port.GetEdges())
                    {
                        int inputNode = nodesList.FindIndex(n => n == edge.target);
                        if (inputNode >= 0)
                        {
                            edges.Add((inputNode, i));
                        }
                    }
                }
            }

            return new GraphLayout()
            {
                nodes = nodes.ToArray(),
                edges = edges.ToArray()
            };
        }

        internal Rect ComputeLayout(Vector2 anchorPosition)
        {
            SolveNodesCollisions();

            Rect graphRect = ComputeGraphBoundingBox();
            TranslateGraph(anchorPosition - graphRect.position);
            ApplyNodePositions();

            return new Rect(anchorPosition, graphRect.size);
        }

        void ApplyNodePositions()
        {
            for (int i = 0; i < nodes.Length; ++i)
            {
                nodes[i].Item1.SetPosition(nodes[i].Item2);
            }
        }

        Rect ComputeGraphBoundingBox()
        {
            Rect graphRect = nodes[0].Item2;
            foreach ((GraphNode node, Rect rect) in nodes)
            {
                graphRect.xMin = math.min(graphRect.xMin, rect.xMin);
                graphRect.xMax = math.max(graphRect.xMax, rect.xMax);
                graphRect.yMin = math.min(graphRect.yMin, rect.yMin);
                graphRect.yMax = math.max(graphRect.yMax, rect.yMax);
            }

            return graphRect;
        }

        void SolveNodesCollisions()
        {
            bool collisions = true;

            for (int iteration = 0; iteration < kNumIterations; ++iteration)
            {
                bool iterationCollisions = false;

                for (int i = 0; i < nodes.Length; ++i)
                {
                    for (int j = 0; j < nodes.Length; ++j)
                    {
                        if (i == j || !IsEdge(i, j))
                        {
                            continue;
                        }

                        if (!collisions)
                        {
                            SeparateNodesFromEdge(i, j);
                        }

                        iterationCollisions = SeparateNodes(i, j, kRight, kHorizontalSpacing) || iterationCollisions;
                    }

                    List<int> inputs = GetInputs(i);
                    for (int j = 1; j < inputs.Count; ++j)
                    {
                        iterationCollisions = SeparateNodes(inputs[j - 1], inputs[j], kDown, kVerticalSpacing) || iterationCollisions;
                    }
                }

                collisions = iterationCollisions;
            }
        }

        bool SeparateNodes(int from, int to, float2 direction, float distance)
        {
            float2 fromPos = GetNodeBound(from, direction);
            float2 toPos = GetNodeBound(to, -direction);

            float2 fromTo = toPos - fromPos;
            float penetration = distance - math.dot(fromTo, direction);
            if (penetration > 0.0f)
            {
                float2 halfSeparation = direction * (penetration * kSeparationPercentagePerIteration * 0.5f);

                TranslateNode(from, -halfSeparation);
                TranslateNode(to, halfSeparation);

                return penetration >= distance * 0.5f;
            }

            return false;
        }

        void SeparateNodesFromEdge(int from, int to)
        {
            float sqMinEdgeLength = kMinOverlappingEdgeLength * kMinOverlappingEdgeLength;

            (float2, float2)edge = GetEdge(from, to);
            if (math.distancesq(edge.Item1, edge.Item2) < sqMinEdgeLength)
            {
                return;
            }

            float2 tangent = math.normalize(edge.Item2 - edge.Item1);
            float2 normal = math.normalize(new float2(-tangent.y, tangent.x));

            for (int i = 0; i < nodes.Length; ++i)
            {
                if (i == from || i == to)
                {
                    continue;
                }

                if (math.dot(GetNodeBound(i, tangent) - edge.Item1, tangent) <= 0.0f)
                {
                    continue;
                }
                if (math.dot(GetNodeBound(i, -tangent) - edge.Item2, -tangent) <= 0.0f)
                {
                    continue;
                }

                float distFirstBound = math.dot(GetNodeBound(i, normal, kOverlappingEdgeMargin) - edge.Item1, normal);
                float distSecondBound = math.dot(GetNodeBound(i, -normal, kOverlappingEdgeMargin) - edge.Item1, normal);

                if (distFirstBound * distSecondBound < 0.0f)
                {
                    // penetration
                    float2 separation;
                    if (math.abs(distFirstBound) < math.abs(distSecondBound))
                    {
                        separation = -math.abs(distFirstBound) * normal;
                    }
                    else
                    {
                        separation = math.abs(distSecondBound) * normal;
                    }

                    float2 halfSeparation = separation * kSeparationPercentagePerIteration * 0.5f;

                    TranslateNode(i, halfSeparation);
                    TranslateNode(from, -halfSeparation);
                    TranslateNode(to, -halfSeparation);
                }
            }
        }

        void TranslateNode(int nodeIndex, float2 translation)
        {
            nodes[nodeIndex].Item2.position = nodes[nodeIndex].Item2.position + (Vector2)translation;
        }

        void TranslateGraph(float2 translation)
        {
            for (int i = 0; i < nodes.Length; ++i)
            {
                TranslateNode(i, translation);
            }
        }

        float2 GetNodeBound(int nodeIndex, float2 direction, float margin = 0.0f)
        {
            Rect rect = nodes[nodeIndex].Item2;
            float halfWidth = rect.width * 0.5f;
            float halfHeight = rect.height * 0.5f;

            float2 snappedDir = new float2(direction.x / halfWidth, direction.y / halfHeight);
            float snapFactor = 1.0f / math.max(math.abs(snappedDir.x), math.abs(snappedDir.y));
            snappedDir = snappedDir * snapFactor;

            return (float2)rect.center + new float2(snappedDir.x * halfWidth, snappedDir.y * halfHeight) + math.normalize(snappedDir) * margin;
        }

        (float2, float2) GetEdge(int from, int to)
        {
            Rect fromRect = nodes[from].Item2;
            Rect toRect = nodes[to].Item2;

            return (new float2(fromRect.xMax, 0.5f * (fromRect.center.y + fromRect.yMax)), new float2(toRect.xMin, 0.5f * (toRect.center.y + toRect.yMax)));
        }

        bool IsEdge(int input, int output)
        {
            return edges.Contains((input, output));
        }

        List<int> GetInputs(int nodeIndex)
        {
            return edges.Where(e => e.Item2 == nodeIndex).Select(e => e.Item1).ToList();
        }
    }
}
