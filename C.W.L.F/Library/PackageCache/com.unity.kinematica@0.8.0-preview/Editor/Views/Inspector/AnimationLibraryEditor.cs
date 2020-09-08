using System.Linq;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    [CustomEditor(typeof(TaggedAnimationClipSelectionContainer))]
    public class AnimationLibraryEditor : UnityEditor.Editor
    {
        public static readonly string k_AnimationLibraryEditorStyle = "Inspectors/AnimationLibraryEditor.uss";
        VisualElement m_Root;
        ObjectField m_RetargetSourceAvatarField;

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();

            UIElementsUtils.CloneTemplateInto($"Inspectors/AnimationLibraryEditor.uxml", m_Root);
            UIElementsUtils.ApplyStyleSheet(k_AnimationLibraryEditorStyle, m_Root);
            UIElementsUtils.ApplyStyleSheet(BuilderWindow.k_Stylesheet, m_Root);

            var label = m_Root.Q<TextField>("clipNames");
            label.SetEnabled(false);

#if !UNITY_2020_1_OR_NEWER
            var retargetingInput = m_Root.Q<VisualElement>("retargeting");
            retargetingInput.style.display = DisplayStyle.None;
#endif

            m_RetargetSourceAvatarField = m_Root.Q<ObjectField>("retargetSourceAvatar");
            m_RetargetSourceAvatarField.allowSceneObjects = false;
            m_RetargetSourceAvatarField.objectType = typeof(Avatar);
            m_RetargetSourceAvatarField.RegisterValueChangedCallback(OnRetargetSourceAvatarChanged);

            var library = target as TaggedAnimationClipSelectionContainer;
            InitializeRetargetingSourceSelector();

            m_Root.RegisterCallback<AttachToPanelEvent>(evt => Refresh());
            library.SelectionChanged += Refresh;

            return m_Root;
        }

        void OnDisable()
        {
            var library = target as TaggedAnimationClipSelectionContainer;
            if (library != null)
            {
                library.SelectionChanged += Refresh;
            }
        }

        void Refresh()
        {
            var library = target as TaggedAnimationClipSelectionContainer;
            if (library != null)
            {
                var label = m_Root.Q<TextField>("clipNames");
                label.value = library.SelectionNames;

                InitializeRetargetingSourceSelector();
            }
        }

        void InitializeRetargetingSourceSelector()
        {
            var library = target as TaggedAnimationClipSelectionContainer;
            if (library.Selection.All(tac => tac.RetargetSourceAvatar != null))
            {
                var currentValues = library.Selection.Select(tac => tac.RetargetSourceAvatar).Distinct().ToList();
                if (currentValues.Count == 1)
                {
                    m_RetargetSourceAvatarField.SetValueWithoutNotify(currentValues.FirstOrDefault());
                }
                else
                {
                    m_RetargetSourceAvatarField.SetValueWithoutNotify(null);
                }
            }
            else
            {
                m_RetargetSourceAvatarField.SetValueWithoutNotify(null);
            }
        }

        void OnRetargetSourceAvatarChanged(ChangeEvent<Object> evt)
        {
            var library = target as TaggedAnimationClipSelectionContainer;
            if (library != null)
            {
                foreach (var clip in library.Selection)
                {
                    clip.RetargetSourceAvatar = evt.newValue as Avatar;
                }
            }
        }
    }
}
