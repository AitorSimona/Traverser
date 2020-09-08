using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class JointField : VisualElement
    {
        Asset m_Asset;
        Asset.Metric m_Metric;
        SerializedProperty m_Property;

        List<string> m_JointNames;
        readonly ListView m_ListView = new ListView();

        bool m_ForceDisabled;

        public JointField(Asset asset, SerializedProperty property, Asset.Metric metric)
        {
            m_Asset = asset;
            m_Property = property;
            m_Metric = metric;

            Foldout foldout = new Foldout { text = "Joints"};
            foldout.AddToClassList("jointToggle");
            foldout.value = property.isExpanded;
            Add(foldout);

            foldout.RegisterValueChangedCallback(evt => ToggleListVisibility());

            Add(m_ListView);
            m_ListView.AddToClassList("jointsListView");
            m_ListView.style.display = property.isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            Rebuild();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            focusable = true;

            m_ForceDisabled = false;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += Refresh;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= Refresh;
        }

        void Refresh()
        {
            m_ListView.Refresh();
        }

        void ToggleListVisibility()
        {
            if (m_ListView.style.display == DisplayStyle.Flex)
            {
                m_ListView.style.display = DisplayStyle.None;
                m_Property.isExpanded = false;
            }
            else
            {
                m_ListView.style.display = DisplayStyle.Flex;
                m_Property.isExpanded = true;
            }

            m_Asset.MarkDirty();
        }

        public void Rebuild()
        {
            if (m_Asset == null)
            {
                Add(new Label { text = "invalid asset" });
                return;
            }

            Avatar avatar = m_Asset.DestinationAvatar;
            if (avatar == null)
            {
                Add(new Label { text = "please select an avatar" });
                return;
            }

            m_JointNames = avatar.GetAvatarJointNames();
            if (m_JointNames == null)
            {
                Add(new Label { text = "Avatar not found on disk." });
                return;
            }

            m_ListView.AddToClassList("jointList");
            m_ListView.itemHeight = 16;
            m_ListView.style.minHeight = 16 * m_JointNames.Count + 4;
            m_ListView.itemsSource = m_JointNames;
            m_ListView.makeItem = MakeItem;
            m_ListView.bindItem = BindItem;
        }

        void BindItem(VisualElement element, int index)
        {
            if (m_JointNames == null || index >= m_JointNames.Count)
            {
                return;
            }

            Toggle t = element.ElementAt(0) as Toggle;
            t.value = m_Metric.joints.Contains(m_JointNames[index]);
            Label joint = element.ElementAt(1) as Label;
            joint.text = m_JointNames[index];

            if (EditorApplication.isPlaying || m_ForceDisabled)
            {
                element.SetEnabled(false);
            }
        }

        VisualElement MakeItem()
        {
            var root = new VisualElement();
            root.AddToClassList("jointListItem");

            Toggle t = new Toggle();
            root.Add(t);

            var joint = new Label();
            //TODO - this value change callback can cause toggles to happen during binding (instead of just on the user toggling).
            //       this does not happen currently as the ListView is forced to be fully displayed (no virtualization)
            t.RegisterValueChangedCallback(evt => ToggleJoint(joint.text));
            root.Add(joint);

            return root;
        }

        void ToggleJoint(string joint)
        {
            if (m_Metric.joints.Contains(joint))
            {
                Undo.IncrementCurrentGroup();
                Undo.RecordObject(m_Asset, $"Unselect joint {joint} on {m_Metric.name}");
                m_Metric.joints.Remove(joint);
            }
            else
            {
                Undo.RecordObject(m_Asset, $"Select joint {joint} on {m_Metric.name}");
                m_Metric.joints.Add(joint);
            }

            m_Asset.MarkDirty();
        }

        public void SetInputEnabled(bool enabled)
        {
            m_ForceDisabled = !enabled;
            EditorApplication.delayCall += m_ListView.Refresh;
        }
    }
}
