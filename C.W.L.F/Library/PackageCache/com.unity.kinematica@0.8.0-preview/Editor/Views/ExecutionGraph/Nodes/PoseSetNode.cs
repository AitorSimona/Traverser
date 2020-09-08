using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.UIElements;

namespace Unity.Kinematica.Editor
{
    [GraphNode(typeof(PoseSet))]
    internal class PoseSetNode : GraphNode
    {
        public override string Title
        {
            get
            {
                using (PoseSet poseSet = GetDebugObject<PoseSet>())
                {
                    if (poseSet.debugName.LengthInBytes > 0)
                    {
                        return $"{poseSet.debugName.ToString()} poses";
                    }
                }

                return kPoseSetTitle;
            }
        }

        static readonly string kPoseSetTitle = "Pose Set";
    }
}
