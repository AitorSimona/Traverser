using System;
using Unity.Kinematica.UIElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class PlayControls : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<PlayControls, UxmlTraits>
        {
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
        }

        ToolbarToggle m_PlayToggle;

        void LoadTemplate()
        {
            UIElementsUtils.CloneTemplateInto("PlayControls.uxml", this);
            UIElementsUtils.ApplyStyleSheet("PlayControl.uss", this);
            AddToClassList("playControl");

            var gotoBeginButton = this.Q<Button>("firstFrame");
            gotoBeginButton.clickable.clicked += OnFirstFrameClicked;
            var previousFrameButton = this.Q<Button>("previousFrame");
            previousFrameButton.clickable.clicked += OnStepBackClicked;
            m_PlayToggle = this.Q<ToolbarToggle>("play");
            m_PlayToggle.RegisterValueChangedCallback(evt => OnPlayClicked(evt.newValue));
            var nextFrameButton = this.Q<Button>("nextFrame");
            nextFrameButton.clickable.clicked += OnStepForwardClicked;
            var gotoEndButton = this.Q<Button>("lastFrame");
            gotoEndButton.clickable.clicked += OnLastFrameClicked;
        }

        public event Action<bool> TogglePlay;

        bool m_Playing;
        void OnPlayClicked(bool newValue)
        {
            m_Playing = newValue;
            TogglePlay?.Invoke(m_Playing);
        }

        public void TogglePlayOff()
        {
            m_PlayToggle.value = false;
        }

        public void TogglePlayOffWithoutNotify()
        {
            m_PlayToggle.SetValueWithoutNotify(false);
        }

        public event Action<int> StepFrame;

        void OnStepBackClicked()
        {
            StepFrame?.Invoke(-1);
        }

        void OnStepForwardClicked()
        {
            StepFrame?.Invoke(1);
        }

        public event Action JumpToFirst;
        void OnFirstFrameClicked()
        {
            JumpToFirst?.Invoke();
        }

        public event Action JumpToLast;
        void OnLastFrameClicked()
        {
            JumpToLast?.Invoke();
        }

        public PlayControls()
        {
            LoadTemplate();
        }
    }
}
