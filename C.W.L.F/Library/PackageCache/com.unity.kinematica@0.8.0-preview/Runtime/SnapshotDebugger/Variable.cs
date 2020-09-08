using System;
using System.Reflection;
using UnityEngine;

namespace Unity.SnapshotDebugger
{
    public sealed class Variable : Serializable
    {
        public GameObject gameObject
        {
            get { return provider.gameObject; }
        }

        public object value
        {
            get { return field.GetValue(provider); }
            set { field.SetValue(provider, value); }
        }

        public SnapshotAttribute attribute
        {
            get; private set;
        }

        public string name
        {
            get { return field.Name; }
        }

        public Variable(SnapshotProvider provider, FieldInfo field, SnapshotAttribute attribute)
        {
            this.provider = provider;
            this.field = field;
            this.attribute = attribute;
        }

        public void WriteToStream(Buffer buffer)
        {
            buffer.WriteType(value.GetType());

            if (value is Serializable serializable)
            {
                serializable.WriteToStream(buffer);
            }
            else
            {
                buffer.WriteBlittableGeneric(value);
            }
        }

        public void ReadFromStream(Buffer buffer)
        {
            Type type = buffer.ReadType();

            if (type == null)
            {
                throw new InvalidOperationException("Failed to read type information");
            }

            if (value is IDisposable disposable)
            {
                disposable.Dispose();
            }

            if (typeof(Serializable).IsAssignableFrom(type) == true)
            {
                if (type.IsClass)
                {
                    (value as Serializable).ReadFromStream(buffer);
                }
                else
                {
                    var serializable = Activator.CreateInstance(type) as Serializable;

                    serializable.ReadFromStream(buffer);

                    value = serializable;
                }
            }
            else
            {
                value = buffer.ReadBlittableGeneric(type);
            }
        }

        SnapshotProvider provider;
        FieldInfo field;
    }
}
