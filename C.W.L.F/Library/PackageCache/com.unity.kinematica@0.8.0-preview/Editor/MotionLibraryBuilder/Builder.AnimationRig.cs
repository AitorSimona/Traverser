namespace Unity.Kinematica.Editor
{
    internal partial class Builder
    {
        void BuildAnimationRig()
        {
            ref Binary binary = ref Binary;

            var joints = rig.Joints;

            int numJoints = joints.Length;

            allocator.Allocate(numJoints,
                ref binary.animationRig.bindPose);

            for (int i = 0; i < numJoints; ++i)
            {
                int nameIndex = stringTable.RegisterString(joints[i].name);

                binary.animationRig.bindPose[i].localTransform = joints[i].localTransform;
                binary.animationRig.bindPose[i].parentIndex = joints[i].parentIndex;
                binary.animationRig.bindPose[i].nameIndex = nameIndex;
            }
        }
    }
}
