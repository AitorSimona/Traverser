using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Kinematica.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class BoundarySelector : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<BoundarySelector , UxmlTraits> {}

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
            UxmlStringAttributeDescription m_Mode = new UxmlStringAttributeDescription { name = "mode", defaultValue = "pre" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                string mode = m_Mode.GetValueFromBag(bag, cc);
                var selector = ve as BoundarySelector;
                selector.BoundaryMode = mode;
            }
        }

        public enum Mode { Pre, Post }

        Mode m_Mode;

        string BoundaryMode
        {
            set
            {
                if (value.Equals("pre", StringComparison.InvariantCultureIgnoreCase))
                {
                    m_Mode = Mode.Pre;
                    m_Label.text = "Pre-Boundary Animation Clip";
                }
                else if (value.Equals("post", StringComparison.InvariantCultureIgnoreCase))
                {
                    m_Mode = Mode.Post;
                    m_Label.text = "Post-Boundary Animation Clip";
                }
            }
        }

        class TaggedAnimationClipSelector : VisualElement
        {
            Asset m_Asset;

            ToolbarSearchField m_FilterField;
            Label m_CurrentSelectionLabel;
            ListView m_View;

            TaggedAnimationClip m_Selection;

            string m_FilterString;

            string FilterString
            {
                set
                {
                    if (!string.Equals(m_FilterString, value))
                    {
                        m_FilterString = value;
                        m_FilterRegex = new Regex(m_FilterString, RegexOptions.IgnoreCase);
                        Refresh();
                    }
                }
            }
            Regex m_FilterRegex;

            Regex Filter
            {
                get { return m_FilterRegex ?? (m_FilterRegex = new Regex(m_FilterString)); }
            }

            List<TaggedAnimationClip> FilteredClips
            {
                get
                {
                    var list = m_Asset.AnimationLibrary.Where(tac => Filter.Matches(tac.ClipName).Count > 0).ToList();
                    list.Insert(0, null);
                    return list;
                }
            }

            Image m_RevertImage;
            bool m_Changes;

            public bool Changes
            {
                get { return m_Changes; }
                set
                {
                    if (m_Changes != value)
                    {
                        m_Changes = value;
                        if (!m_Changes)
                        {
                            m_RevertImage.style.display = DisplayStyle.None;
                            ReInit();
                        }
                        else
                        {
                            m_RevertImage.style.display = DisplayStyle.Flex;
                        }
                    }
                }
            }


            public TaggedAnimationClipSelector()
            {
                UIElementsUtils.ApplyStyleSheet("BoundaryClipWindow.uss", this);
                AddToClassList("clipSelector");

                m_FilterString = string.Empty;

                m_FilterField = new ToolbarSearchField();
                m_FilterField.RegisterValueChangedCallback(OnFilterChanged);
                Add(m_FilterField);

                var selectionArea = new VisualElement();
                selectionArea.AddToClassList("row");
                {
                    m_RevertImage = new Image();
                    var revertClick = new Clickable(() => Changes = false);
                    m_RevertImage.AddManipulator(revertClick);
                    m_RevertImage.AddToClassList("revertIcon");
                    selectionArea.Add(m_RevertImage);
                    m_CurrentSelectionLabel = new Label();
                    m_CurrentSelectionLabel.AddToClassList("selectionLabel");
                    selectionArea.Add(m_CurrentSelectionLabel);
                }
                Add(selectionArea);

                m_View = new ListView();
                m_View.itemHeight = 18;
                m_View.makeItem = MakeItem;
                m_View.bindItem = BindItem;

                m_View.selectionType = SelectionType.None;
                m_View.AddToClassList("clipList");

                Add(m_View);

                UpdateLabel();
            }

            void OnFilterChanged(ChangeEvent<string> evt)
            {
                FilterString = evt.newValue;
            }

            void BindItem(VisualElement element, int index)
            {
                var label = element as Label;
                TaggedAnimationClip clip;
                if (index == 0)
                {
                    clip = null;
                    label.text = "None";
                    label.style.borderBottomWidth = 1;
                }
                else
                {
                    clip = FilteredClips[index];
                    label.text = clip.ClipName;
                    label.style.borderBottomWidth = 0;
                }

                element.userData = clip;
                if (value == clip)
                {
                    element.AddToClassList("toggleItem--checked");
                }
                else
                {
                    element.RemoveFromClassList("toggleItem--checked");
                }
            }

            VisualElement MakeItem()
            {
                var label = new Label();
                label.AddToClassList("toggleItem");

                Clickable c = new Clickable(() =>
                {
                    var taggedAnimationClip = label.userData as TaggedAnimationClip;
                    if (taggedAnimationClip != null)
                    {
                        EditorGUIUtility.PingObject(taggedAnimationClip.GetOrLoadClipSync());
                    }

                    ToggleClip(taggedAnimationClip);
                    Refresh();
                });

                label.AddManipulator(c);
                return label;
            }

            void Refresh()
            {
                m_View.itemsSource = FilteredClips;
                m_View.Refresh();
            }

            void ToggleClip(TaggedAnimationClip clip)
            {
                value = clip;
                Changes = true;
            }

            List<TaggedAnimationClip> m_OriginalSelection = new List<TaggedAnimationClip>();
            public void Init(Asset asset, List<TaggedAnimationClip> currentBoundaryClips)
            {
                m_Asset = asset;
                m_OriginalSelection = currentBoundaryClips;
                ReInit();
            }

            void ReInit()
            {
                IEnumerable<TaggedAnimationClip> currentSelection = m_OriginalSelection.Distinct().ToList();
                if (currentSelection.Count() == 1)
                {
                    value = currentSelection.FirstOrDefault();
                }
                else
                {
                    value = null;
                }

                Refresh();
            }

            public TaggedAnimationClip value
            {
                get { return m_Selection; }
                set
                {
                    if (m_Selection != value)
                    {
                        m_Selection = value;
                    }
                    else
                    {
                        m_Selection = null;
                    }

                    UpdateLabel();
                }
            }

            const string k_DefaultLabel = "None";

            void UpdateLabel()
            {
                m_CurrentSelectionLabel.text = value == null ? k_DefaultLabel : value.ClipName;
            }
        }

        Label m_Label;
        TaggedAnimationClipSelector m_Selector;

        List<TaggedAnimationClip> m_Targets;

        bool NoChanges
        {
            get { return !m_Selector.Changes; }
        }

        public BoundarySelector()
        {
            AddToClassList("boundarySelector");
            m_Label = new Label();
            m_Label.AddToClassList("selectorLabel");
            m_Selector = new TaggedAnimationClipSelector();

            Add(m_Label);
            Add(m_Selector);
        }

        public void Init(List<TaggedAnimationClip> targets)
        {
            m_Targets = targets;
            List<TaggedAnimationClip> currentSelection;
            switch (m_Mode)
            {
                case Mode.Pre:
                    currentSelection = m_Targets.Select(t => t.TaggedPreBoundaryClip).ToList();
                    break;
                case Mode.Post:
                    currentSelection = m_Targets.Select(t => t.TaggedPostBoundaryClip).ToList();
                    break;
                default:
                    currentSelection = new List<TaggedAnimationClip>();
                    break;
            }

            m_Selector.Init(targets.First().Asset, currentSelection);
        }

        public void Apply()
        {
            if (!NoChanges)
            {
                foreach (var target in m_Targets)
                {
                    switch (m_Mode)
                    {
                        case Mode.Pre:
                            target.TaggedPreBoundaryClip = m_Selector.value;
                            break;
                        case Mode.Post:
                            target.TaggedPostBoundaryClip = m_Selector.value;
                            break;
                    }
                }
            }
        }
    }
}
