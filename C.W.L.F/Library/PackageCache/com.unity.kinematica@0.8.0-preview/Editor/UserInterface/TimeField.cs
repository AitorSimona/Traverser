using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class TimeField : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TimeField, UxmlTraits>
        {
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_FieldName = new UxmlStringAttributeDescription { name = "label", defaultValue = "Time" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var field = ve as TimeField;
                string name = m_FieldName.GetValueFromBag(bag, cc);
                if (!string.IsNullOrEmpty(name))
                {
                    field.m_FieldName.text = name;
                }
            }
        }

        Label m_FieldName;
        FloatField m_SecondsField;
        IntegerField m_FrameField;

        TaggedAnimationClip m_Clip;

        public TimeField(string fieldName)
        {
            LoadTemplate();
            m_FieldName.text = fieldName;
        }

        public TimeField()
        {
            LoadTemplate();
        }

        void LoadTemplate()
        {
            AddToClassList("timeField");
            m_FieldName = new Label();
            m_FieldName.AddToClassList("timeFieldNameLabel");
            Add(m_FieldName);
            var inputContainer = new VisualElement();
            inputContainer.AddToClassList("timeFieldInputContainer");
            Add(inputContainer);

            m_SecondsField = new FloatField("Seconds");
            m_SecondsField.AddToClassList("timeInput");
            m_SecondsField.RegisterValueChangedCallback(OnTimeInSecondsChanged);
            inputContainer.Add(m_SecondsField);

            m_FrameField = new IntegerField("Frame");
            m_FrameField.AddToClassList("timeInput");
            m_FrameField.RegisterValueChangedCallback(OnFrameChanged);
            inputContainer.Add(m_FrameField);

            SyncToViewModeSetting();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Timeline.TimelineViewModeChange += SetTimeEditMode;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Timeline.TimelineViewModeChange -= SetTimeEditMode;
        }

        void SyncToViewModeSetting()
        {
            string viewMode = EditorPrefs.GetString(Timeline.k_TimelineUnitsPreferenceKey);
            TimelineViewMode timelineViewMode = TimelineViewMode.frames;
            if (!string.IsNullOrEmpty(viewMode))
            {
                if (int.TryParse(viewMode, out int intVal))
                {
                    timelineViewMode = (TimelineViewMode)intVal;
                }
                else
                {
                    timelineViewMode = TimelineViewMode.frames;
                }
            }

            SetTimeEditMode(timelineViewMode);
        }

        void SetTimeEditMode(TimelineViewMode timelineViewMode)
        {
            if (timelineViewMode == TimelineViewMode.frames)
            {
                m_SecondsField.style.display = DisplayStyle.None;
                m_FrameField.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_FrameField.style.display = DisplayStyle.None;
                m_SecondsField.style.display = DisplayStyle.Flex;
            }
        }

        public event Action<float> TimeChanged;

        public void Init(TaggedAnimationClip clip, float value)
        {
            m_Clip = clip;
            SetValueWithoutNotify(value);
        }

        public float value
        {
            get { return m_SecondsField.value; }
            set { m_SecondsField.value = value; }
        }
        public void SetValueWithoutNotify(float value)
        {
            m_SecondsField.SetValueWithoutNotify(value);
            m_FrameField.SetValueWithoutNotify(Mathematics.Missing.roundToInt(value * m_Clip.SampleRate));
        }

        void OnTimeInSecondsChanged(ChangeEvent<float> evt)
        {
            if (m_Clip == null)
            {
                return;
            }

            int frameValue = Mathematics.Missing.roundToInt(evt.newValue * m_Clip.SampleRate);
            m_FrameField.SetValueWithoutNotify(frameValue);
            TimeChanged?.Invoke(evt.newValue);
        }

        void OnFrameChanged(ChangeEvent<int> evt)
        {
            if (m_Clip == null)
            {
                return;
            }

            float secondsValue = evt.newValue / m_Clip.SampleRate;
            {
                m_SecondsField.SetValueWithoutNotify(secondsValue);
                TimeChanged?.Invoke(secondsValue);
            }
        }
    }
}
