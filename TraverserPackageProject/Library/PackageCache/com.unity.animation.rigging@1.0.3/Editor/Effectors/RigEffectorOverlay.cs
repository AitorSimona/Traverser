using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomOverlay(typeof(RigEffector))]
    class RigEffectorOverlay : IRigEffectorOverlay
    {
        private SerializedObject m_SerializedObject;

        private SerializedProperty m_Visible;
        private SerializedProperty m_Shape;
        private SerializedProperty m_Color;
        private SerializedProperty m_Size;
        private SerializedProperty m_Position;
        private SerializedProperty m_Rotation;

        private bool m_ExpandOverlay;

        private static GUIContent s_VisibleLabel = new GUIContent("Visible");
        private static GUIContent s_ShapeLabel = new GUIContent("Shape");
        private static GUIContent s_ColorLabel = new GUIContent("Color");
        private static GUIContent s_SizeLabel = new GUIContent("Size");
        private static GUIContent s_PositionLabel = new GUIContent("Position");
        private static GUIContent s_RotationLabel = new GUIContent("Rotation");

        private static GUIContent s_RemoveLabel = new GUIContent("Remove");

        private static string s_ExpandOverlayPrefKey = "AnimationRigging.ExpandOverlay";

        public void Initialize(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            SerializedProperty data = m_SerializedObject.FindProperty("m_Data");

            m_Visible = data.FindPropertyRelative("m_Visible");

            SerializedProperty style = data.FindPropertyRelative("m_Style");

            m_Shape = style.FindPropertyRelative("shape");
            m_Color = style.FindPropertyRelative("color");
            m_Size = style.FindPropertyRelative("size");
            m_Position = style.FindPropertyRelative("position");
            m_Rotation = style.FindPropertyRelative("rotation");

            m_ExpandOverlay = EditorPrefs.GetBool(s_ExpandOverlayPrefKey, true);
        }

        private IRigEffectorHolder FetchRigEffectorHolder(Transform transform)
        {
            var rigBuilder = EditorHelper.GetClosestComponent<RigBuilder>(transform);
            var rig = EditorHelper.GetClosestComponent<Rig>(transform, (rigBuilder != null) ? rigBuilder.transform : null);

            if (rigBuilder.ContainsEffector(transform))
            {
                return rigBuilder;
            }
            else if (rig.ContainsEffector(transform))
            {
                return rig;
            }

            return null;
        }

        public void OnSceneGUIOverlay()
        {
            GameObject targetGameObject = null;

            m_SerializedObject.Update();

            if (!m_SerializedObject.isEditingMultipleObjects)
            {
                RigEffector rigEffector = m_SerializedObject.targetObject as RigEffector;
                if (rigEffector.transform != null)
                {
                    targetGameObject = rigEffector.transform.gameObject;
                }
            }

            GUILayout.BeginHorizontal(GUILayout.Width(210.0f));

            EditorGUI.BeginChangeCheck();
            m_ExpandOverlay = EditorGUILayout.Toggle(m_ExpandOverlay, EditorStyles.foldout, GUILayout.Width(12));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(s_ExpandOverlayPrefKey, m_ExpandOverlay);
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.PropertyField(m_Visible, GUIContent.none, GUILayout.Width(17));

            GUILayout.Label((targetGameObject != null) ? targetGameObject.name : "(Multiple objects)");

            if (GUILayout.Button(GUIContent.none, "OL Minus", GUILayout.Width(17)))
            {
                UnityEngine.Object[] targetObjects = m_SerializedObject.targetObjects;
                foreach(var targetObject in targetObjects)
                {
                    var effector = targetObject as IRigEffector;
                    Transform transform = effector.transform;

                    IRigEffectorHolder holder = FetchRigEffectorHolder(transform);
                    if (holder != null)
                    {
                        var holderObject = holder as UnityEngine.Object;

                        Undo.RecordObject(holderObject, "Remove Effector");

                        if (PrefabUtility.IsPartOfPrefabInstance(holderObject))
                            EditorUtility.SetDirty(holderObject);

                        holder.RemoveEffector(transform);
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndHorizontal();

            if (m_ExpandOverlay)
            {
                EditorGUILayout.LabelField(s_ShapeLabel);
                EditorGUILayout.PropertyField(m_Shape, GUIContent.none);

                Rect rect = GUILayoutUtility.GetRect(s_ColorLabel, EditorStyles.colorField);

                // Shenanigans to bypass color picker bug.
                var evt = Event.current;
                if (evt.type == EventType.MouseUp)
                {
                    if (rect.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = 0;
                    }
                }

                EditorGUI.BeginProperty(rect, s_ColorLabel, m_Color);
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUI.ColorField(rect, s_ColorLabel, m_Color.colorValue, false, true, false);
                if (EditorGUI.EndChangeCheck())
                {
                    m_Color.colorValue = newColor;
                }
                EditorGUI.EndProperty();

                EditorGUILayout.PropertyField(m_Size, s_SizeLabel);
                EditorGUILayout.PropertyField(m_Position, s_PositionLabel);
                EditorGUILayout.PropertyField(m_Rotation, s_RotationLabel);
            }

            if (m_SerializedObject.hasModifiedProperties)
            {
                UnityEngine.Object[] targetObjects = m_SerializedObject.targetObjects;
                foreach(var targetObject in targetObjects)
                {
                    var effector = targetObject as IRigEffector;
                    Transform transform = effector.transform;

                    IRigEffectorHolder holder = FetchRigEffectorHolder(transform);
                    if (holder != null)
                    {
                        var holderObject = holder as UnityEngine.Object;
                        Undo.RecordObject(holderObject, "Edit Effector");

                        if (PrefabUtility.IsPartOfPrefabInstance(holderObject))
                            EditorUtility.SetDirty(holderObject);
                    }
                }

                m_SerializedObject.ApplyModifiedProperties();
            }
        }
    }
}
