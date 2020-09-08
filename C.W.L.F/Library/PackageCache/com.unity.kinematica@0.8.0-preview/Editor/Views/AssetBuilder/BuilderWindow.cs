using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Kinematica.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;
using System.Collections;

namespace Unity.Kinematica.Editor
{
    partial class BuilderWindow : EditorWindow
    {
        static class Styles
        {
            //TODO - this style value gets repeated frequently in trunk, there might be a more global way of setting/getting it
            public static GUIStyle lockButton = "IN LockButton";
        }

        [SerializeField]
        internal bool isLocked;

        enum LayoutMode
        {
            CreateAsset,
            ConfigureAndBuildAsset
        }

        // Templates
        const string k_MainLayout = "KinematicaWindow.uxml";

        //Styles
        internal const string k_Stylesheet = "KinematicaWindow.uss";
        const string k_ToolbarStyle = "Toolbar.uss";
        const string k_AnimationLibraryStyle = "AnimationClipLibrary.uss";

        const string k_WindowTitle = "Kinematica Asset Builder";

        VisualElement m_Toolbar;
        VisualElement m_MainLayout;

        VisualElement m_ClipAndSettingsInput;

        VisualElement m_GutterToggleMenu;
        VisualElement m_GutterLabels;

        Button m_EditAssetButton;

        VisualElement m_AssetDirtyWarning;
        ProgressBar m_ProgressBar;
        Button m_BuildButton;
        Button m_CancelBuildButton;

        AnimationClipListView m_AnimationLibraryListView;

        ToolbarToggle m_PreviewToggle;

        [SerializeField]
        GameObject m_PreviewTarget;

        [SerializeField]
        List<int> m_PreviousSelection;

        Timeline m_Timeline;
        VisualElement m_AssetCreateLayout;
        VisualElement m_MainInputLayout;

        bool m_UpdateTitle = false;

        BuildProcess m_BuildProcess = null;

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            var kinematicaAsset = EditorUtility.InstanceIDToObject(instanceID) as Asset;
            if (kinematicaAsset)
            {
                BuilderWindow window = GetWindow<BuilderWindow>();
                window.Asset = kinematicaAsset;
                return true; //catch open file
            }

            return false; // let unity open the file
        }

        [MenuItem(("Window/Animation/Kinematica Asset Builder"))]
        public static void ShowWindow()
        {
            var wnd = GetWindow(typeof(BuilderWindow)) as BuilderWindow;
            wnd.UpdateTitle();
        }

        class AssetChangedProcessor : UnityEditor.AssetModificationProcessor
        {
            internal static Asset k_ActiveAsset;
            internal static BuilderWindow k_Window;

            static string[] OnWillSaveAssets(string[] paths)
            {
                //TODO - remove this before shipping, we won't tell users when changes are not saved
                if (k_Window != null && k_ActiveAsset != null)
                {
                    if (paths.Contains(AssetDatabase.GetAssetPath(k_ActiveAsset)))
                    {
                        k_Window.m_UpdateTitle = true;
                    }
                }

                return paths;
            }

            static AssetDeleteResult OnWillDeleteAsset(string s, RemoveAssetOptions options)
            {
                if (k_ActiveAsset != null && k_Window != null)
                {
                    if (s.Equals(AssetDatabase.GetAssetPath(k_ActiveAsset)))
                    {
                        k_Window.Asset = null;
                        k_Window.TargetCurrentSelection();
                    }
                }

                return AssetDeleteResult.DidNotDelete;
            }
        }

        //TODO - remove functionality
        void UpdateTitle()
        {
            bool dirty = Asset != null && EditorUtility.IsDirty(Asset);
            titleContent = new GUIContent(k_WindowTitle + (dirty ? " *" : string.Empty));
        }

        protected virtual void OnEnable()
        {
            LoadTemplate();
            TargetCurrentSelection();

            Selection.selectionChanged += OnSelectionChanged;

            AssetChangedProcessor.k_Window = this;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        void OnBeforeAssemblyReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            m_PreviousAsset = Asset;
            m_PreviousSelection = new List<int>();

            foreach (var selected in m_AnimationLibraryListView.m_ClipSelection)
            {
                int index = Asset.AnimationLibrary.IndexOf(selected);
                if (index >= 0)
                {
                    m_PreviousSelection.Add(index);
                }
            }

            Asset = null;
        }

        void OnAfterAssemblyReload()
        {
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;

            if (Asset == null && m_PreviousAsset != null)
            {
                Asset = m_PreviousAsset;
                ChangeLayoutMode(LayoutMode.ConfigureAndBuildAsset);

                if (m_PreviousSelection != null && m_PreviousSelection.Any())
                {
                    rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedAfterAssemblyReload);
                }

                m_PreviousAsset = null;
            }
            else
            {
                m_PreviousSelection = null;
            }
        }

        void OnGeometryChangedAfterAssemblyReload(GeometryChangedEvent evt)
        {
            // we need to cache the reselect flag here as it will change when we re-select items in the list view
            bool reselect = m_ReselectAsset;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedAfterAssemblyReload);
            m_AnimationLibraryListView.SelectItems(m_PreviousSelection);
            m_PreviousSelection = null;
            if (reselect)
            {
                Selection.activeObject = Asset;
            }
        }

        void OnDisable()
        {
            AssetChangedProcessor.k_Window = null;

            // In case we en up closing the window before the layout has loaded
            if (m_MainLayout == null)
            {
                return;
            }

            m_EditAssetButton.clickable.clicked -= EditAsset;
            m_BuildButton.clickable.clicked -= BuildAsset;
            m_CancelBuildButton.clickable.clicked -= CancelBuildAsset;

            var createButton = m_MainLayout.Q<Button>("createButton");
            createButton.clickable.clicked -= CreateButtonClicked;

            Selection.selectionChanged -= OnSelectionChanged;
            m_AnimationLibraryListView.onSelectionChanged -= OnLibrarySelectionChanged;

            if (m_Timeline != null)
            {
                m_PreviewTarget = m_Timeline.PreviewTarget;
                m_Timeline.Dispose();
                m_Timeline.PreviewTargetChanged -= OnTimelinePreviewTargetChanged;
                m_Timeline.GutterTrackAdded -= OnGutterTrackCreated;
                m_Timeline.ForceGutterTrackDisplay -= ForceGutterTrackDisplay;
                m_Timeline = null;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnDestroy()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Hide clip highlights
            foreach (var clip in m_AnimationLibraryListView.Children())
            {
                clip.ElementAt(k_ClipHighlight).style.visibility = Visibility.Hidden;
            }

            m_Timeline.PreviewTarget = m_PreviewTarget;
            bool enabled = state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode;
            if (!enabled)
            {
                CancelBuildAsset();
            }

            rootVisualElement.Q<PlayControls>().SetEnabled(enabled);
            SetAssetEditingEnabled(enabled);
        }

        void SetAssetEditingEnabled(bool enabled, bool showDirtyWarning = true)
        {
            m_Timeline.SetTimelineEditingEnabled(enabled);
            m_BuildButton.SetEnabled(enabled);
            ShowOrHideAssetDirtyWarning(!enabled && showDirtyWarning);
        }

        void Update()
        {
            if (m_UpdateTitle)
            {
                if (m_Asset != null)
                {
                    m_Asset.OnSave();
                }

                UpdateTitle();
                m_UpdateTitle = false;
            }

            PlayModeUpdate();
        }

        protected virtual void ShowButton(Rect r)
        {
            EditorGUI.BeginChangeCheck();
            //TODO - stole code from internal EditorGUIUtility....
            bool newLock = GUI.Toggle(r, isLocked, GUIContent.none, Styles.lockButton);

            if (EditorGUI.EndChangeCheck())
            {
                if (newLock != isLocked)
                {
                    isLocked = !isLocked;
                    if (!isLocked)
                    {
                        TargetCurrentSelection();
                    }
                }
            }
        }

        void LoadTemplate()
        {
            rootVisualElement.Clear();

            UIElementsUtils.ApplyStyleSheet(k_Stylesheet, rootVisualElement);
            UIElementsUtils.ApplyStyleSheet(Timeline.k_Stylesheet, rootVisualElement);
            UIElementsUtils.ApplyStyleSheet(k_ToolbarStyle, rootVisualElement);

            UIElementsUtils.CloneTemplateInto(k_MainLayout, rootVisualElement);

            VisualElement outerElement = rootVisualElement.Q<VisualElement>("kinematica");
            m_Toolbar = outerElement.Q<VisualElement>("toolbar");

            m_MainLayout = outerElement.Q<VisualElement>("windowContent");

            // Input for Build
            {
                m_MainInputLayout = outerElement.Q<VisualElement>("inputLayout");
                m_ClipAndSettingsInput = m_MainLayout.Q<VisualElement>("inputArea");

                m_GutterToggleMenu =  m_MainLayout.Q<VisualElement>("gutterToggleMenu");
                var selectorClick = new Clickable(OnGutterMenuClicked);
                m_GutterToggleMenu.AddManipulator(selectorClick);

                m_GutterLabels = m_MainLayout.Q<VisualElement>("gutterList");

                //Profile and Asset creation
                {
                    m_AssetCreateLayout = m_MainLayout.Q<VisualElement>(classes: "createLayout");

                    var createButton = m_AssetCreateLayout.Q<Button>("createButton");
                    createButton.clickable.clicked += CreateButtonClicked;
                    createButton.text = "Create";
                }

                m_EditAssetButton = rootVisualElement.Q<Button>("editAssetButton");
                m_EditAssetButton.clickable.clicked += EditAsset;

                m_AssetDirtyWarning = rootVisualElement.Q<VisualElement>("assetDirtyWarning");

                m_ProgressBar = rootVisualElement.Q<ProgressBar>("progressBar");
                HideProgressBar();

                m_BuildButton = rootVisualElement.Q<Button>("buildButton");
                m_BuildButton.clickable.clicked += BuildAsset;

                m_CancelBuildButton = rootVisualElement.Q<Button>("cancelBuildButton");
                m_CancelBuildButton.clickable.clicked += CancelBuildAsset;
                DisplayCancelBuildButton(false);

                var assetSelector = m_Toolbar.Q<ObjectField>("asset");
                assetSelector.objectType = typeof(Asset);
                assetSelector.RegisterValueChangedCallback(OnAssetSelectionChanged);

                m_AnimationLibraryListView = m_ClipAndSettingsInput.Q<AnimationClipListView>("animationLibrary");
                m_AnimationLibraryListView.m_Window = this;
                m_AnimationLibraryListView.selectionType = SelectionType.Multiple;
                m_AnimationLibraryListView.makeItem = MakeAnimationItem;
                m_AnimationLibraryListView.bindItem = BindAnimationItem;
                m_AnimationLibraryListView.itemHeight = 18;
                UIElementsUtils.ApplyStyleSheet(k_AnimationLibraryStyle, m_ClipAndSettingsInput.Q<VisualElement>("clipsArea"));

                m_Timeline = rootVisualElement.Q<Timeline>("timeline");
                m_Timeline.PreviewTargetChanged += OnTimelinePreviewTargetChanged;
                m_Timeline.GutterTrackAdded += OnGutterTrackCreated;
                m_Timeline.ForceGutterTrackDisplay += ForceGutterTrackDisplay;
                m_Timeline.LoadTemplate(rootVisualElement);
                m_AnimationLibraryListView.onSelectionChanged += OnLibrarySelectionChanged;

                m_PreviewToggle = rootVisualElement.Q<ToolbarToggle>("previewToggle");
                m_PreviewToggle.SetValueWithoutNotify(m_Timeline.PreviewEnabled);
                m_PreviewToggle.RegisterValueChangedCallback(evt => m_Timeline.PreviewEnabled = evt.newValue);
                m_Timeline.PreviewEnabledChangeEvent += enabled => m_PreviewToggle.SetValueWithoutNotify(enabled);
            }

            SetToolbarEnable(false);
        }

        void OnGutterMenuClicked()
        {
            var menu = new GenericMenu();
            foreach (GutterTrack gutter in m_GutterLabelLookup.Keys)
            {
                menu.AddItem(new GUIContent(gutter.name), gutter.style.display == DisplayStyle.Flex, () => { OnGutterTrackToggled(gutter); });
            }

            menu.DropDown(m_GutterToggleMenu.worldBound);
        }

        void OnTimelinePreviewTargetChanged(GameObject previewTarget)
        {
            m_PreviewTarget = previewTarget;
        }

        readonly Dictionary<GutterTrack, VisualElement> m_GutterLabelLookup = new Dictionary<GutterTrack, VisualElement>();

        void OnGutterTrackCreated(GutterTrack t)
        {
            VisualElement label = new Label { text = t.name };
            m_GutterLabelLookup[t] = label;
            m_GutterLabels.Add(label);

            if (t.style.display == DisplayStyle.None)
            {
                SetGutterTrackDisplay(t, DisplayStyle.None);
            }
        }

        void OnGutterTrackToggled(GutterTrack t)
        {
            var display = t.style.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None;
            SetGutterTrackDisplay(t, display);
        }

        void ForceGutterTrackDisplay(GutterTrack t)
        {
            SetGutterTrackDisplay(t, DisplayStyle.Flex);
        }

        void SetGutterTrackDisplay(GutterTrack t, DisplayStyle display)
        {
            if (m_GutterLabelLookup.TryGetValue(t, out VisualElement l))
            {
                l.style.display = display;
            }

            t.SetDisplay(display);
        }

        void OnLibrarySelectionChanged(List<object> selection)
        {
            OnLibrarySelectionChanged(selection.OfType<TaggedAnimationClip>().ToList());
        }

        void OnLibrarySelectionChanged(List<TaggedAnimationClip> selection)
        {
            if (!selection.Any())
            {
                m_Timeline.SetClip(null);
                Selection.activeObject = Asset;
            }
            else
            {
                if (selection.Count == 1)
                {
                    AnimationClipSelectionContainer.Clear();
                    m_Timeline.SetClip(selection.First());
                }
                else
                {
                    m_Timeline.SetClip(null);
                    AnimationClipSelectionContainer.SetSelection(selection);
                    Selection.activeObject = AnimationClipSelectionContainer;
                }
            }
        }

        TaggedAnimationClipSelectionContainer m_AnimationClipSelectionContainer;

        TaggedAnimationClipSelectionContainer AnimationClipSelectionContainer
        {
            get
            {
                if (m_AnimationClipSelectionContainer == null)
                {
                    m_AnimationClipSelectionContainer = CreateInstance<TaggedAnimationClipSelectionContainer>();
                }

                return m_AnimationClipSelectionContainer;
            }
        }

        void OnTagSelectionClicked(DropdownMenuAction a)
        {
            var tagType = a.userData as Type;
            foreach (var clip in m_AnimationLibraryListView.m_ClipSelection)
            {
                clip.AddTag(tagType);
            }
        }

        void OnAssetSelectionChanged(ChangeEvent<Object> e)
        {
            Asset = e.newValue as Asset;
        }

        void OnSelectionChanged()
        {
            if (isLocked)
            {
                //TODO - if the target object is deleted we need to prevent entering an invalid state.
                return;
            }

            //m_TargetGameObject = null;
            TargetCurrentSelection();
        }

        void CreateButtonClicked()
        {
            if (!CreateNewAsset())
            {
                return;
            }

            //TODO - MotionSynthesizer component has been moved to the project, we will revisit generating our own GameObject and adding a component later...
            /* if (m_TargetGameObject == null)
            {
                CreateGameObject();
            }
            */
        }

        void TargetCurrentSelection()
        {
            var asset = Selection.activeObject as Asset;
            if (asset != null)
            {
                Asset = asset;
                m_ReselectAsset = true;
            }
            else if (Selection.activeObject is TimelineSelectionContainer)
            {
                m_ReselectAsset = false;
                return;
            }

            if (Asset == null)
            {
                m_Timeline.Reset();
                ChangeLayoutMode(LayoutMode.CreateAsset);
            }
            else
            {
                ChangeLayoutMode(LayoutMode.ConfigureAndBuildAsset);
            }
        }

        void ChangeLayoutMode(LayoutMode mode)
        {
            switch (mode)
            {
                case LayoutMode.CreateAsset:
                    var label = m_AssetCreateLayout.Q<Label>("createLabel");
                    /*
                    if (m_TargetGameObject == null)
                    {
                        label.text = "Create a new Kinematica Asset.";
                        //label.text = "Create a new Kinematica Asset and GameObject with a Kinematica Component.";
                    }
                    else
                    */
                    {
                        //TODO - MotionSynthesizer is no longer a component, need to update this section
                        /*
                        var cmp = m_TargetGameObject.GetComponent<MotionSynthesizer>();
                        string labelText = string.Empty;
                        if (KinematicaLibrarySettings == null)
                        {
                            labelText = "Create a new Kinematica Asset";
                        }

                        if (cmp == null)
                        {
                            labelText += $" and component on \"{m_TargetGameObject.name}\"";
                        }

                        label.text = labelText;
                        */
                    }

                    m_AssetCreateLayout.style.display = DisplayStyle.Flex;
                    m_MainInputLayout.style.display = DisplayStyle.None;
                    break;
                case LayoutMode.ConfigureAndBuildAsset:
                    m_AssetCreateLayout.style.display = DisplayStyle.None;
                    m_MainInputLayout.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        [SerializeField]
        Asset m_PreviousAsset;
        [SerializeField]
        bool m_ReselectAsset;

        Asset m_Asset;

        public Asset Asset
        {
            get { return m_Asset; }
            set
            {
                if (m_Asset != value)
                {
                    if (m_Asset != null)
                    {
                        m_Asset.MarkedDirty -= UpdateTitle;
                        m_Asset.AssetWasDeserialized -= OnAssetDeserialized;
                        CancelBuildAsset();
                    }

                    m_Asset = value;
                    AssetChangedProcessor.k_ActiveAsset = m_Asset;

                    if (m_Asset != null)
                    {
                        m_Asset.MarkedDirty += UpdateTitle;
                        m_Asset.AssetWasDeserialized += OnAssetDeserialized;

                        m_AnimationLibraryListView.UpdateSource(m_Asset.AnimationLibrary);
                        ChangeLayoutMode(LayoutMode.ConfigureAndBuildAsset);
                    }
                    else
                    {
                        m_AnimationLibraryListView.UpdateSource(null);
                        m_Timeline.Reset();
                        ChangeLayoutMode(LayoutMode.CreateAsset);
                    }

                    SetToolbarEnable(m_Asset != null);

                    m_Toolbar.Q<ObjectField>("asset").value = m_Asset;

                    //Ensure Timeline is refreshed
                    m_Timeline.TargetAsset = null;
                    m_Timeline.TargetAsset = m_Asset;
                    UpdateTitle();
                }
            }
        }

        const int k_ClipHighlight = 0;
        const int k_ClipFieldIndex = 1;
        const int k_ClipWarningIndex = 2;

        // For custom display item
        void BindAnimationItem(VisualElement e, int i)
        {
            if (Asset == null)
            {
                return;
            }

            var taggedClips = Asset.AnimationLibrary;
            if (taggedClips.Count <= i)
            {
                return;
            }

            var taggedClip = taggedClips[i];

            //TODO - remove tooltip and reference to AnimationClip?
            var clipValid = taggedClip.Valid;

            (e.ElementAt(k_ClipFieldIndex) as Label).text = taggedClip.ClipName;
            e.ElementAt(k_ClipWarningIndex).style.display = clipValid ? DisplayStyle.None : DisplayStyle.Flex;
            if (clipValid)
            {
                e.tooltip = $"Duration {taggedClip.DurationInSeconds:F2}s/{Mathf.RoundToInt(taggedClip.DurationInSeconds * taggedClip.SampleRate )} frames";
            }
            else
            {
                e.tooltip = TaggedAnimationClip.k_MissingClipText;
            }

            e.userData = taggedClip;
        }

        VisualElement MakeAnimationItem()
        {
            var ve = new VisualElement();

            var highlight = new VisualElement();
            highlight.AddToClassList("animationClipHighlight");
            highlight.style.visibility = Visibility.Hidden;
            ve.Add(highlight);

            var clipLabel = new Label();
            clipLabel.AddToClassList("animationClipLabel");
            clipLabel.text = "Anim";
            ve.Add(clipLabel);

            var warningIcon = new Image();
            warningIcon.AddToClassList("warningImage");
            warningIcon.tooltip = TaggedAnimationClip.k_MissingClipText;
            warningIcon.style.display = DisplayStyle.Flex;
            ve.Add(warningIcon);

            ve.style.flexDirection = FlexDirection.Row;
            ve.style.justifyContent = Justify.SpaceBetween;

            ContextualMenuManipulator itemContext = new ContextualMenuManipulator(evt => BuildAnimationClipItemMenu(evt, ve));
            ve.AddManipulator(itemContext);

            Clickable doubleClick = new Clickable(() => OnAnimationLibraryItemDoubleClick(ve));
            doubleClick.activators.Clear();
            doubleClick.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2});
            ve.AddManipulator(doubleClick);

            return ve;
        }

        void OnAnimationLibraryItemDoubleClick(VisualElement ve)
        {
            if (ve.userData is TaggedAnimationClip clip)
            {
                EditorGUIUtility.PingObject(clip.GetOrLoadClipSync());
            }
        }

        void BuildAnimationClipItemMenu(ContextualMenuPopulateEvent evt, VisualElement ve)
        {
            var clip = ve.userData as TaggedAnimationClip;
            if (clip == null)
            {
                evt.menu.AppendSeparator("Missing Clip");
                return;
            }

            if (m_AnimationLibraryListView.m_ClipSelection.Count > 0)
            {
                if (!m_AnimationLibraryListView.m_ClipSelection.Contains(clip))
                {
                    // We can't change the selection of the list from inside a context menu event
                    return;
                }

                evt.menu.AppendAction("Delete Selected Clip(s)", action => m_AnimationLibraryListView.DeleteSelection(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
                foreach (var tagType in TagAttribute.GetVisibleTypesInInspector())
                {
                    evt.menu.AppendAction($"Tag Selected Clip(s)/{TagAttribute.GetDescription(tagType)}", OnTagSelectionClicked, DropdownMenuAction.AlwaysEnabled, tagType);
                }

                evt.menu.AppendAction("Set Boundary Clips", action => BoundaryClipWindow.ShowWindow(m_Asset, m_AnimationLibraryListView.m_ClipSelection));

                evt.menu.AppendAction("Experimental - Generate Procedural Annotations", action => m_AnimationLibraryListView.GenerateProceduralAnnotations(), m_AnimationLibraryListView.CanGenerateProceduralAnnotations());
            }
            else
            {
                evt.menu.AppendAction("Delete Clip", action =>
                {
                    m_Asset.RemoveClips(new[] { clip });
                    m_AnimationLibraryListView.Refresh();
                }, DropdownMenuAction.AlwaysEnabled);

                evt.menu.AppendSeparator();
                foreach (var tagType in TagAttribute.GetVisibleTypesInInspector())
                {
                    evt.menu.AppendAction($"Tag Clip/{TagAttribute.GetDescription(tagType)}",
                        action => clip.AddTag(tagType),
                        DropdownMenuAction.AlwaysEnabled, tagType);
                }
            }
        }

        public const string k_KinematicaBinaryExtension = "asset";

        bool CreateNewAsset()
        {
            string assetPathAndName = EditorUtility.SaveFilePanelInProject("Choose Kinematica Asset filename",
                string.Empty, k_KinematicaBinaryExtension, string.Empty,
                ScriptableObjectUtility.GetSelectedPathOrFallback());

            if (!string.IsNullOrEmpty(assetPathAndName))
            {
                assetPathAndName = Path.ChangeExtension(assetPathAndName, ".asset");

                var asset = CreateInstance<Asset>();

                AssetDatabase.CreateAsset(asset, assetPathAndName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.FocusProjectWindow();

                Asset = asset;

                return true;
            }

            return false;
        }

        /*
        void CreateGameObject()
        {
            m_TargetGameObject = new GameObject();
            AddRequiredComponents(m_TargetGameObject);
            SelectGameObject(m_TargetGameObject);
        }
        */

        void SetToolbarEnable(bool enable)
        {
            bool playing = EditorApplication.isPlaying;
            m_EditAssetButton.SetEnabled(enable);
            m_BuildButton.SetEnabled(enable && !playing);
            m_CancelBuildButton.SetEnabled(enable && !playing);
            m_PreviewToggle.SetEnabled(enable);
            rootVisualElement.Q<PreviewSelector>().SetEnabled(enable);
            rootVisualElement.Q<PlayControls>().SetEnabled(enable);
            rootVisualElement.Q<Button>(classes: "viewMode").SetEnabled(enable);

            ShowOrHideAssetDirtyWarning(playing);
        }

        void DisplayCancelBuildButton(bool display)
        {
            m_BuildButton.style.display = display ? DisplayStyle.None : DisplayStyle.Flex;
            m_CancelBuildButton.style.display = display ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ShowOrHideAssetDirtyWarning(bool isOrEnteringPlayMode)
        {
            if (isOrEnteringPlayMode && Asset != null && !Asset.BinaryUpToDate)
            {
                m_AssetDirtyWarning.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_AssetDirtyWarning.style.display = DisplayStyle.None;
            }
        }

        void EditAsset()
        {
            Debug.Assert(Asset != null);
            Selection.activeObject = Asset;
        }

        void BuildAsset()
        {
            Debug.Assert(Asset != null);

            if (!Unity.Burst.BurstCompiler.Options.EnableBurstCompilation)
            {
                if (!EditorUtility.DisplayDialog("Kinematica Asset Build Warning", "Burst compilation is currently disabled, it is advised to enable it from menu " +
                    "(Burst -> Enable Compilation) otherwise building Kinematica asset can be extremely slow.", "Build anyway", "Cancel build"))
                {
                    return;
                }
            }

            CancelBuildAsset();

            m_BuildProcess = Asset.BuildAsync();
            if (m_BuildProcess != null)
            {
                DisplayCancelBuildButton(true);

                m_BuildProcess.Builder.progressFeedback += DisplayProgressBar;
                EditorApplication.update += UpdateBuildProcess;
                SetAssetEditingEnabled(false, showDirtyWarning: false);
            }
        }

        void CancelBuildAsset()
        {
            m_BuildProcess?.Cancel();
            HideProgressBar();
            DisplayCancelBuildButton(false);
            SetAssetEditingEnabled(true);
        }

        /*
        void AddRequiredComponents(GameObject obj)
        {
            if (KinematicaLibrarySettings == null)
            {
                return;
            }

            //TODO - MotionSynthesizer is no longer a component, need to update this section
            /*
            var synthesizer = obj.GetComponent<MotionSynthesizer>();
            if (synthesizer == null)
            {
                synthesizer = obj.AddComponent<MotionSynthesizer>();
            }

            synthesizer.kinematicaAsset = KinematicaLibrarySettings;
        }
            */

        void OnAssetDeserialized(Asset asset)
        {
            m_AnimationLibraryListView.MarkClipListsForUpdate();

            if (asset != null)
            {
                EditorApplication.delayCall += () => { m_Timeline?.OnAssetModified(); };
            }
        }

        void HideProgressBar()
        {
            m_ProgressBar.style.display = DisplayStyle.None;
        }

        void DisplayProgressBar(ProgressInfo progressInfo)
        {
            m_ProgressBar.title = progressInfo.title;
            m_ProgressBar.value = progressInfo.progress * 100.0f;
            m_ProgressBar.style.display = DisplayStyle.Flex;
        }

        void UpdateBuildProcess()
        {
            m_BuildProcess.FrameUpdate();

            if (m_BuildProcess.IsFinished)
            {
                m_BuildProcess.Builder.progressFeedback -= DisplayProgressBar;
                EditorApplication.update -= UpdateBuildProcess;

                m_BuildProcess.Dispose();
                m_BuildProcess = null;

                HideProgressBar();
                DisplayCancelBuildButton(false);
                SetAssetEditingEnabled(true);
            }
        }

        static string GetPrefKeyName(string propName)
        {
            return $"Kinematica_{nameof(BuilderWindow)}_{propName}";
        }
    }
}
