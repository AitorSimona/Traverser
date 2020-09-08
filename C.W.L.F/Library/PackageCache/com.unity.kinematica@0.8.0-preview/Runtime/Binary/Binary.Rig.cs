using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Kinematica
{
    //
    // Motion synthesizer binary resource
    //

    public partial struct Binary
    {
        //
        // Animation rig
        //

        internal struct AnimationRig
        {
            public struct Joint
            {
                public int nameIndex;
                public int parentIndex;
                public AffineTransform localTransform;
            }

            public int NumJoints
            {
                get
                {
                    return bindPose.Length;
                }
            }

            public int GetParentJointIndex(int index)
            {
                Assert.IsTrue(index < NumJoints);
                return bindPose[index].parentIndex;
            }

            public int GetJointIndexForNameIndex(int nameIndex)
            {
                for (int i = 0; i < bindPose.Length; ++i)
                {
                    if (bindPose[i].nameIndex == nameIndex)
                    {
                        return i;
                    }
                }

                return -1;
            }

            public BlobArray<Joint> bindPose;
        }

        /// <summary>
        /// Returns the number of animation rig joints.
        /// </summary>
        public int numJoints => animationRig.NumJoints;

        internal void DebugDrawAnimationRig()
        {
            AffineTransform[] transforms = new AffineTransform[numJoints];
            for (int i = 0; i < numJoints; ++i)
            {
                ref AnimationRig.Joint joint = ref animationRig.bindPose[i];

                if (joint.parentIndex >= 0)
                {
                    AffineTransform characterSpaceTransform =
                        transforms[joint.parentIndex] * joint.localTransform;

                    transforms[i] = characterSpaceTransform;

                    Debug.DrawLine(characterSpaceTransform.t,
                        transforms[joint.parentIndex].t,
                        Color.white, 0.0f, false);
                }
                else
                {
                    transforms[i] = joint.localTransform;
                }
            }
        }
    }
}
