using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    internal class ManipulatorGizmo : IDisposable
    {
        public enum GizmoMode
        {
            Move,
            Rotate,
            RotateNormal,
        }

        static ManipulatorGizmo s_Instance;

        public static ManipulatorGizmo Instance => s_Instance ?? (s_Instance = new ManipulatorGizmo());

        AffineTransform m_Transform;
        GameObject m_GizmoObject;
        GameObject m_PreviewTarget;

        public event Action<Vector3> PositionChanged;
        public event Action<Quaternion> RotationChanged;

        ManipulatorGizmo() {}

        GizmoMode Mode { get; set; }
        string CurrentPropertyPath { get; set; }

        void OnSceneGUI(SceneView obj)
        {
            if (CurrentPropertyPath == default)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            var gizmoTransform = m_GizmoObject.transform;
            var localMode = Tools.pivotRotation == PivotRotation.Local || m_PreviewTarget == null;

            switch (Mode)
            {
                case GizmoMode.Move:
                {
                    var currentPosition = (localMode && m_PreviewTarget != null) ? gizmoTransform.localPosition + m_PreviewTarget.transform.position : gizmoTransform.position;
                    var currentRotation = localMode ? gizmoTransform.localRotation : gizmoTransform.rotation;

                    var newPosition = Handles.PositionHandle(currentPosition, currentRotation);

                    if (m_PreviewTarget != null)
                    {
                        if (localMode)
                        {
                            newPosition = newPosition - m_PreviewTarget.transform.position;
                        }
                        else
                        {
                            newPosition = m_PreviewTarget.transform.InverseTransformPoint(newPosition);
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Transform.t = newPosition;
                        gizmoTransform.localPosition = newPosition;
                        PositionChanged?.Invoke(newPosition);
                        gizmoTransform.hasChanged = false;
                    }
                    break;
                }
                case GizmoMode.Rotate:
                case GizmoMode.RotateNormal:
                {
                    Quaternion newRotation = Handles.RotationHandle(localMode ? gizmoTransform.localRotation : gizmoTransform.rotation, gizmoTransform.position);

                    if (Mode == GizmoMode.RotateNormal)
                    {
                        var savedColor = Handles.color;
                        Handles.color = Color.yellow;
                        Handles.ArrowHandleCap(0, gizmoTransform.position, localMode ? gizmoTransform.localRotation : gizmoTransform.rotation, HandleUtility.GetHandleSize(gizmoTransform.position) * 1.1f, EventType.Repaint);
                        Handles.color = savedColor;
                    }

                    if (m_PreviewTarget != null && !localMode)
                    {
                        newRotation = Quaternion.Inverse(m_PreviewTarget.transform.rotation) * newRotation;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        m_Transform.q = newRotation;
                        gizmoTransform.localRotation = newRotation;
                        RotationChanged?.Invoke(newRotation);
                        gizmoTransform.hasChanged = false;
                    }
                    break;
                }
            }
        }

        public void Hook(SerializedProperty property, Vector3 position)
        {
            Mode = GizmoMode.Move;

            Unhook();
            StartPicker(position, Quaternion.identity);
            CurrentPropertyPath = property.propertyPath;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public void Hook(SerializedProperty property, Quaternion rotation, GizmoMode gizmoMode = GizmoMode.Rotate)
        {
            Mode = gizmoMode;

            Unhook();
            StartPicker(Vector3.zero, rotation);
            CurrentPropertyPath = property.propertyPath;
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.RepaintAll();
        }

        public void Unhook()
        {
            CurrentPropertyPath = default;
            PositionChanged = null;
            RotationChanged = null;

            SceneView.duringSceneGui -= OnSceneGUI;;
            StopPicker();
            SceneView.RepaintAll();
        }

        public void ReTransform(SerializedProperty property, Vector3 position)
        {
            if (IsTarget(property))
            {
                m_GizmoObject.transform.localPosition = position;
            }
        }

        public void ReTransform(SerializedProperty property, Quaternion rotation)
        {
            if (IsTarget(property))
            {
                m_GizmoObject.transform.localRotation = rotation;
            }
        }

        void StartPicker(Vector3 position, Quaternion rotation)
        {
            m_GizmoObject = new GameObject();
            m_GizmoObject.hideFlags |= HideFlags.HideAndDontSave;
            if (m_PreviewTarget != null)
            {
                m_GizmoObject.transform.SetParent(m_PreviewTarget.transform);
            }

            m_GizmoObject.transform.localPosition = position;
            m_GizmoObject.transform.localRotation = rotation;
            Selection.selectionChanged += SelectionChanged;
        }

        public void Dispose()
        {
            StopPicker();
            PositionChanged = null;
            RotationChanged = null;
        }

        void SelectionChanged()
        {
            Unhook();
        }

        void StopPicker()
        {
            if (m_GizmoObject != null)
            {
                Selection.selectionChanged -= SelectionChanged;
                UnityEngine.Object.DestroyImmediate(m_GizmoObject);
                m_GizmoObject = null;
            }
        }

        public void SetPreviewTarget(GameObject previewTarget)
        {
            m_PreviewTarget = previewTarget;
            Unhook();
        }

        public bool IsTarget(SerializedProperty property)
        {
            return CurrentPropertyPath == property.propertyPath;
        }
    }
}
