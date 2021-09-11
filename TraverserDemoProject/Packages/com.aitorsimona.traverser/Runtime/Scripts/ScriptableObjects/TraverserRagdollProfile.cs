using System;
using UnityEngine;

namespace Traverser
{
    [CreateAssetMenu(menuName = "Traverser/Scriptable Objects/TraverserRagdollProfile")]

    public class TraverserRagdollProfile : ScriptableObject
    {
        // --- Contains all data required by the ragdoll adjustment ---

        [Serializable]
        public struct TraverserJointDrive
        {
            public float positionSpring;
            public float positionDamper;
            public float maximumForce;

            public void Initialize()
            {
                positionSpring = 0.0f;
                positionDamper = 0.0f;
                maximumForce = 0.0f;
            }
        } 

        [Serializable]
        public struct TraverserJointData
        {
            [Range(0.0f, 1.0f)]
            public float weight;
            public TraverserJointDrive angularXDrive;
            public TraverserJointDrive angularYZDrive;

            public void Initialize()
            {
                weight = 0.0f;
                angularXDrive.Initialize();
                angularYZDrive.Initialize();
            }

            public void SetJointData(ref TraverserRagdollProfile.TraverserJointData jointData)
            {
                weight = jointData.weight;
                angularXDrive.positionSpring = jointData.angularXDrive.positionSpring;
                angularXDrive.positionDamper = jointData.angularXDrive.positionDamper;
                angularXDrive.maximumForce = jointData.angularXDrive.maximumForce;

                angularYZDrive.positionSpring = jointData.angularYZDrive.positionSpring;
                angularYZDrive.positionDamper = jointData.angularYZDrive.positionDamper;
                angularYZDrive.maximumForce = jointData.angularYZDrive.maximumForce;
            }
        }

        public TraverserJointData hipsRGJoint;
        public TraverserJointData spineRGJoint;

        public TraverserJointData headRGJoint;

        public TraverserJointData leftUpperLegRGJoint;
        public TraverserJointData leftLowerLegRGJoint;
        public TraverserJointData rightUpperLegRGJoint;
        public TraverserJointData rightLowerLegRGJoint;

        public TraverserJointData leftArmRGJoint;
        public TraverserJointData leftForeArmRGJoint;
        public TraverserJointData leftHandRGJoint;

        public TraverserJointData rightArmRGJoint;
        public TraverserJointData rightForeArmRGJoint;
        public TraverserJointData rightHandRGJoint;

    }
}
