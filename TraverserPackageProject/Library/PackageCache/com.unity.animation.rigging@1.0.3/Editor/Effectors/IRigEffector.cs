using UnityEngine;

namespace UnityEditor.Animations.Rigging
{
    internal interface IRigEffector
    {
        Transform transform { get; }
        bool visible { get; set; }

        void OnSceneGUI();
    }
}
