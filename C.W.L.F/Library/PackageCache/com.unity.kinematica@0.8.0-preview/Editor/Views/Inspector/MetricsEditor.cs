using System.Collections.Generic;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Unity.Kinematica.Editor
{
    class MetricsEditor : VisualElement
    {
        static readonly List<MetricsEditor> k_Editors = new List<MetricsEditor>();

        public static void RegisterMetricsEditor(MetricsEditor ed)
        {
            k_Editors.Add(ed);
        }

        public static void UnregisterMetricsEditor(MetricsEditor ed)
        {
            k_Editors.Remove(ed);
        }

        static void RebuildRegisteredMetricsEditors()
        {
            k_Editors.ForEach(ed => ed.BuildMetricFields());
        }

        const string k_ButtonContainerKey = "drawerButtonContainer";
        const string k_RemoveMetricButtonKey = "removeMetricButton";
        const string k_ArrayButtonKey = "arrayButton";
        const string k_DrawerElementKey = "drawerElement";

        const string k_AddTooltip = "Add Metric";
        const string k_RemoveTooltip = "Remove Metric";
        const string k_RemoveButtonText = "-";

        Asset m_Asset;
        SerializedObject m_SO;
        VisualElement m_MetricEditorContainer;

        public MetricsEditor(Asset asset)
        {
            m_Asset = asset;

            UIElementsUtils.CloneTemplateInto("Drawers/MetricsDrawer.uxml", this);
            UIElementsUtils.ApplyStyleSheet("Drawers/MetricsDrawer.uss", this);

            AddToClassList("drawerRoot");

            m_MetricEditorContainer = this.Q<VisualElement>("metricsContainer");

            BuildMetricFields();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            RegisterMetricsEditor(this);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            m_MetricEditorContainer.Unbind();
            m_MetricEditorContainer.Clear();
            m_SO = null;
            UnregisterMetricsEditor(this);
        }

        void OnUndoRedo()
        {
            if (m_Asset == null)
            {
                m_SO = null;
                m_MetricEditorContainer.Clear();
                return;
            }

            if (m_Asset.Metrics.Count != m_MetricEditorContainer.Query(classes: MetricField.k_MetricFieldClass).Build().ToList().Count)
            {
                BuildMetricFields();
            }
        }

        void BuildMetricFields()
        {
            m_MetricEditorContainer.Clear();
            m_SO = new SerializedObject(m_Asset);

            SerializedProperty metrics = m_SO.FindProperty(Asset.k_MetricsPropertyPath);
            int metricsCount = metrics.arraySize;
            VisualElement buttonContainer = null;

            for (int metricIndex = 0; metricIndex < metricsCount; ++metricIndex)
            {
                VisualElement metricElement = new VisualElement();
                SerializedProperty metricProperty = metrics.GetArrayElementAtIndex(metricIndex);
                metricElement.Add(new MetricField(m_Asset, metricProperty, metricIndex));

                buttonContainer = new VisualElement();
                buttonContainer.AddToClassList(k_ButtonContainerKey);
                metricElement.Add(buttonContainer);

                Button removeButton = new Button();
                int index = metricIndex;
                removeButton.clickable.clicked += () => { RemoveMetric(index); };
                removeButton.text = k_RemoveButtonText;
                removeButton.tooltip = k_RemoveTooltip;
                removeButton.AddToClassList(k_RemoveMetricButtonKey);
                removeButton.AddToClassList(k_ArrayButtonKey);
                buttonContainer.Add(removeButton);

                metricElement.AddToClassList(k_DrawerElementKey);

                m_MetricEditorContainer.Add(metricElement);
            }

            if (buttonContainer == null)
            {
                //If we have no metrics we should still show the Add button
                buttonContainer = new VisualElement();
                m_MetricEditorContainer.Add(buttonContainer);
            }

            Button addButton = new Button();
            addButton.name = "addMetricButton";
            addButton.clickable.clicked += AddMetric;
            addButton.text = "+";
            addButton.tooltip = k_AddTooltip;
            addButton.AddToClassList("metricButton");
            addButton.AddToClassList(k_ArrayButtonKey);
            buttonContainer.Insert(0, addButton);

            m_MetricEditorContainer.Bind(m_SO);
        }

        void AddMetric()
        {
            Undo.RecordObject(m_Asset, k_AddTooltip);
            Asset.Metric newMetric = Asset.Metric.Copy(Asset.k_DefaultMetric);
            newMetric.TagTypes.Clear();
            newMetric.name = "New Metric";
            m_Asset.AddMetric(newMetric);
            RebuildRegisteredMetricsEditors();
        }

        void RemoveMetric(int index)
        {
            Undo.RecordObject(m_Asset, k_RemoveTooltip);
            if (m_Asset.Metrics.Count <= index)
            {
                Debug.LogError($"Cannot remove Metric at index {index} when only {m_Asset.Metrics.Count} are present");
                return;
            }

            m_Asset.RemoveMetric(index);
            RebuildRegisteredMetricsEditors();
        }

        public void OnAvatarChanged()
        {
            foreach (var jointField in this.Query<JointField>().Build().ToList())
            {
                jointField.Rebuild();
            }
        }

        public void SetInputEnabled(bool enable)
        {
            UQueryState<Button> buttons = m_MetricEditorContainer.Query<Button>(classes: k_ArrayButtonKey).Build();
            buttons.ForEach(b => b.SetEnabled(enable));

            UQueryState<MetricField> metricFields = m_MetricEditorContainer.Query<MetricField>().Build();
            metricFields.ForEach(mf => mf.SetInputEnabled(enable));
        }
    }
}
