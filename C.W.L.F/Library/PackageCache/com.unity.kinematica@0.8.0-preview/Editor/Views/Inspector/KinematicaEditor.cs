using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    [CustomEditor(typeof(Kinematica))]
    class KinematicaEditor : UnityEditor.Editor
    {
        VisualElement m_Root;
        VisualElement m_Warning;

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();

            m_Warning = new VisualElement();
            UIElementsUtils.CloneTemplateInto("AssetDirtyWarning.uxml", m_Warning);
            UIElementsUtils.ApplyStyleSheet(BuilderWindow.k_Stylesheet, m_Warning);

            ShowOrHideAssetDirtyWarning(EditorApplication.isPlaying);

            m_Root.Add(m_Warning);

            m_Root.Add(new IMGUIContainer(OnInspectorGUI));

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            return m_Root;
        }

        void OnDisable()
        {
            if (m_Root != null)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            ShowOrHideAssetDirtyWarning(state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.ExitingEditMode);
        }

        void ShowOrHideAssetDirtyWarning(bool inOrEnteringPlayMode)
        {
            if (inOrEnteringPlayMode)
            {
                var binary = target as Kinematica;
                if (binary != null)
                {
                    SerializableGuid guid = binary.resource.assetGuid;
                    // TODO - 2020.1 supports faster search pattern
                    // var assetGuids = AssetDatabase.FindAssets("glob:Assets/**/*.{asset|Asset}");
                    var assetGuids = AssetDatabase.FindAssets("t:Asset");
                    foreach (string assetGuid in assetGuids)
                    {
                        Asset asset = AssetDatabase.LoadAssetAtPath<Asset>(AssetDatabase.GUIDToAssetPath(assetGuid));
                        if (asset == null)
                        {
                            continue;
                        }

                        SerializableGuid binaryGuid = asset.GetBinaryReference().assetGuid;
                        if (binaryGuid == guid)
                        {
                            m_Warning.style.display = asset.BinaryUpToDate ? DisplayStyle.None : DisplayStyle.Flex;
                            return;
                        }
                    }
                }
            }


            m_Warning.style.display = DisplayStyle.None;
        }
    }
}
