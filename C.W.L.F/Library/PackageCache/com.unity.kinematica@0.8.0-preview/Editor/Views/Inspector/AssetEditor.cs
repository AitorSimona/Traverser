using System.Linq;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    [CustomEditor(typeof(Asset))]
    internal class AssetEditor : UnityEditor.Editor
    {
        const string k_TemplatePath = "Inspectors/AssetEditor.uxml";
        const string k_Stylesheet = "Inspectors/AssetEditor.uss";

        const float k_MinTimeHorizon = 1f;
        const float k_MaxTimeHorizon = 5f;

        const float k_MinSampleRate = 1f;
        const float k_MaxSampleRate = 120f;

        VisualElement m_Root;

        MetricsEditor m_MetricsEditor;
        VisualElement m_AssetSettingsInput;

        FloatField m_TimeHorizonInput;
        Slider m_TimeHorizonSlider;

        Asset m_Asset;

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();
            UIElementsUtils.ApplyStyleSheet(BuilderWindow.k_Stylesheet, m_Root);
            UIElementsUtils.CloneTemplateInto(k_TemplatePath, m_Root);
            m_Root.AddToClassList("mainContainer");

            m_Asset = target as Asset;
            m_Asset.BuildStarted += BuildStarted;
            m_Asset.BuildStopped += BuildStopped;

            m_AssetSettingsInput = m_Root.Q<VisualElement>("assetSettings");
            //set restrictions on asset settings
            var sampleRateInput = m_AssetSettingsInput.Q<FloatField>("sampleRate");
            sampleRateInput.isDelayed = true;
            var sampleRateSlider = m_AssetSettingsInput.Q<Slider>("sampleRateSlider");
            float sampleRate = Mathf.Clamp(m_Asset.SampleRate, 1f, m_Asset.SampleRate);

            sampleRateInput.value = sampleRate;
            sampleRateSlider.value = sampleRate;
            sampleRateInput.RegisterValueChangedCallback(evt =>
            {
                float newSampleRate = Mathf.Clamp(evt.newValue, k_MinSampleRate, k_MaxSampleRate);
                m_Asset.SampleRate = newSampleRate;
                sampleRateSlider.SetValueWithoutNotify(newSampleRate);
                ClampTimeHorizonInput(1f / newSampleRate);
            });
            sampleRateSlider.RegisterValueChangedCallback(evt =>
            {
                float newSampleRate = Mathf.Clamp(evt.newValue, k_MinSampleRate, k_MaxSampleRate);
                m_Asset.SampleRate = newSampleRate;
                sampleRateInput.SetValueWithoutNotify(newSampleRate);
                ClampTimeHorizonInput(1f / newSampleRate);
            });
            sampleRateInput.SetFloatFieldRange(sampleRateSlider.lowValue, sampleRateSlider.highValue);

            m_TimeHorizonInput = m_AssetSettingsInput.Q<FloatField>("timeHorizon");
            m_TimeHorizonInput.isDelayed = true;
            m_TimeHorizonSlider = m_AssetSettingsInput.Q<Slider>("timeHorizonSlider");
            m_TimeHorizonSlider.value = m_Asset.TimeHorizon;
            m_TimeHorizonInput.value = m_Asset.TimeHorizon;
            m_TimeHorizonInput.RegisterValueChangedCallback(evt =>
            {
                float newTimeHorizon = Mathf.Clamp(evt.newValue, k_MinTimeHorizon, k_MaxTimeHorizon);
                m_Asset.TimeHorizon = newTimeHorizon;
                m_TimeHorizonSlider.SetValueWithoutNotify(newTimeHorizon);
            });
            m_TimeHorizonSlider.RegisterValueChangedCallback(evt =>
            {
                float newTimeHorizon = Mathf.Clamp(evt.newValue, k_MinTimeHorizon, k_MaxTimeHorizon);
                m_Asset.TimeHorizon = newTimeHorizon;
                m_TimeHorizonInput.SetValueWithoutNotify(newTimeHorizon);
            });

            m_TimeHorizonSlider.lowValue = 1f / sampleRateSlider.lowValue;
            m_TimeHorizonInput.SetFloatFieldRange(m_TimeHorizonSlider.lowValue, m_TimeHorizonSlider.highValue);

            var avatarSelector = m_Root.Q<ObjectField>("destinationAvatar");
            avatarSelector.objectType = typeof(Avatar);

            avatarSelector.value = m_Asset.DestinationAvatar;
            avatarSelector.RegisterValueChangedCallback(OnAvatarSelectionChanged);

            UIElementsUtils.ApplyStyleSheet(k_Stylesheet, m_Root);

            m_MetricsEditor = new MetricsEditor(m_Asset);
            m_Root.Q<VisualElement>("metrics").Add(m_MetricsEditor);

            var buildButton = m_Root.Q<Button>("buildButton");
            buildButton.clickable.clicked += BuildButtonClicked;

            if (EditorApplication.isPlaying || m_Asset.BuildInProgress)
            {
                SetInputEnabled(false);
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            return m_Root;
        }

        void OnDisable()
        {
            if (m_Root != null)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }

            if (m_Asset != null)
            {
                m_Asset.BuildStarted -= BuildStarted;
                m_Asset.BuildStopped -= BuildStopped;
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            SetInputEnabled(state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode);
        }

        void BuildStarted()
        {
            SetInputEnabled(false);
        }

        void BuildStopped()
        {
            SetInputEnabled(!EditorApplication.isPlaying);
        }

        void SetInputEnabled(bool enable)
        {
            m_AssetSettingsInput.SetEnabled(enable);
            m_MetricsEditor.SetInputEnabled(enable);
        }

        void ClampTimeHorizonInput(float lowValue)
        {
            m_TimeHorizonInput.SetFloatFieldRange(lowValue);
            m_TimeHorizonSlider.lowValue = lowValue;
        }

        void OnAvatarSelectionChanged(ChangeEvent<Object> evt)
        {
            var asset = target as Asset;
            if (asset != null)
            {
                asset.DestinationAvatar = evt.newValue as Avatar;
                m_MetricsEditor.OnAvatarChanged();
            }
        }

        void BuildButtonClicked()
        {
            if (target != null)
            {
                Selection.activeObject = target;
            }

            BuilderWindow.ShowWindow();
        }
    }
}
