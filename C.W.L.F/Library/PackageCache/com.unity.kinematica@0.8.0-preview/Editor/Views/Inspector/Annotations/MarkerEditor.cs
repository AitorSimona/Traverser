using System.Collections.Generic;
using Unity.Kinematica.Editor.GenericStruct;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class MarkerEditor : VisualElement
    {
        GenericStructInspector m_PayloadInspector;
        MarkerAnnotation m_Marker;
        TaggedAnimationClip m_Clip;

        public MarkerEditor(MarkerAnnotation marker, TaggedAnimationClip clip)
        {
            m_Clip = clip;
            m_Marker = marker;
            UIElementsUtils.ApplyStyleSheet(BuilderWindow.k_Stylesheet, this);
            UIElementsUtils.CloneTemplateInto("Inspectors/MarkerEditor.uxml", this);
            AddToClassList("drawerElement");

            var deleteButton = this.Q<Button>("deleteButton");
            deleteButton.clickable.clicked += () => { RemoveMarker(m_Marker); };

            if (!marker.payload.ValidPayloadType)
            {
                var contents = this.Q<VisualElement>(classes: "arrayEntryContents");
                contents.Clear();
                var unknownLabel = new Label { text = MarkerAttribute.k_UnknownMarkerType };
                unknownLabel.AddToClassList(AnnotationsEditor.k_UnknownAnnotationType);
                contents.Add(unknownLabel);
                return;
            }

            TextField typeLabel = this.Q<TextField>();
            typeLabel.value = MarkerAttribute.GetDescription(m_Marker.payload.Type);
            typeLabel.SetEnabled(false);

            var timeField = this.Q<TimeField>("timeInSeconds");
            timeField.Init(m_Clip, marker.timeInSeconds);
            timeField.TimeChanged += newTime =>
            {
                if (!EqualityComparer<float>.Default.Equals(m_Marker.timeInSeconds, newTime))
                {
                    Undo.RecordObject(m_Clip.Asset, "Change marker time");
                    m_Marker.timeInSeconds = newTime;
                    m_Marker.NotifyChanged();
                    clip.Asset.MarkDirty();
                }
            };

            m_Marker.Changed += UpdateTime;

            m_PayloadInspector = UnityEditor.Editor.CreateEditor(m_Marker.payload.ScriptableObject) as GenericStructInspector;

            m_PayloadInspector.StructModified += () =>
            {
                m_Marker.payload.Serialize();
                m_Clip.Asset.MarkDirty();
            };

            VisualElement inspectorElement = m_PayloadInspector.CreateInspectorGUI() ?? new IMGUIContainer(m_PayloadInspector.OnInspectorGUI);
            var inspectorContainer = this.Q<VisualElement>("payloadInspector");
            inspectorContainer.Add(inspectorElement);

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            m_Clip.Asset.AssetWasDeserialized += UpdateTime;
        }

        void UpdateTime(Asset unused)
        {
            UpdateTime();
        }

        void UpdateTime()
        {
            if (m_Marker == null)
            {
                return;
            }

            var timeField = this.Q<TimeField>("timeInSeconds");
            timeField.SetValueWithoutNotify(m_Marker.timeInSeconds);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_Marker != null)
            {
                m_Marker.Changed -= UpdateTime;
            }

            if (m_PayloadInspector != null)
            {
                Object.DestroyImmediate(m_PayloadInspector);
                m_PayloadInspector = null;
                ManipulatorGizmo.Instance.Unhook();
            }

            m_Clip.Asset.AssetWasDeserialized -= UpdateTime;
        }

        void RemoveMarker(MarkerAnnotation marker)
        {
            m_Clip.RemoveMarker(marker);
        }
    }
}
