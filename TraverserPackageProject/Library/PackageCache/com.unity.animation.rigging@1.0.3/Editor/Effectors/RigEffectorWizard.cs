using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    class RigEffectorWizard : IRigEffectorOverlay
    {
        private static GUIContent s_CreateEffectorLabel = new GUIContent("Create Effector");

        private struct HolderTransformPair
        {
            public IRigEffectorHolder holder;
            public Transform transform;
        }

        private List<HolderTransformPair> m_Transforms = new List<HolderTransformPair>();

        public void Add(IRigEffectorHolder holder, Transform transform)
        {
            m_Transforms.Add(new HolderTransformPair() { holder = holder, transform = transform });
        }

        public void OnSceneGUIOverlay()
        {
            string labelName = "(no selection)";
            if (m_Transforms.Count > 1)
            {
                labelName = "(Multiple objects)";
            }
            else if (m_Transforms.Count > 0)
            {
                if (m_Transforms[0].transform.gameObject != null)
                {
                    labelName = m_Transforms[0].transform.gameObject.name;
                }
            }

            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(210.0f));

            GUILayout.Label(labelName);

            if (GUILayout.Button(GUIContent.none, "OL Plus", GUILayout.Width(17)))
            {
                foreach (var pair in m_Transforms)
                {
                    var targetObject = pair.holder as UnityEngine.Object;

                    Undo.RecordObject(targetObject, "Add Effector");

                    if (PrefabUtility.IsPartOfPrefabInstance(targetObject))
                        EditorUtility.SetDirty(targetObject);

                    pair.holder.AddEffector(pair.transform, RigEffector.defaultStyle);
                }
            }

            GUILayout.EndHorizontal();
        }
    }
}
