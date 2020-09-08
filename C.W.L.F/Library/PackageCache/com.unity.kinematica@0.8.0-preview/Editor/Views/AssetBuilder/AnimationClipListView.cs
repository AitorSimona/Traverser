using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class AnimationClipListView : ListView
    {
        internal void MarkClipListsForUpdate()
        {
            EditorApplication.delayCall += () =>
            {
                Refresh();
            };
        }

        public new class UxmlFactory : UxmlFactory<AnimationClipListView, UxmlTraits>
        {
        }

        public new class UxmlTraits : BindableElement.UxmlTraits
        {
        }

        public AnimationClipListView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            onSelectionChanged += OnAnimationClipSelectionChanged;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            AnimationClipPostprocessor.AddOnImport(MarkClipListsForUpdate);
            AnimationClipPostprocessor.AddOnDelete(MarkClipListsForUpdate);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
            UnregisterCallback<DragPerformEvent>(OnDragPerform);

            AnimationClipPostprocessor.RemoveOnImport(MarkClipListsForUpdate);
            AnimationClipPostprocessor.RemoveOnDelete(MarkClipListsForUpdate);
        }

        public void SelectItems(List<int> items)
        {
            if (itemsSource == null)
            {
                return;
            }

            foreach (int index in items)
            {
                if (index >= 0 && index < itemsSource.Count)
                {
                    AddToSelection(index);
                }
            }
        }

        public void SelectItem(TaggedAnimationClip obj)
        {
            if (itemsSource == null)
            {
                return;
            }

            int index = itemsSource.IndexOf(obj);
            if (index >= 0)
            {
                SetSelection(index);
            }
        }

        internal BuilderWindow m_Window;

        internal List<TaggedAnimationClip> m_ClipSelection = new List<TaggedAnimationClip>();

        void OnAnimationClipSelectionChanged(List<object> animationClips)
        {
            m_ClipSelection.Clear();
            foreach (var c in animationClips.Cast<TaggedAnimationClip>())
            {
                m_ClipSelection.Add(c);
            }
        }

        void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (!EditorApplication.isPlaying && DragAndDrop.objectReferences.Any(x => x is AnimationClip))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }

        void OnDragPerform(DragPerformEvent evt)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            var clips = DragAndDrop.objectReferences.OfType<AnimationClip>().ToList();
            var taggedClips = TaggedAnimationClip.BuildFromClips(clips, m_Window.Asset, ETagImportOption.AddDefaultTag);
            if (taggedClips.Any())
            {
                if (m_Window.Asset != null)
                {
                    m_Window.Asset.AddClips(taggedClips);
                    Refresh();
                }
            }
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent keyDownEvt)
            {
                if (keyDownEvt.keyCode == KeyCode.Delete && !EditorApplication.isPlaying)
                {
                    DeleteSelection();
                }
            }
        }

        internal void DeleteSelection()
        {
            int currentSelection = selectedIndex;
            m_Window.Asset.RemoveClips(m_ClipSelection);
            m_ClipSelection.Clear();
            ClearSelection();
            Refresh();
            while (currentSelection >= m_Window.Asset.AnimationLibrary.Count)
            {
                --currentSelection;
            }

            selectedIndex = currentSelection;
        }

        internal void GenerateProceduralAnnotations()
        {
            bool bNotationsAdded = false;

            Experimental.IProceduralAnnotationsGenerator[] generators = Experimental.ProceduralAnnotations.CreateGenerators(false);

            for (int i = 0; i < m_ClipSelection.Count; ++i)
            {
                var taggedClip = m_ClipSelection[i];

                AnimationClip clip = taggedClip.GetOrLoadClipSync();
                Experimental.ProceduralAnnotations annotations = new Experimental.ProceduralAnnotations();

                foreach (var generator in generators)
                {
                    generator.GenerateAnnotations(clip, annotations);
                }

                if (annotations.NumAnnotations > 0)
                {
                    bNotationsAdded = true;

                    foreach (TagAnnotation tag in annotations.Tags)
                    {
                        taggedClip.AddTag(tag);
                    }

                    foreach (MarkerAnnotation marker in annotations.Markers)
                    {
                        taggedClip.AddMarker(marker);
                    }
                }
            }

            if (bNotationsAdded)
            {
                //Refresh() alone doesn't seems to work for showing new tags and markers.
                if (m_ClipSelection.Count == 1)
                {
                    SelectItem(m_ClipSelection[0]);
                }

                Refresh();
            }
        }

        internal DropdownMenuAction.Status CanGenerateProceduralAnnotations()
        {
            //The generators will be empty if none are implemented or none are implemented in for tools

            Experimental.IProceduralAnnotationsGenerator[] generators = Experimental.ProceduralAnnotations.CreateGenerators(false);
            if (generators.Length > 0)
            {
                return DropdownMenuAction.Status.Normal;
            }

            return DropdownMenuAction.Status.Hidden;
        }

        internal void UpdateSource(IList source)
        {
            itemsSource = source ?? new List<TaggedAnimationClip>();

            int itemCount = 0;
            if (itemsSource != null)
            {
                itemCount = itemsSource.OfType<TaggedAnimationClip>().Count();
            }

            if (itemCount > 0)
            {
                selectedIndex = -1;
            }
        }
    }
}
