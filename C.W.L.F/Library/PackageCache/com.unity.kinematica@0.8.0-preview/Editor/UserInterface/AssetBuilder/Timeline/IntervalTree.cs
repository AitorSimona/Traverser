using System;
using System.Collections.Generic;
using UnityEngine.Assertions.Comparers;
using UnityEngine.Profiling;

namespace Unity.Kinematica.Editor
{
    interface IInterval
    {
        float intervalStart { get; }
        float intervalEnd { get; }
    }

    struct IntervalTreeNode         // interval node,
    {
        public float center;        // midpoint for this node
        public int first;           // index of first element of this node in m_Entries
        public int last;            // index of the last element of this node in m_Entries
        public int left;            // index in m_Nodes of the left subnode
        public int right;           // index in m_Nodes of the right subnode
    }

    class IntervalTree<T> where T : IInterval, new()
    {
        internal struct Entry
        {
            public float intervalStart;
            public float intervalEnd;
            public T item;
        }

        const int kMinNodeSize = 10;     // the minimum number of entries to have subnodes
        const int kInvalidNode = -1;
        const float kCenterUnknown = float.MaxValue; // center hasn't been calculated. indicates no children

        readonly List<Entry> m_Entries = new List<Entry>();
        readonly List<IntervalTreeNode> m_Nodes = new List<IntervalTreeNode>();

        /// <summary>
        /// Whether the tree will be rebuilt on the next query
        /// </summary>
        public bool dirty { get; internal set; }

        /// <summary>
        /// Add an IInterval to the tree
        /// </summary>
        public void Add(T item)
        {
            if (item == null)
                return;

            m_Entries.Add(
                new Entry
                {
                    intervalStart = item.intervalStart,
                    intervalEnd = item.intervalEnd,
                    item = item
                }
            );
            dirty = true;
        }

        /// <summary>
        /// Query the tree at a particular range of time
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="results"></param>
        public void IntersectsWithRange(float start, float end, List<T> results)
        {
            Profiler.BeginSample("IntervalTree.IntersectsWithRange");
            try
            {
                if (start > end)
                    return;

                if (m_Entries.Count == 0)
                    return;

                if (dirty)
                {
                    Rebuild();
                    dirty = false;
                }

                if (m_Nodes.Count > 0)
                    QueryRange(m_Nodes[0], start, end, results);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        void QueryRange(IntervalTreeNode intervalTreeNode, float start, float end, List<T> results)
        {
            for (int i = intervalTreeNode.first; i <= intervalTreeNode.last; i++)
            {
                Entry entry = m_Entries[i];
                if (end >= entry.intervalStart && start < entry.intervalEnd)
                {
                    results.Add(entry.item);
                }
            }

            results.AddRange(results);

            if (FloatComparer.s_ComparerWithDefaultTolerance.Equals(intervalTreeNode.center, kCenterUnknown))
                return;
            if (intervalTreeNode.left != kInvalidNode && start < intervalTreeNode.center)
                QueryRange(m_Nodes[intervalTreeNode.left], start, end, results);
            if (intervalTreeNode.right != kInvalidNode && end > intervalTreeNode.center)
                QueryRange(m_Nodes[intervalTreeNode.right], start, end, results);
        }

        void Rebuild()
        {
            m_Nodes.Clear();
            m_Nodes.Capacity = m_Entries.Capacity;
            Rebuild(0, m_Entries.Count - 1);
        }

        int Rebuild(int start, int end)
        {
            IntervalTreeNode intervalTreeNode = new IntervalTreeNode();

            // minimum size, don't subdivide
            int count = end - start + 1;
            if (count < kMinNodeSize)
            {
                intervalTreeNode = new IntervalTreeNode() {center = kCenterUnknown, first = start, last = end, left = kInvalidNode, right = kInvalidNode};
                m_Nodes.Add(intervalTreeNode);
                return m_Nodes.Count - 1;
            }

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = start; i <= end; i++)
            {
                var o = m_Entries[i];
                min = Math.Min(min, o.intervalStart);
                max = Math.Max(max, o.intervalEnd);
            }

            float center = (max + min) / 2;
            intervalTreeNode.center = center;

            // first pass, put every thing left of center, left
            int x = start;
            int y = end;
            while (true)
            {
                while (x <= end && m_Entries[x].intervalEnd < center)
                    x++;

                while (y >= start && m_Entries[y].intervalEnd >= center)
                    y--;

                if (x > y)
                    break;

                var nodeX = m_Entries[x];
                var nodeY = m_Entries[y];

                m_Entries[y] = nodeX;
                m_Entries[x] = nodeY;
            }

            intervalTreeNode.first = x;

            // second pass, put every start passed the center right
            y = end;
            while (true)
            {
                while (x <= end && m_Entries[x].intervalStart <= center)
                    x++;

                while (y >= start && m_Entries[y].intervalStart > center)
                    y--;

                if (x > y)
                    break;

                var nodeX = m_Entries[x];
                var nodeY = m_Entries[y];

                m_Entries[y] = nodeX;
                m_Entries[x] = nodeY;
            }

            intervalTreeNode.last = y;

            // reserve a place
            m_Nodes.Add(new IntervalTreeNode());
            int index = m_Nodes.Count - 1;

            intervalTreeNode.left = kInvalidNode;
            intervalTreeNode.right = kInvalidNode;

            if (start < intervalTreeNode.first)
                intervalTreeNode.left = Rebuild(start, intervalTreeNode.first - 1);

            if (end > intervalTreeNode.last)
                intervalTreeNode.right = Rebuild(intervalTreeNode.last + 1, end);

            m_Nodes[index] = intervalTreeNode;
            return index;
        }
    }
}
