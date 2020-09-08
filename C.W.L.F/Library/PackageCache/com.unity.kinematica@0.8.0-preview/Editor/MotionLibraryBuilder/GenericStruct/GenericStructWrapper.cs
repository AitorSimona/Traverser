using System;
using UnityEngine;

namespace Unity.Kinematica.Editor.GenericStruct
{
    internal interface IValue
    {
    }

    internal interface IValueStore<T> : IValue where T : struct
    {
        T val { get; set; }
    }

    [Serializable]
    public class GenericStructWrapper<T> : ScriptableObject, IValueStore<T> where T : struct
    {
        [SerializeField]
        T m_Value;

        public T val
        {
            get { return m_Value; }
            set { m_Value = value; }
        }
    }
}
