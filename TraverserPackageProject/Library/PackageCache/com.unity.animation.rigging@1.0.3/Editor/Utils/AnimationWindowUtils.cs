using UnityEngine;

namespace UnityEditor.Animations.Rigging
{
    [InitializeOnLoad]
    static class AnimationWindowUtils
    {
        static AnimationWindow m_AnimationWindow = null;

        public static AnimationWindow animationWindow
        {
            get
            {
                if (m_AnimationWindow == null)
                    m_AnimationWindow = FindWindowOpen();

                return m_AnimationWindow;
            }
        }

        public static AnimationClip activeAnimationClip
        {
            get
            {
                if (animationWindow != null)
                    return animationWindow.animationClip;

                return null;
            }
            set
            {
                if (animationWindow != null)
                    animationWindow.animationClip = value;
            }
        }

        public static void StartPreview()
        {
            if (animationWindow != null)
                animationWindow.previewing = true;
        }

        public static void StopPreview()
        {
            if (animationWindow != null)
                animationWindow.previewing = false;
        }

        public static bool isPreviewing
        {
            get
            {
                if (animationWindow != null)
                    return animationWindow.previewing;

                return false;
            }
        }

        // This does not check if there is an AnimationClip to play
        public static bool canPreview
        {
            get
            {
                if (animationWindow != null)
                    return animationWindow.canPreview;

                return false;
            }
        }

        static AnimationWindow FindWindowOpen()
        {
            UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll(typeof(AnimationWindow));

            foreach (UnityEngine.Object o in objs)
            {
                if (o.GetType() == typeof(AnimationWindow))
                    return (AnimationWindow)o;
            }

            return null;
        }
    }
}
