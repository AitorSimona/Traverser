using System.Collections.Generic;
using System.Collections;
using System;

namespace UnityEngine.Animations.Rigging
{
    /// <summary>
    /// This struct defines a List of WeightedTransform.
    /// WeightedTransformArray can be animated (as opposed to System.List and C# arrays) and is implemented
    /// with a hard limit of eight WeightedTransform elements.
    /// </summary>
    /// <seealso cref="WeightedTransform"/>
    [System.Serializable]
    public struct WeightedTransformArray : IList<WeightedTransform>, IList
    {
        /// <summary>Maximum number of elements in WeightedTransformArray.</summary>
        public static readonly int k_MaxLength = 8;

        [SerializeField, NotKeyable] private int m_Length;

        [SerializeField] private WeightedTransform m_Item0;
        [SerializeField] private WeightedTransform m_Item1;
        [SerializeField] private WeightedTransform m_Item2;
        [SerializeField] private WeightedTransform m_Item3;
        [SerializeField] private WeightedTransform m_Item4;
        [SerializeField] private WeightedTransform m_Item5;
        [SerializeField] private WeightedTransform m_Item6;
        [SerializeField] private WeightedTransform m_Item7;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="size">Size of the array. This will clamped to be a number in between 0 and k_MaxLength.</param>
        /// <seealso cref="WeightedTransformArray.k_MaxLength"/>
        public WeightedTransformArray(int size)
        {
            m_Length = ClampSize(size);
            m_Item0 = new WeightedTransform();
            m_Item1 = new WeightedTransform();
            m_Item2 = new WeightedTransform();
            m_Item3 = new WeightedTransform();
            m_Item4 = new WeightedTransform();
            m_Item5 = new WeightedTransform();
            m_Item6 = new WeightedTransform();
            m_Item7 = new WeightedTransform();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        public IEnumerator<WeightedTransform> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        /// <summary>
        /// Adds an item to the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The object to add to the WeightedTransformArray.</param>
        /// <returns>The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection.</returns>
        int IList.Add(object value)
        {
            Add((WeightedTransform)value);
            return m_Length - 1;
        }

        /// <summary>
        /// Adds an item to the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The WeightedTransform to add to the WeightedTransformArray.</param>
        public void Add(WeightedTransform value)
        {
            if (m_Length >= k_MaxLength)
                throw new ArgumentException($"This array cannot have more than '{k_MaxLength}' items.");

            Set(m_Length, value);

            ++m_Length;
        }

        /// <summary>
        /// Removes all items from the WeightedTransformArray.
        /// </summary>
        public void Clear()
        {
            m_Length = 0;
        }

        /// <summary>
        /// Determines the index of a specific item in the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The object to locate in the WeightedTransformArray.</param>
        /// <returns>The index of value if found in the list; otherwise, -1.</returns>
        int IList.IndexOf(object value) => IndexOf((WeightedTransform)value);

        /// <summary>
        /// Determines the index of a specific item in the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The WeightedTransform to locate in the WeightedTransformArray.</param>
        /// <returns>The index of value if found in the list; otherwise, -1.</returns>
        public int IndexOf(WeightedTransform value)
        {
            for (int i = 0; i < m_Length; ++i)
            {
                if (Get(i).Equals(value))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Determines whether the WeightedTransformArray contains a specific value.
        /// </summary>
        /// <param name="value">The object to locate in the WeightedTransformArray.</param>
        /// <returns>true if the Object is found in the WeightedTransformArray; otherwise, false.</returns>
        bool IList.Contains(object value) => Contains((WeightedTransform)value);

        /// <summary>
        /// Determines whether the WeightedTransformArray contains a specific value.
        /// </summary>
        /// <param name="value">The WeightedTransform to locate in the WeightedTransformArray.</param>
        /// <returns>true if the Object is found in the WeightedTransformArray; otherwise, false.</returns>
        public bool Contains(WeightedTransform value)
        {
            for (int i = 0; i < m_Length; ++i)
            {
                if (Get(i).Equals(value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Copies the elements of the WeightedTransform to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from WeightedTransform. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("The array cannot be null.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("The starting array index cannot be negative.");
            if (Count > array.Length - arrayIndex + 1)
                throw new ArgumentException("The destination array has fewer elements than the collection.");

            for (int i = 0; i < m_Length; i++)
            {
                array.SetValue(Get(i), i + arrayIndex);
            }
        }

        /// <summary>
        /// Copies the elements of the WeightedTransform to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from WeightedTransform. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(WeightedTransform[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("The array cannot be null.");
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("The starting array index cannot be negative.");
            if (Count > array.Length - arrayIndex + 1)
                throw new ArgumentException("The destination array has fewer elements than the collection.");

            for (int i = 0; i < m_Length; i++)
            {
                array[i + arrayIndex] = Get(i);
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The object to remove from the WeightedTransformArray.</param>
        void IList.Remove(object value) { Remove((WeightedTransform)value); }

        /// <summary>
        /// Removes the first occurrence of a specific WeightedTransform from the WeightedTransformArray.
        /// </summary>
        /// <param name="value">The WeightedTransform to remove from the WeightedTransformArray.</param>
        /// <returns>True if value was removed from the array, false otherwise.</returns>
        public bool Remove(WeightedTransform value)
        {
            for (int i = 0; i < m_Length; ++i)
            {
                if (Get(i).Equals(value))
                {
                    for (; i < m_Length - 1; ++i)
                    {
                        Set(i, Get(i + 1));
                    }

                    --m_Length;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the WeightedTransformArray item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            CheckOutOfRangeIndex(index);

            for (int i = index; i < m_Length - 1; ++i)
            {
                Set(i, Get(i + 1));
            }

            --m_Length;
        }

        /// <summary>
        /// Inserts an item to the WeightedTransformArray at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The object to insert into the WeightedTransformArray.</param>
        void IList.Insert(int index, object value) => Insert(index, (WeightedTransform)value);

        /// <summary>
        /// Inserts an item to the WeightedTransformArray at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted.</param>
        /// <param name="value">The WeightedTransform to insert into the WeightedTransformArray.</param>
        public void Insert(int index, WeightedTransform value)
        {
            if (m_Length >= k_MaxLength)
                throw new ArgumentException($"This array cannot have more than '{k_MaxLength}' items.");

            CheckOutOfRangeIndex(index);

            if (index >= m_Length)
            {
                Add(value);
                return;
            }

            for (int i = m_Length; i > index; --i)
            {
                Set(i, Get(i - 1));
            }

            Set(index, value);
            ++m_Length;
        }

        /// <summary>
        /// Clamps specified value in between 0 and k_MaxLength.
        /// </summary>
        /// <param name="size">Size value.</param>
        /// <returns>Value in between 0 and k_MaxLength.</returns>
        /// <seealso cref="WeightedTransformArray.k_MaxLength"/>
        private static int ClampSize(int size)
        {
            return Mathf.Clamp(size, 0, k_MaxLength);
        }

        /// <summary>
        /// Checks whether specified index value in within bounds.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <exception cref="IndexOutOfRangeException">Index value is not between 0 and k_MaxLength.</exception>
        /// <seealso cref="WeightedTransformArray.k_MaxLength"/>
        private void CheckOutOfRangeIndex(int index)
        {
            if (index < 0 || index >= k_MaxLength)
                throw new IndexOutOfRangeException($"Index {index} is out of range of '{m_Length}' Length.");
        }

        /// <summary>
        /// Retrieves a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <returns>The WeightedTransform value at specified index.</returns>
        private WeightedTransform Get(int index)
        {
            CheckOutOfRangeIndex(index);

            switch(index)
            {
                case 0: return m_Item0;
                case 1: return m_Item1;
                case 2: return m_Item2;
                case 3: return m_Item3;
                case 4: return m_Item4;
                case 5: return m_Item5;
                case 6: return m_Item6;
                case 7: return m_Item7;
            }

            // Shouldn't happen.
            return m_Item0;
        }

        /// <summary>
        /// Sets a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="value">The WeightedTransform value to set.</param>
        private void Set(int index, WeightedTransform value)
        {
            CheckOutOfRangeIndex(index);

            switch(index)
            {
                case 0: m_Item0 = value; break;
                case 1: m_Item1 = value; break;
                case 2: m_Item2 = value; break;
                case 3: m_Item3 = value; break;
                case 4: m_Item4 = value; break;
                case 5: m_Item5 = value; break;
                case 6: m_Item6 = value; break;
                case 7: m_Item7 = value; break;
            }
        }

        /// <summary>
        /// Sets a weight on a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="weight">The weight value to set.</param>
        public void SetWeight(int index, float weight)
        {
            var weightedTransform = Get(index);
            weightedTransform.weight = weight;

            Set(index, weightedTransform);
        }

        /// <summary>
        /// Retrieves a weight on a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <returns>The weight value at specified index.</returns>
        public float GetWeight(int index)
        {
            return Get(index).weight;
        }

        /// <summary>
        /// Sets the Transform reference on a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <param name="transform">The Transform reference to set.</param>
        public void SetTransform(int index, Transform transform)
        {
            var weightedTransform = Get(index);
            weightedTransform.transform = transform;

            Set(index, weightedTransform);
        }

        /// <summary>
        /// Retrieves a Transform reference on a WeightedTransform at specified index.
        /// </summary>
        /// <param name="index">Index value.</param>
        /// <returns>The Transform reference at specified index.</returns>
        public Transform GetTransform(int index)
        {
            return Get(index).transform;
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        object IList.this[int index] { get => (object)Get(index); set => Set(index, (WeightedTransform)value); }

        /// <summary>
        /// Gets or sets the WeightedTransform at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the WeightedTransform to get or set.</param>
        public WeightedTransform this[int index] { get => Get(index); set => Set(index, value); }

        /// <summary>The number of elements contained in the WeightedTransformArray.</summary>
        public int Count { get => m_Length; }

        /// <summary>
        /// Retrieves whether WeightedTransformArray is read-only. Always false.
        /// </summary>
        public bool IsReadOnly { get => false; }

        /// <summary>
        /// Retrieves whether WeightedTransformArray has a fixed size. Always false.
        /// </summary>
        public bool IsFixedSize { get => false; }

        bool ICollection.IsSynchronized { get => true; }

        object ICollection.SyncRoot { get => null; }

        [System.Serializable]
        private struct Enumerator : IEnumerator<WeightedTransform>
        {
            private WeightedTransformArray m_Array;
            private int m_Index;

            public Enumerator(ref WeightedTransformArray array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                m_Index++;
                return (m_Index < m_Array.Count);
            }

            public void Reset()
            {
                m_Index = -1;
            }

            void IDisposable.Dispose() { }

            public WeightedTransform Current => m_Array.Get(m_Index);

            object IEnumerator.Current => Current;
        }
    }
}

