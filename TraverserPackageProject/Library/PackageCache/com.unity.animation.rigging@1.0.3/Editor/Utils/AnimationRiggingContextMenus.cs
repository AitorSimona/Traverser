using UnityEngine;

namespace UnityEditor.Animations.Rigging
{
    internal static class AnimationRiggingContextMenus
    {
        [MenuItem("CONTEXT/Animator/Rig Setup", false, 611)]
        static void RigSetup(MenuCommand command)
        {
            var animator = command.context as Animator;

            AnimationRiggingEditorUtils.RigSetup(animator.transform);
        }

        [MenuItem("CONTEXT/Animator/Bone Renderer Setup", false, 612)]
        static void BoneRendererSetup(MenuCommand command)
        {
            var animator = command.context as Animator;

            AnimationRiggingEditorUtils.BoneRendererSetup(animator.transform);
        }
    }
}
