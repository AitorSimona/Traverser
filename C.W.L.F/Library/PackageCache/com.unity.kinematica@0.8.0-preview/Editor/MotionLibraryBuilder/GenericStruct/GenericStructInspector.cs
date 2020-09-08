using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor.GenericStruct
{
    [CustomEditor(typeof(GenericStructWrapper<>), true)]
    internal class GenericStructInspector : UnityEditor.Editor
    {
        EventCallback<IChangeEvent> m_ChangeEvent;

        public event Action StructModified;

        void OnEnable()
        {
            m_ChangeEvent = OnPropertyFieldChanged;
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject.targetObject == null)
            {
                return;
            }

            SerializedProperty value = serializedObject.FindProperty("m_Value");

            if (!value.hasChildren)
            {
                return;
            }

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(value, new GUIContent("Payload"), true);
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "field modification");
                m_ChangeEvent.Invoke(null);
            }
        }

        void OnPropertyFieldChanged(IChangeEvent evt)
        {
            StructModified?.Invoke();
        }
    }
}
