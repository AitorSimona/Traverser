using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Unity.Kinematica.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MovementController))]
    public class MovementControllerEditor : UnityEditor.Editor
    {
        private MovementController Target
        {
            get { return target as MovementController; }
        }

        private void OnEnable()
        {
            RetrieveLayers();
        }

        public override void OnInspectorGUI()
        {
            if (InspectorGUI())
            {
                EditorUtility.SetDirty(Target);

                if (!EditorApplication.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
            }
        }

        private bool InspectorGUI()
        {
            bool dirty = false;

            bool enabled = EditorGUILayout.Toggle(new GUIContent("Is Enabled",
                    "Determines if the controller is updating and controlling movement and rotation."),
                        Target.IsEnabled);
            if (enabled != Target.IsEnabled)
            {
                dirty = true;
                Target.IsEnabled = enabled;
            }

            //
            // Character properties
            //

            {
                EditorGUILayout.LabelField("Character Properties", EditorStyles.boldLabel, GUILayout.Height(16.0f));

                EditorGUILayout.BeginVertical();

                FloatField("Mass", "Mass of the character.", Target.mass, (value) =>
                {
                    dirty = true;
                    Target.mass = value;
                });

                EditorGUILayout.EndVertical();

                GUILayout.Space(5);
            }

            //
            // Gravity
            //

            EditorGUILayout.LabelField("Gravity", EditorStyles.boldLabel, GUILayout.Height(16.0f));

            EditorGUILayout.BeginVertical();

            {
                EditorGUILayout.BeginHorizontal();

                bool gravityEnabled = EditorGUILayout.Toggle(new GUIContent("Gravity Enabled",
                    "Determines whether or not gravity is enabled."),
                        Target.gravityEnabled);

                if (gravityEnabled != Target.gravityEnabled)
                {
                    dirty = true;
                    Target.gravityEnabled = gravityEnabled;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            //
            // Grounding
            //

            {
                EditorGUILayout.LabelField("Grounding", EditorStyles.boldLabel, GUILayout.Height(16.0f));

                EditorGUILayout.BeginVertical();

                FloatField("Ground Tolerance", "Distance tolerance in meters within which the character is considered to be grounded.",
                    Target.groundTolerance, (value) =>
                    {
                        dirty = true;
                        Target.groundTolerance = value;
                    });

                FloatField("Ground Support", "Area of support, i.e. a circle around the point of support specified in meters.",
                    Target.groundSupport, (value) =>
                        {
                            dirty = true;
                            Target.groundSupport = value;
                        });

                {
                    GUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel(
                        new GUIContent("Ground Probe", "Ground probing offset and length specified in meters."));

                    FloatField("Ground Probe Offset", Target.groundProbeOffset, (value) =>
                    {
                        dirty = true;
                        Target.groundProbeOffset = value;
                    });

                    FloatField("Ground Probe Length", Target.groundProbeLength, (value) =>
                    {
                        dirty = true;
                        Target.groundProbeLength = value;
                    });

                    GUILayout.EndHorizontal();
                }

                {
                    GUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel(
                        new GUIContent("Ground Snap", "Determines whether or not ground snapping is enabled and the snap distance."));

                    BooleanField("Ground snap", Target.groundSnap, (value) =>
                    {
                        dirty = true;
                        Target.groundSnap = value;
                    });

                    if(Target.groundSnap)
                    {
                        FloatField("Snap distance", Target.groundSnapDistance, (value) =>
                        {
                            dirty = true;
                            Target.groundSnapDistance = value;
                        });
                    }

                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();

                GUILayout.Space(5);
            }

            //
            // Collisions
            //

            {
                EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel, GUILayout.Height(16.0f));

                EditorGUILayout.BeginVertical();

                {
                    GUILayout.BeginHorizontal();

                    EditorGUILayout.PrefixLabel(
                        new GUIContent("Collision Enabled", "Determines whether or not collisions are enabled."));

                    BooleanField("Collision Enabled", Target.collisionEnabled, (value) =>
                    {
                        dirty = true;
                        Target.collisionEnabled = value;
                    });

                    GUILayout.EndHorizontal();
                }

                LayerMaskField("Collision Layers",
                    "Layers that collisions will be checked against",
                        Target.layerMask, (value) =>
                        {
                            dirty = true;
                            Target.layerMask = value;
                        });

                EditorGUILayout.EndVertical();

                GUILayout.Space(5);
            }

            return dirty;
        }

        string[] layerNames = null;
        int[] layerValues = null;

        void RetrieveLayers()
        {
            List<string> layerNames = new List<string>();
            List<int> layerValues = new List<int>();

            for (int i = 0; i < 32; i++)
            {
                try
                {
                    var name = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(name))
                    {
                        layerNames.Add(name);
                        layerValues.Add(1 << i);
                    }
                }
                catch
                {
                }
            }

            this.layerNames = layerNames.ToArray();
            this.layerValues = layerValues.ToArray();
        }

        public bool LayerMaskField(string title, string tooltip, int layerMask, Action<int> setter)
        {
            int layerMaskValue = 0;

            if (layerNames == null)
            {
                RetrieveLayers();
            }

            for (int i = 0; i < layerNames.Length; i++)
            {
                if (layerValues[i] != 0)
                {
                    if ((layerMask & layerValues[i]) == layerValues[i])
                    {
                        layerMaskValue |= 1 << i;
                    }
                }
                else if (layerMask == 0)
                {
                    layerMaskValue |= 1 << i;
                }
            }

            EditorGUI.BeginChangeCheck();

            var result = EditorGUILayout.MaskField(
                new GUIContent(title, tooltip),
                    layerMaskValue, layerNames);

            if (EditorGUI.EndChangeCheck())
            {
                int changedlayerMask = layerMaskValue ^ result;

                for (int i = 0; i < layerValues.Length; i++)
                {
                    if ((changedlayerMask & (1 << i)) != 0)
                    {
                        if ((result & (1 << i)) != 0)
                        {
                            if (layerValues[i] == 0)
                            {
                                layerMask = 0;
                                break;
                            }
                            else
                            {
                                layerMask |= layerValues[i];
                            }
                        }
                        else
                        {
                            layerMask &= ~layerValues[i];
                        }
                    }
                }

                setter(layerMask);

                Undo.RecordObject(Target, "Set " + title);

                return true;
            }

            return false;
        }

        bool FloatField(string title, string tooltip, float value, Action<float> setter)
        {
            EditorGUI.BeginChangeCheck();

            var result = EditorGUILayout.FloatField(new GUIContent(title, tooltip), value);

            if (EditorGUI.EndChangeCheck())
            {
                setter(result);

                Undo.RecordObject(Target, "Set " + title);

                return true;
            }

            return false;
        }

        bool BooleanField(string title, bool value, Action<bool> setter)
        {
            EditorGUI.BeginChangeCheck();

            var result = EditorGUILayout.Toggle(value, GUILayout.MaxWidth(15));

            if (EditorGUI.EndChangeCheck())
            {
                setter(result);

                Undo.RecordObject(Target, "Set " + title);

                return true;
            }

            return false;
        }

        bool FloatField(string title, float value, Action<float> setter)
        {
            EditorGUI.BeginChangeCheck();

            var result = EditorGUILayout.FloatField(value);

            if (EditorGUI.EndChangeCheck())
            {
                setter(result);

                Undo.RecordObject(Target, "Set " + title);

                return true;
            }

            return false;
        }
    }
}
