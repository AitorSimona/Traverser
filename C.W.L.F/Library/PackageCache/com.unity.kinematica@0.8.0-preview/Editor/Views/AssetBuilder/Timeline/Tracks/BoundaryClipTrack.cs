using Timeline;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    class BoundaryClipTrack : GutterTrack
    {
        protected override DisplayStyle DefaultDisplay => DisplayStyle.None;
        BoundaryAnimationClipElement m_PreBoundaryClipElement;
        BoundaryAnimationClipElement m_PostBoundaryClipElement;

        Preview m_PreBoundaryPreview;
        Preview m_PostBoundaryPreview;

        public BoundaryClipTrack(Timeline owner) : base(owner)
        {
            name = "Boundary Clips";
            AddToClassList("boundaryClipTrack");

            m_PreBoundaryClipElement = new BoundaryAnimationClipElement();
            m_PreBoundaryClipElement.LabelUpdated += ResizeContents;
            m_PreBoundaryClipElement.SelectionChanged += SetPreBoundaryClip;
            m_PostBoundaryClipElement = new BoundaryAnimationClipElement();
            m_PostBoundaryClipElement.LabelUpdated += ResizeContents;
            m_PostBoundaryClipElement.SelectionChanged += SetPostBoundaryClip;

            Add(m_PreBoundaryClipElement);
            Add(m_PostBoundaryClipElement);

            if (EditorApplication.isPlaying)
            {
                DisableEdit();
            }
            else
            {
                EnableEdit();
            }

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += UpdateBoundaryClips;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= UpdateBoundaryClips;
        }

        public override void SetClip(TaggedAnimationClip taggedClip)
        {
            base.SetClip(taggedClip);
            if (m_TaggedClip != null)
            {
                m_PreBoundaryClipElement.style.display = DisplayStyle.Flex;
                m_PostBoundaryClipElement.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_PreBoundaryClipElement.style.display = DisplayStyle.None;
                m_PostBoundaryClipElement.style.display = DisplayStyle.None;
            }

            UpdateBoundaryClips();
        }

        public override void ReloadElements()
        {
            UpdateBoundaryClips();
        }

        public override void ResizeContents()
        {
            if (m_TaggedClip?.Clip == null)
            {
                return;
            }

            float startOfClipPos = m_Owner.TimeToLocalPos(0f, this);
            float endOfClipPos = m_Owner.TimeToLocalPos(m_TaggedClip.Clip.DurationInSeconds, this);
            RepositionBoundaryClips(startOfClipPos, endOfClipPos);
        }

        void UpdateBoundaryClips()
        {
            if (m_TaggedClip == null || m_Owner.TargetAsset == null)
            {
                m_PreBoundaryClipElement.Reset();
                m_PreBoundaryClipElement.Reset();
            }
            else
            {
                m_PreBoundaryClipElement.SetClips(m_Owner.TargetAsset.AnimationLibrary);
                m_PreBoundaryClipElement.Select(m_TaggedClip.PreBoundaryClipGuid);
                m_PostBoundaryClipElement.SetClips(m_Owner.TargetAsset.AnimationLibrary);
                m_PostBoundaryClipElement.Select(m_TaggedClip.PostBoundaryClipGuid);
            }

            ResizeContents();
        }

        void RepositionBoundaryClips(float startOfClipPos, float endOfClipPos)
        {
            float controlWidth = m_PreBoundaryClipElement.EstimateControlWidth();
            m_PreBoundaryClipElement.style.left = startOfClipPos - controlWidth;
            m_PostBoundaryClipElement.style.left = endOfClipPos;
        }

        public override void EnableEdit()
        {
            m_PreBoundaryClipElement.SetEnabled(true);
            m_PostBoundaryClipElement.SetEnabled(true);
        }

        public override void DisableEdit()
        {
            m_PreBoundaryClipElement.SetEnabled(false);
            m_PostBoundaryClipElement.SetEnabled(false);
        }

        void SetPreBoundaryClip(TaggedAnimationClip boundary)
        {
            if (m_TaggedClip != null && m_TaggedClip.TaggedPreBoundaryClip != boundary)
            {
                m_TaggedClip.TaggedPreBoundaryClip = boundary;
                m_Owner.PreviewActiveTime();
            }
        }

        void SetPostBoundaryClip(TaggedAnimationClip boundary)
        {
            if (m_TaggedClip != null && m_TaggedClip.TaggedPostBoundaryClip != boundary)
            {
                m_TaggedClip.TaggedPostBoundaryClip = boundary;
                m_Owner.PreviewActiveTime();
            }
        }

        public bool Preview(float time)
        {
            if (m_TaggedClip == null)
            {
                return false;
            }

            if (time >= 0 && time <= m_TaggedClip.DurationInSeconds)
            {
                return false;
            }

            if (time < 0)
            {
                if (m_PreBoundaryPreview != null)
                {
                    m_PreBoundaryPreview.PreviewTime(m_PreBoundaryClipElement.Selection, time + m_PreBoundaryClipElement.Selection.DurationInSeconds);
                }
            }
            else if (time > m_TaggedClip.DurationInSeconds)
            {
                if (m_PostBoundaryPreview != null)
                {
                    m_PostBoundaryPreview.PreviewTime(m_PostBoundaryClipElement.Selection, time - m_TaggedClip.DurationInSeconds);
                }
            }

            return true;
        }

        internal void ValidateBoundaryPreviews()
        {
            m_PreBoundaryPreview?.DisableDisplayTrajectory();
            if (m_PreBoundaryPreview == null)
            {
                if (m_TaggedClip.TaggedPreBoundaryClip != null)
                {
                    m_PreBoundaryPreview = Editor.Preview.CreatePreview(m_Owner.TargetAsset, m_Owner.PreviewTarget);
                }
            }
            else if (m_TaggedClip.TaggedPreBoundaryClip == null)
            {
                m_PreBoundaryPreview?.Dispose();
                m_PreBoundaryPreview = null;
            }

            m_PostBoundaryPreview?.DisableDisplayTrajectory();
            if (m_PostBoundaryPreview == null)
            {
                if (m_TaggedClip.TaggedPostBoundaryClip != null)
                {
                    m_PostBoundaryPreview = Editor.Preview.CreatePreview(m_Owner.TargetAsset, m_Owner.PreviewTarget);
                }
            }
            else if (m_TaggedClip.TaggedPostBoundaryClip == null)
            {
                m_PostBoundaryPreview?.Dispose();
                m_PostBoundaryPreview = null;
            }
        }

        public void DisposePreviews()
        {
            if (m_PreBoundaryPreview != null)
            {
                m_PreBoundaryPreview.Dispose();
                m_PreBoundaryPreview = null;
            }

            if (m_PostBoundaryPreview != null)
            {
                m_PostBoundaryPreview.Dispose();
                m_PostBoundaryPreview = null;
            }
        }
    }
}
