using System;
using System.Reflection;
using Unity.Kinematica.Editor.GenericStruct;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Unity.Kinematica.Editor
{
    [Serializable]
    internal class Payload : IDisposable
    {
        ScriptableObject m_ScriptableObject;

        [SerializeField]
        private string serializedData;

        [SerializeField]
        private string assemblyQualifiedTypeName;

        public static Payload Create(Type type)
        {
            return new Payload(type);
        }

        public Type Type
        {
            get
            {
                Debug.Assert(!string.IsNullOrEmpty(assemblyQualifiedTypeName));
                return Type.GetType(assemblyQualifiedTypeName);
            }
        }

        public string SimplifiedTypeName
        {
            get
            {
                if (!assemblyQualifiedTypeName.Contains(","))
                {
                    return assemblyQualifiedTypeName;
                }

                return assemblyQualifiedTypeName.Substring(0, assemblyQualifiedTypeName.IndexOf(','));
            }
        }

        public bool ValidPayloadType
        {
            get { return Type != null && ScriptableObject != null && Type != null; }
        }

        internal ScriptableObject ScriptableObject
        {
            get
            {
                InitializeScriptableObject();

                Debug.Assert(new SerializedObject(m_ScriptableObject).FindProperty("m_Value") != null);

                return m_ScriptableObject;
            }
        }

        void InitializeScriptableObject()
        {
            if (m_ScriptableObject != null)
            {
                return;
            }

            if (Type == null)
            {
                return;
            }

            var scriptableObject = GenericStructHelper.Wrap(Type);
            if (scriptableObject == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(serializedData))
            {
                scriptableObject.Deserialize(serializedData);
            }

            m_ScriptableObject = scriptableObject;
        }

        internal int GetHashedData()
        {
            Serialize();
            if (string.IsNullOrEmpty(serializedData))
            {
                return 0;
            }

            return serializedData.GetHashCode();
        }

        Payload(Type type)
        {
            Debug.Assert(type.IsValueType);

            assemblyQualifiedTypeName = type.AssemblyQualifiedName;

            InitializeScriptableObject();
            Serialize();
        }

        public T GetValue<T>() where T : struct
        {
            if (ScriptableObject == null)
            {
                return default;
            }

            var storage = ScriptableObject as IValueStore<T>;
            return storage.val;
        }

        public void SetValue<T>(T value) where T : struct
        {
            if (ScriptableObject == null)
            {
                return;
            }

            var storage = ScriptableObject as IValueStore<T>;
            storage.val = value;
            Serialize();
        }

        public void SetValueObject(object value)
        {
            MethodInfo setValueMethod = GetType().GetMethod("SetValue", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(value.GetType());
            setValueMethod.Invoke(this, new object[] { value });
        }

        public void Dispose()
        {
            if (m_ScriptableObject != null)
            {
                Object.DestroyImmediate(m_ScriptableObject);
                m_ScriptableObject = null;
            }
        }

        internal void Serialize()
        {
            if (m_ScriptableObject != null)
            {
                serializedData = ScriptableObject.Serialize();
            }
        }

        internal bool ScriptableObjectInitialized()
        {
            return m_ScriptableObject != null;
        }
    }
}
