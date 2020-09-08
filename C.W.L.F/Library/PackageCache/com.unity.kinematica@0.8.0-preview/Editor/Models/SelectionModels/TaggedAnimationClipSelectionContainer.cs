using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Kinematica.Editor
{
    class TaggedAnimationClipSelectionContainer : ScriptableObject
    {
        public event Action SelectionChanged;

        List<TaggedAnimationClip> m_Selection;

        public List<TaggedAnimationClip> Selection
        {
            get { return m_Selection ?? (m_Selection = new List<TaggedAnimationClip>()); }
        }

        public string SelectionNames
        {
            get
            {
                return string.Join(",", Selection.Select(tac => tac.ClipName));
            }
        }

        public void Clear()
        {
            Selection.Clear();
            SelectionChanged?.Invoke();
        }

        public void SetSelection(IEnumerable<TaggedAnimationClip> selection)
        {
            Selection.Clear();
            Select(selection);
        }

        public void Select(IEnumerable<TaggedAnimationClip> selection)
        {
            var selectionChanged = false;
            foreach (var clip in selection)
            {
                selectionChanged |= Select(clip, false);
            }

            if (selectionChanged)
            {
                SelectionChanged?.Invoke();
            }
        }

        public bool Select(TaggedAnimationClip clip, bool notify = true)
        {
            if (!Selection.Contains(clip))
            {
                Selection.Add(clip);
                if (notify)
                {
                    SelectionChanged?.Invoke();
                }

                return true;
            }

            return false;
        }

        public bool Unselect(TaggedAnimationClip clip)
        {
            if (Selection.Remove(clip))
            {
                SelectionChanged?.Invoke();
                return true;
            }

            return false;
        }
    }
}
