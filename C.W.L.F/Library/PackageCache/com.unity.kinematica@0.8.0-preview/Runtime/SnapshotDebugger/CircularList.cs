using System;

namespace Unity.SnapshotDebugger
{
    public class CircularList<T>
    {
        public CircularList(int capacity = 2)
        {
            m_Elements = new T[capacity];
            m_FirstIndex = 0;
            m_Count = 0;
        }

        public T this[int index]
        {
            get
            {
                return m_Elements[(m_FirstIndex + index) % m_Elements.Length];
            }
            set
            {
                m_Elements[(m_FirstIndex + index) % m_Elements.Length] = value;
            }
        }

        public int Count
        {
            get { return m_Count; }
        }

        public T Last => this[m_Count - 1];

        public void PushBack(T elem)
        {
            if (m_Count >= m_Elements.Length)
            {
                T[] elements = new T[2 * (m_Count + 1)];
                for (int i = 0; i < m_Count; ++i)
                {
                    elements[i] = this[i];
                }
                m_Elements = elements;
                m_FirstIndex = 0;
            }

            ++m_Count;
            this[m_Count - 1] = elem;
        }

        public void PopFront()
        {
            if (m_Count > 0)
            {
                m_Elements[m_FirstIndex] = default(T);
                m_FirstIndex = (m_FirstIndex + 1) % m_Elements.Length;
                --m_Count;
            }
        }

        public void PopBack()
        {
            if (m_Count > 0)
            {
                --m_Count;
            }
        }

        public void SwapElements(int i, int j)
        {
            if (i == j)
            {
                return;
            }

            T temp = this[i];
            this[i] = this[j];
            this[j] = temp;
        }

        T[]     m_Elements;
        int     m_FirstIndex;
        int     m_Count;
    }
}
