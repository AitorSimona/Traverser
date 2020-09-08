using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Kinematica;
using Unity.Kinematica.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Timeline
{
    class BoundaryAnimationClipElement : VisualElement
    {
        const string k_DefaultLabel = "Select boundary clip";
        readonly List<TaggedAnimationClip> m_Clips;
        TaggedAnimationClip m_Selection;

        Image m_WarningIcon;
        internal Button m_Button { get; }

        public string text => m_Button.text;

        public TaggedAnimationClip Selection
        {
            get { return m_Selection; }
        }

        public BoundaryAnimationClipElement()
        {
            AddToClassList("boundaryClip");
            AddToClassList("unity-button");

            m_Clips = new List<TaggedAnimationClip>();

            m_Button = new Button();
            m_Button.clickable = null;
            m_Button.RemoveFromClassList("unity-button");
            Add(m_Button);

            var selectClipContextManipulator = new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("None", dropDownMenuAction => { Select(null); });
                evt.menu.AppendSeparator();
                foreach (var clip in m_Clips)
                {
                    var statusCallback = clip == m_Selection ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    evt.menu.AppendAction(clip.ClipName, dropDownMenuAction =>
                    {
                        Select(dropDownMenuAction.userData as TaggedAnimationClip);
                    }, e => statusCallback, clip);
                }
            });

            selectClipContextManipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse});

            this.AddManipulator(selectClipContextManipulator);

            m_WarningIcon = new Image();
            m_WarningIcon.AddToClassList("warningImage");

            m_WarningIcon.tooltip = TaggedAnimationClip.k_MissingClipText;
            m_WarningIcon.style.display = DisplayStyle.None;

            Add(m_WarningIcon);

            Reset();
        }

        public event Action<TaggedAnimationClip> SelectionChanged;

        public void Select(SerializableGuid selection)
        {
            TaggedAnimationClip found = m_Clips.FirstOrDefault(tc => tc.AnimationClipGuid == selection);

            if (m_Selection != found)
            {
                m_Selection = found;
                UpdateLabel();
            }
        }

        internal void Select(TaggedAnimationClip selection)
        {
            if (selection != null && !m_Clips.Contains(selection))
            {
                selection = null;
            }

            if (m_Selection == selection)
            {
                m_Selection = null;
            }
            else
            {
                m_Selection = selection;
            }

            UpdateLabel();
            SelectionChanged?.Invoke(m_Selection);
        }

        public event Action LabelUpdated;

        void UpdateLabel()
        {
            if (m_Selection == null)
            {
                m_Button.text = k_DefaultLabel;
            }
            else
            {
                bool valid = m_Selection.Valid;
                if (string.IsNullOrEmpty(m_Selection.ClipName))
                {
                    m_Button.text = k_DefaultLabel;
                }
                else
                {
                    m_WarningIcon.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
                    m_Button.text = m_Selection.ClipName;
                }
            }

            EditorApplication.delayCall += () => { LabelUpdated?.Invoke(); };
        }

        public void SetClips(List<TaggedAnimationClip> newClips)
        {
            int index = m_Clips.FindIndex(clip =>
            {
                if (!string.IsNullOrEmpty(clip.ClipName))
                {
                    return clip.ClipName == m_Button.text;
                }

                return false;
            });

            m_Clips.Clear();
            m_Clips.AddRange(newClips);
            if (index >= 0)
            {
                if (m_Clips.All(t =>
                {
                    if (!string.IsNullOrEmpty(t.ClipName))
                    {
                        return t.ClipName != m_Button.text;
                    }

                    return true;
                }))
                {
                    if (m_Clips.Count >= index)
                    {
                        if (!string.IsNullOrEmpty(m_Clips[index].ClipName))
                        {
                            m_Button.text = m_Clips[index].ClipName;
                        }
                    }
                }
            }
        }

        public float EstimateControlWidth()
        {
            float estimatedTextSize = TimelineUtility.EstimateTextSize(m_Button);
            float controlWidth = Math.Max(float.IsNaN(estimatedTextSize) ? layout.width : estimatedTextSize + 16, 80) + 2;
            if (m_WarningIcon.style.display == DisplayStyle.Flex)
            {
                controlWidth += m_WarningIcon.layout.width;
            }

            return controlWidth;
        }

        public void Reset()
        {
            m_Selection = null;
            m_Clips.Clear();
            m_Button.text = k_DefaultLabel;
        }
    }
}
