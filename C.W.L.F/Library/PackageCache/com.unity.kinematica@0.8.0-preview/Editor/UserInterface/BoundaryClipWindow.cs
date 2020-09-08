using System.Collections.Generic;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class BoundaryClipWindow : EditorWindow
    {
        Button m_SelectorDropdown;

        [SerializeField]
        Asset m_Asset;

        [SerializeField]
        List<TaggedAnimationClip> m_ClipsToModify;

        BoundarySelector m_PreSelector;
        BoundarySelector m_PostSelector;

        public static void ShowWindow(Asset asset, List<TaggedAnimationClip> selection)
        {
            var window = CreateInstance<BoundaryClipWindow>();

            var size = new Vector2(450, 315);
            window.minSize = size;
            window.Init(asset, selection);
            var resolution = Screen.currentResolution;
            window.position = new Rect(resolution.width / 2f - size.x, resolution.height / 2f - size.y, 0, size.y);
            window.ShowModalUtility();
        }

        void LoadTemplate()
        {
            UIElementsUtils.ApplyStyleSheet($"{nameof(BoundaryClipWindow)}.uss", rootVisualElement);
            UIElementsUtils.CloneTemplateInto($"{nameof(BoundaryClipWindow)}.uxml", rootVisualElement);
            rootVisualElement.AddToClassList("boundaryClipWindow");

            m_PreSelector = rootVisualElement.Q<BoundarySelector>("preBoundarySelector");
            m_PostSelector = rootVisualElement.Q<BoundarySelector>("postBoundarySelector");

            var okButton = rootVisualElement.Q<Button>("confirmButton");
            okButton.clickable.clicked += OKClicked;

            var cancelButton = rootVisualElement.Q<Button>("cancelButton");
            cancelButton.clickable.clicked += Close;
        }

        public void Init(Asset asset, List<TaggedAnimationClip> toModify)
        {
            if (m_PreSelector == null)
            {
                LoadTemplate();
            }

            titleContent = new GUIContent("Boundary Clip Selection");

            var size = new Vector2(450, 60);
            minSize = size;

            m_Asset = asset;
            m_ClipsToModify = toModify;

            m_PreSelector.Init(m_ClipsToModify);
            m_PostSelector.Init(m_ClipsToModify);
        }

        void OnEnable()
        {
            if (m_PreSelector == null)
            {
                LoadTemplate();
            }

            if (m_Asset != null)
            {
                Init(m_Asset, m_ClipsToModify);
            }
        }

        void OKClicked()
        {
            m_PreSelector.Apply();
            m_PostSelector.Apply();

            Close();
        }
    }
}
