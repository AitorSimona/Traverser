using Unity.Kinematica.UIElements;
using UnityEngine.UIElements;

namespace Unity.Kinematica.Editor
{
    abstract class Track : VisualElement
    {
        protected internal readonly Timeline m_Owner;
        protected TaggedAnimationClip m_TaggedClip;

        public TaggedAnimationClip Clip
        {
            get { return m_TaggedClip; }
        }

        protected Track(Timeline owner)
        {
            AddToClassList("track");
            m_Owner = owner;

            UIElementsUtils.ApplyStyleSheet(Timeline.k_Stylesheet, this);

            AddToClassList("trackElement");
        }

        protected void AddElement(ITimelineElement timelineElement)
        {
            if (timelineElement is VisualElement element)
            {
                Add(element);
            }
        }

        public virtual void SetClip(TaggedAnimationClip clip)
        {
            if (m_TaggedClip != null)
            {
                m_TaggedClip.DataChanged -= ReloadElements;
            }

            m_TaggedClip = clip;

            if (m_TaggedClip != null)
            {
                m_TaggedClip.DataChanged += ReloadElements;
            }
        }

        public virtual void OnAssetModified()
        {
        }

        public virtual void EnableEdit() {}
        public virtual void DisableEdit() {}

        public abstract void ReloadElements();

        public abstract void ResizeContents();
    }
}
