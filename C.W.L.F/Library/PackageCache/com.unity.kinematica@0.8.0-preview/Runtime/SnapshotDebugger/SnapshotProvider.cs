using System;
using System.Reflection;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.SnapshotDebugger
{
    public abstract class SnapshotProvider : MonoBehaviour, Serializable
    {
        public Identifier<SnapshotProvider> identifier
        {
            get; internal set;
        }

        public Identifier<Aggregate> aggregate
        {
            get; internal set;
        }

        public virtual void Awake()
        {
            CollectVariables();
        }

        public virtual void OnEnable()
        {
            Debugger.registry.OnEnable(this);
        }

        public virtual void OnDisable()
        {
            Debugger.registry.OnDisable(this);
        }

        public virtual void WriteToStream(Buffer buffer)
        {
// TODO: Well, well, record/rewind in non-editor build doesn't make much sense, right?
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                CollectVariables();
            }
#endif

            WriteVariables(buffer);
        }

        public virtual void ReadFromStream(Buffer buffer)
        {
            if (!buffer.EndRead)
            {
                ReadVariables(buffer);
            }
        }

        public virtual void OnEarlyUpdate(bool rewind)
        {
        }

        /// <summary>
        /// Returns true if the provider need post process callbacks called after serialization and deserialization
        /// </summary>
        public virtual bool RequirePostProcess => false;

        /// <summary>
        /// Post process callback called after all snapshot objects have been serialized, can be use to serialize additional data
        /// </summary>
        public virtual void OnWritePostProcess(Buffer buffer)
        {
        }

        /// <summary>
        /// Post process callback called after all snapshot objects have been deserialized, can be use to deserialize additional data
        /// </summary>
        public virtual void OnReadPostProcess(Buffer buffer)
        {
        }

        protected virtual void WriteVariables(Buffer buffer)
        {
            short size = (short)_variables.Length;

            buffer.Write(size);

            foreach (var variable in _variables)
            {
                buffer.Write(variable.name.GetHashCode());

                variable.WriteToStream(buffer);
            }
        }

        protected virtual void ReadVariables(Buffer buffer)
        {
            short size = buffer.Read16();

            for (int i = 0; i < size; i++)
            {
                int hashCode = buffer.Read32();

                int index = Array.FindIndex(_variables,
                    variable => variable.name.GetHashCode() == hashCode);

                if (index == -1)
                {
                    break;
                }

                _variables[index].ReadFromStream(buffer);
            }
        }

        protected virtual void CollectVariables()
        {
            var result = new List<Variable>();

            foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsDefined(typeof(SnapshotAttribute), true))
                {
                    object[] attributes = field.GetCustomAttributes(typeof(SnapshotAttribute), true);

                    if (attributes.Length > 0)
                    {
                        var attribute = attributes[0] as SnapshotAttribute;

                        result.Add(new Variable(this, field, attribute));
                    }
                }
            }

            _variables = result.ToArray();
        }

        Variable[] _variables;
    }
}
