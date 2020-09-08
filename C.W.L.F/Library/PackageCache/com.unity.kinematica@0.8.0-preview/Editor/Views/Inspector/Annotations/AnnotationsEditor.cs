using System.Linq;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    [CustomEditor(typeof(TimelineSelectionContainer))]
    internal class AnnotationsEditor : UnityEditor.Editor
    {
        public static readonly string k_AnnotationsEditorStyle = "Inspectors/AnnotationInspectors.uss";
        public static readonly string k_AnnotationsContainer = "editor";

        internal static readonly string k_UnknownAnnotationType = "unknownAnnotationTypeLabel";

        VisualElement m_Root;
        ObjectField m_RetargetSourceAvatarField;
        TextField m_ClipField;

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();
            UIElementsUtils.CloneTemplateInto($"Inspectors/TaggedAnimationClipEditor.uxml", m_Root);
            UIElementsUtils.ApplyStyleSheet(k_AnnotationsEditorStyle, m_Root);
            UIElementsUtils.ApplyStyleSheet(BuilderWindow.k_Stylesheet, m_Root);

#if !UNITY_2020_1_OR_NEWER
            var retargetingInput = m_Root.Q<VisualElement>("retargeting");
            retargetingInput.style.display = DisplayStyle.None;
#endif
            m_ClipField = m_Root.Q<TextField>("clipName");
            m_ClipField.SetEnabled(false);

            m_RetargetSourceAvatarField = m_Root.Q<ObjectField>("retargetSourceAvatar");
            m_RetargetSourceAvatarField.allowSceneObjects = false;
            m_RetargetSourceAvatarField.objectType = typeof(Avatar);
            m_RetargetSourceAvatarField.RegisterValueChangedCallback(OnRetargetSourceAvatarChanged);

            Refresh();

            var annotationsList = target as TimelineSelectionContainer;
            annotationsList.AnnotationsChanged += Refresh;

            TaggedAnimationClip clip = annotationsList.Clip;
            Asset asset = null;
            if (clip != null)
            {
                asset = clip.Asset;
                if (asset != null)
                {
                    asset.BuildStarted += BuildStarted;
                    asset.BuildStopped += BuildStopped;
                }
            }

            if (EditorApplication.isPlaying || asset != null && asset.BuildInProgress)
            {
                SetInputEnabled(false);
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            return m_Root;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            SetInputEnabled(state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode);
        }

        void SetInputEnabled(bool enable)
        {
            m_Root.SetEnabled(enable);
            Refresh();
        }

        void Refresh()
        {
            Clear();
            VisualElement tagsContainer = m_Root.Q<VisualElement>("tags");
            var selectionContainer = target as TimelineSelectionContainer;

            if (selectionContainer.m_FullClipSelection)
            {
                selectionContainer.ReloadFromClip();
            }

            if (selectionContainer.Clip != null)
            {
                m_RetargetSourceAvatarField.value = selectionContainer.Clip.RetargetSourceAvatar;
                m_ClipField.value = selectionContainer.Clip.ClipName;
            }
            else
            {
                m_ClipField.value = string.Empty;
            }

            if (selectionContainer.Tags.Any())
            {
                tagsContainer.style.display = DisplayStyle.Flex;
                VisualElement tagsListElement = tagsContainer.Q<VisualElement>("tagsList");
                foreach (var tag in selectionContainer.Tags)
                {
                    tagsListElement.Add(new TagEditor(tag, selectionContainer.Clip));
                }
            }
            else
            {
                tagsContainer.style.display = DisplayStyle.None;
            }

            VisualElement markerContainer = m_Root.Q<VisualElement>("markers");
            if (selectionContainer.Markers.Any())
            {
                markerContainer.style.display = DisplayStyle.Flex;
                var markerListElement = markerContainer.Q<VisualElement>("markersList");
                foreach (var marker in selectionContainer.Markers)
                {
                    markerListElement.Add(new MarkerEditor(marker, selectionContainer.Clip));
                }
            }
            else
            {
                markerContainer.style.display = DisplayStyle.None;
            }
        }

        void OnDisable()
        {
            Clear();

            if (target is TimelineSelectionContainer selectionContainer)
            {
                selectionContainer.AnnotationsChanged -= Refresh;
                if (selectionContainer != null)
                {
                    TaggedAnimationClip clip = selectionContainer.Clip;
                    if (clip != null)
                    {
                        Asset asset = clip.Asset;
                        if (asset != null)
                        {
                            asset.BuildStarted -= BuildStarted;
                            asset.BuildStopped -= BuildStopped;
                        }
                    }
                }
            }

            if (m_Root != null)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        void BuildStarted()
        {
            SetInputEnabled(false);
        }

        void BuildStopped()
        {
            SetInputEnabled(!EditorApplication.isPlaying);
        }

        void Clear()
        {
            if (m_Root == null)
            {
                return;
            }

            VisualElement tagsContainer = m_Root.Q<VisualElement>("tagsList");
            tagsContainer.Clear();
            VisualElement markerContainer = m_Root.Q<VisualElement>("markersList");
            markerContainer.Clear();
        }

        void OnRetargetSourceAvatarChanged(ChangeEvent<Object> evt)
        {
            if (target != null)
            {
                var selectionContainer = target as TimelineSelectionContainer;
                if (selectionContainer != null && selectionContainer.Clip != null)
                {
                    selectionContainer.Clip.RetargetSourceAvatar = evt.newValue as Avatar;
                }
            }
        }
    }
}
