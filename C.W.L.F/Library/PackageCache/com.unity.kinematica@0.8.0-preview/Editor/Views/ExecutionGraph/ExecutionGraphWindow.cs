using System;
using Unity.Kinematica.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEngine.Assertions;

using Unity.SnapshotDebugger;

using PreLateUpdate = UnityEngine.PlayerLoop.PreLateUpdate;
using UnityEngine.Rendering;

namespace Unity.Kinematica.Editor
{
    internal class ExecutionGraphWindow : EditorWindow
    {
        [NonSerialized]
        IMotionSynthesizerProvider m_SynthesizerProvider;

        readonly string styleSheet = "NodeGraphWindow.uss";
        readonly string toolbarStyleSheet = "NodeGraphToolbar.uss";

        [SerializeField]
        bool m_ExitingPlayMode;

        private ExecutionGraphWindow()
        {
        }

        [MenuItem("Window/Analysis/Kinematica Execution Graph")]
        public static void ShowWindow()
        {
            GetWindow<ExecutionGraphWindow>("Kinematica Execution Graph");
        }

        void OnEnable()
        {
            m_SynthesizerProvider = null;

            LoadTemplate();

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            //For regular RP
            Camera.onPostRender -= OnRender;
            Camera.onPostRender += OnRender;

            //For HDRP
            RenderPipelineManager.endCameraRendering -= OnRenderRP;
            RenderPipelineManager.endCameraRendering += OnRenderRP;

            UpdateSystem.Listen<PreLateUpdate>(OnPreLateUpdate);

            m_ExitingPlayMode = false;
        }

        void OnDisable()
        {
            m_SynthesizerProvider = null;

            UpdateSystem.Ignore<PreLateUpdate>(OnPreLateUpdate);

            Camera.onPostRender -= OnRender;
            RenderPipelineManager.endCameraRendering -= OnRenderRP;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void LoadTemplate()
        {
            UIElementsUtils.ApplyStyleSheet(styleSheet, rootVisualElement);
            UIElementsUtils.ApplyStyleSheet(toolbarStyleSheet, rootVisualElement);

            UIElementsUtils.CloneTemplateInto("NodeGraphWindow.uxml", rootVisualElement);

            var breadcrumb = rootVisualElement.Q<Breadcrumb>("breadcrumb");
            breadcrumb.Clear();
            breadcrumb.PushItem("Root");

            var content = rootVisualElement.Q<VisualElement>("content");
            var graphNodeView = new GraphNodeView(this);
            graphNodeView.name = "graphNodeView";
            content.Add(graphNodeView);

            DisplayMessages();
        }

        bool DisplayMessages()
        {
            var playModeMessage = rootVisualElement.Q("playModeMessage");
            var selectionMessage = rootVisualElement.Q("debuggerMessage");
            var graphNodeView = rootVisualElement.Q("graphNodeView");

            if (!EditorApplication.isPlaying)
            {
                playModeMessage.style.display = DisplayStyle.Flex;
                selectionMessage.style.display = DisplayStyle.None;
                graphNodeView.style.display = DisplayStyle.None;
                return true;
            }

            if (Debugger.instance.IsState(Debugger.State.Inactive))
            {
                playModeMessage.style.display = DisplayStyle.None;
                selectionMessage.style.display = DisplayStyle.Flex;
                graphNodeView.style.display = DisplayStyle.None;
                return true;
            }

            playModeMessage.style.display = DisplayStyle.None;
            selectionMessage.style.display = DisplayStyle.None;
            graphNodeView.style.display = DisplayStyle.Flex;

            return true;
        }

        void OnPreLateUpdate()
        {
            if (EditorApplication.isPlaying && (m_SynthesizerProvider != null))
            {
                var graphNodeView =
                    rootVisualElement.Q<GraphNodeView>("graphNodeView");

                var synthesizer = m_SynthesizerProvider.Synthesizer;

                foreach (var selectable in graphNodeView.selection)
                {
                    var graphNode = selectable as GraphNode;

                    if (graphNode != null)
                    {
                        graphNode.OnPreLateUpdate(ref synthesizer.Ref);
                    }
                }
            }
        }

        void OnRenderRP(ScriptableRenderContext context, Camera camera)
        {
            OnRender(camera);
        }

        void OnRender(Camera camera)
        {
            if (EditorApplication.isPlaying && m_SynthesizerProvider != null && !m_ExitingPlayMode)
            {
                var graphNodeView =
                    rootVisualElement.Q<GraphNodeView>("graphNodeView");

                var synthesizer = m_SynthesizerProvider.Synthesizer;

                DebugDraw.Begin(camera);

                DebugDraw.SetDepthRendering(false);

                foreach (var selectable in graphNodeView.selection)
                {
                    var graphNode = selectable as GraphNode;

                    if (graphNode != null)
                    {
                        graphNode.OnSelected(ref synthesizer.Ref);
                    }
                }

                DebugDraw.End();
            }
        }

        void OnSelectionChange()
        {
            IMotionSynthesizerProvider synthProvider = null;

            // Try get provider from frame debugger selection
            if (synthProvider == null)
            {
                foreach (SelectedFrameDebugProvider selectedProvider in Debugger.frameDebugger.Selection)
                {
                    synthProvider = selectedProvider.providerInfo.provider as IMotionSynthesizerProvider;
                    break;
                }
            }

            // Get first provider we find in scene
            if (synthProvider == null)
            {
                synthProvider = FindMotionSynthesizerObject();
            }

            // If we found a provider, we sync the frame debugger selection with it
            if (synthProvider != null)
            {
                IFrameDebugProvider frameDebugProvider = synthProvider as IFrameDebugProvider;
                if (frameDebugProvider != null)
                {
                    if (Debugger.frameDebugger.NumSelected != 1 || Debugger.frameDebugger.GetSelected(0).providerInfo.provider != frameDebugProvider)
                    {
                        Debugger.frameDebugger.ClearSelection();
                        if (!Debugger.frameDebugger.TrySelect(frameDebugProvider))
                        {
                            synthProvider = null;
                        }
                    }
                }
            }

            if (m_SynthesizerProvider != synthProvider)
            {
                if (synthProvider != null && !(synthProvider is IFrameDebugProvider))
                {
                    Debug.Log($"{synthProvider.GetType().FullName} doesn't implement IFrameDebugProvider, nodes won't be selectable in the Kinematica Execution Graph window");
                }

                m_SynthesizerProvider = synthProvider;
            }

            DisplayMessages();
        }

        void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                m_ExitingPlayMode = true;
            }

            OnSelectionChange();
        }

        IMotionSynthesizerProvider FindMotionSynthesizerObject()
        {
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<IMotionSynthesizerProvider>();
            foreach (var type in types)
            {
                if (FindObjectOfType(type) is IMotionSynthesizerProvider candidateProvider)
                {
                    return candidateProvider;
                }
            }

            return null;
        }

        void Update()
        {
            if (EditorApplication.isPlaying)
            {
                OnSelectionChange();

                Repaint();
            }
        }

        void OnInspectorUpdate()
        {
            if (!EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        void OnGUI()
        {
            var graphNodeView =
                rootVisualElement.Q<GraphNodeView>("graphNodeView");

            if (EditorApplication.isPlaying && (m_SynthesizerProvider != null && m_SynthesizerProvider.IsSynthesizerInitialized))
            {
                graphNodeView.Update(m_SynthesizerProvider);
            }
            else
            {
                graphNodeView.Update(null);
            }
        }
    }
}
