using System;
using System.Collections.Generic;

using Unity.Kinematica.Editor.GenericStruct;
using Unity.Kinematica.UIElements;
using UnityEngine.UIElements;
using UnityEditor;

using Object = UnityEngine.Object;

namespace Unity.Kinematica.Editor
{
    internal class TagEditor : VisualElement
    {
        GenericStructInspector m_PayloadInspector;

        TagAnnotation m_Tag;
        TaggedAnimationClip m_Clip;

        internal TagEditor(TagAnnotation tag, TaggedAnimationClip clip)
        {
            m_Tag = tag;
            m_Clip = clip;

            UIElementsUtils.CloneTemplateInto("Inspectors/TagEditor.uxml", this);
            UIElementsUtils.ApplyStyleSheet(AnnotationsEditor.k_AnnotationsEditorStyle, this);
            AddToClassList(AnnotationsEditor.k_AnnotationsContainer);
            AddToClassList("drawerElement");

            var deleteButton = this.Q<Button>("deleteButton");
            deleteButton.clickable.clicked += () => { RemoveTag(m_Tag); };

            if (!tag.payload.ValidPayloadType)
            {
                Clear();
                var unknownLabel = new Label { text = TagAttribute.k_UnknownTagType };
                unknownLabel.AddToClassList(AnnotationsEditor.k_UnknownAnnotationType);
                Add(unknownLabel);
                return;
            }

            TextField tagType = this.Q<TextField>("tagType");
            tagType.value = TagAttribute.GetDescription(m_Tag.Type);
            tagType.SetEnabled(false);

            Asset asset = m_Clip.Asset;

            TextField tagName = this.Q<TextField>("name");
            tagName.value = m_Tag.name;
            tagName.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue.Equals(m_Tag.name, StringComparison.Ordinal))
                {
                    Undo.RecordObject(asset, "Change tag name");
                    m_Tag.name = evt.newValue;
                    m_Tag.NotifyChanged();
                    asset.MarkDirty();
                }
            });

            TextField metricLabel = this.Q<TextField>("metricLabel");
            var metric = asset.GetMetricForTagType(m_Tag.Type);
            if (metric != null)
            {
                metricLabel.value = metric.name;
                metricLabel.SetEnabled(false);
            }
            else
            {
                metricLabel.style.display = DisplayStyle.None;
            }

            TimeField stf = this.Q<TimeField>("startTime");
            stf.Init(m_Clip, m_Tag.startTime);
            stf.TimeChanged += (newTime) =>
            {
                if (!EqualityComparer<float>.Default.Equals(m_Tag.startTime, newTime))
                {
                    Undo.RecordObject(asset, "Change tag start time");
                    m_Tag.startTime = newTime;
                    m_Tag.NotifyChanged();
                    asset.MarkDirty();
                }
            };


            TimeField dtf = this.Q<TimeField>("durationTime");
            dtf.Init(m_Clip, m_Tag.duration);
            dtf.TimeChanged += (newTime) =>
            {
                if (!EqualityComparer<float>.Default.Equals(m_Tag.duration, newTime))
                {
                    Undo.RecordObject(asset, "Change tag duration");
                    m_Tag.duration = newTime;
                    m_Tag.NotifyChanged();
                    asset.MarkDirty();
                }
            };

            var so = m_Tag.payload.ScriptableObject;
            if (so != null)
            {
                m_PayloadInspector = UnityEditor.Editor.CreateEditor(so) as GenericStructInspector;
                m_PayloadInspector.StructModified += () =>
                {
                    m_Tag.payload.Serialize();
                    m_Tag.NotifyChanged();
                    m_Clip.Asset.MarkDirty();
                };

                VisualElement inspectorElement = m_PayloadInspector.CreateInspectorGUI() ?? new IMGUIContainer(m_PayloadInspector.OnInspectorGUI);
                var inspectorContainer = this.Q<VisualElement>("payloadInspector");
                inspectorContainer.Add(inspectorElement);
            }

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_Tag.Changed += Refresh;
            asset.AssetWasDeserialized += Refresh;
        }

        void Refresh(Asset unused)
        {
            Refresh();
        }

        void Refresh()
        {
            TimeField stf = this.Q<TimeField>("startTime");
            stf.SetValueWithoutNotify(m_Tag.startTime);
            TimeField dtf = this.Q<TimeField>("durationTime");
            dtf.SetValueWithoutNotify(m_Tag.duration);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (m_PayloadInspector != null)
            {
                Object.DestroyImmediate(m_PayloadInspector);
                m_PayloadInspector = null;
            }

            if (m_Tag != null)
            {
                m_Tag.Changed -= Refresh;
            }

            if (m_Clip != null)
            {
                m_Clip.Asset.AssetWasDeserialized -= Refresh;
            }
        }

        void RemoveTag(TagAnnotation tag)
        {
            m_Clip.RemoveTag(tag);
        }
    }
}
