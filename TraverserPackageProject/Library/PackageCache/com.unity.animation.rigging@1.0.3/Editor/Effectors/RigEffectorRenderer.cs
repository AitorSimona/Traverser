using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

namespace UnityEditor.Animations.Rigging
{
    [InitializeOnLoad]
    static class RigEffectorRenderer
    {
        static GUIContent s_OverlayTitle = new GUIContent("Animation Rigging");

        static List<RigBuilder> s_RigBuilders = new List<RigBuilder>();
        static Dictionary<RigEffectorData, RigEffector> s_Effectors = new Dictionary<RigEffectorData, RigEffector>();

        static Transform[] s_ActiveSelection = null;
        static List<RigEffector> s_ActiveEffectors = null;
        static IRigEffectorOverlay s_ActiveOverlay = null;

        static RigEffectorRenderer()
        {
            RigBuilder.onAddRigBuilder += OnAddRigBuilder;
            RigBuilder.onRemoveRigBuilder += OnRemoveRigBuilder;

            SceneView.duringSceneGui += OnSceneGUI;
        }

        static void FetchOrCreateEffectors(IRigEffectorHolder holder)
        {
            foreach(var effectorData in holder.effectors)
            {
                if (s_Effectors.ContainsKey(effectorData))
                {
                    s_ActiveEffectors.Add(s_Effectors[effectorData]);
                }
                else
                {
                    var newEffector = ScriptableObject.CreateInstance<RigEffector>();
                    newEffector.Initialize(effectorData);

                    s_Effectors.Add(effectorData, newEffector);
                    s_ActiveEffectors.Add(newEffector);
                }
            }
        }

        static void FetchOrCreateEffectors()
        {
            s_ActiveEffectors = new List<RigEffector>();

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            for (int i = 0; i < s_RigBuilders.Count; i++)
            {
                var rigBuilder = s_RigBuilders[i];

                if (rigBuilder == null)
                    continue;

                if (prefabStage != null)
                {
                    StageHandle stageHandle = prefabStage.stageHandle;
                    if (stageHandle.IsValid() && !stageHandle.Contains(rigBuilder.gameObject))
                        continue;
                }

                FetchOrCreateEffectors(rigBuilder);

                var rigs = rigBuilder.GetComponentsInChildren<Rig>();
                if (rigs != null)
                {
                    foreach(var rig in rigs)
                    {
                        FetchOrCreateEffectors(rig);
                    }
                }
            }
        }

        static IRigEffectorOverlay FetchOrCreateEffectorOverlay()
        {
            Transform[] transforms = Selection.GetTransforms(SelectionMode.ExcludePrefab | SelectionMode.Editable);
            var inspectedEffectors = new List<ScriptableObject>();

            for (int i = 0; i < s_ActiveEffectors.Count; ++i)
            {
                var effector = s_ActiveEffectors[i];
                if (effector != null && effector.transform != null)
                {
                    if (Selection.Contains(effector.transform) || Selection.Contains(effector.transform.gameObject))
                    {
                        inspectedEffectors.Add(s_ActiveEffectors[i]);
                    }
                }
            }

            if (inspectedEffectors.Count > 0)
            {
                var overlay = new RigEffectorOverlay();
                overlay.Initialize(new SerializedObject(inspectedEffectors.ToArray()));

                s_ActiveOverlay = overlay;
            }
            else
            {
                RigEffectorWizard wizard = null;

                foreach(var transform in transforms)
                {
                    RigBuilder rigBuilder = EditorHelper.GetClosestComponent<RigBuilder>(transform);
                    Rig rig = EditorHelper.GetClosestComponent<Rig>(transform, (rigBuilder != null) ? rigBuilder.transform : null);
                    IRigEffectorHolder holder = (rig != null) ? (IRigEffectorHolder)rig : (IRigEffectorHolder)rigBuilder;

                    if (holder == null)
                        continue;

                    if (wizard == null)
                        wizard = new RigEffectorWizard();

                    wizard.Add(holder, transform);
                }

                if (wizard != null)
                {
                    s_ActiveOverlay = wizard;
                }
                else
                {
                    s_ActiveOverlay = null;
                }
            }

            s_ActiveSelection = transforms;

            return s_ActiveOverlay;
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            // Fetch effectors and overlay once in Layout before processing events.
            if (Event.current.type == EventType.Layout)
            {
                FetchOrCreateEffectors();
                FetchOrCreateEffectorOverlay();
            }

            // Process effector events.
            if (s_ActiveEffectors != null)
            {
                for (int i = 0; i < s_ActiveEffectors.Count; ++i)
                {
                    var effector = s_ActiveEffectors[i];
                    if (effector == null)
                        continue;

                    effector.OnSceneGUI();
                }
            }

            // Process overlay events.
            if (s_ActiveOverlay != null)
            {
                SceneViewOverlay.Begin(sceneView);
                SceneViewOverlay.Window(s_OverlayTitle, SceneViewGUICallback, 1200);
                SceneViewOverlay.End();
            }
        }

        static void OnAddRigBuilder(RigBuilder rigBuilder)
        {
            s_RigBuilders.Add(rigBuilder);
        }

        static void OnRemoveRigBuilder(RigBuilder rigBuilder)
        {
            s_RigBuilders.Remove(rigBuilder);
            s_Effectors.Clear();
        }

        private static void SceneViewGUICallback(UnityEngine.Object target, SceneView sceneView)
        {
            if (s_ActiveOverlay != null)
                s_ActiveOverlay.OnSceneGUIOverlay();
        }
    }
}
