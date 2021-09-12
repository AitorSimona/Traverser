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
            public float rbDrag;
            public float rbAngularDrag;
            public TraverserJointDrive angularXDrive;
            public TraverserJointDrive angularYZDrive;


            public void Initialize()
            {
                weight = 0.0f;
                rbDrag = 0.0f;
                rbAngularDrag = 0.0f;
                angularXDrive.Initialize();
                angularYZDrive.Initialize();
            }

            public void SetJointData(ref TraverserJointData jointData)
            {
                weight = jointData.weight;
                rbDrag = jointData.rbDrag;
                rbAngularDrag = jointData.rbAngularDrag;

                angularXDrive.positionSpring = jointData.angularXDrive.positionSpring;
                angularXDrive.positionDamper = jointData.angularXDrive.positionDamper;
                angularXDrive.maximumForce = jointData.angularXDrive.maximumForce;

                angularYZDrive.positionSpring = jointData.angularYZDrive.positionSpring;
                angularYZDrive.positionDamper = jointData.angularYZDrive.positionDamper;
                angularYZDrive.maximumForce = jointData.angularYZDrive.maximumForce;
            }

            public void BlendJointData(ref TraverserJointData jointData, float currentBlendNormalizedTime)
            {
                weight = Mathf.Lerp(weight, jointData.weight, currentBlendNormalizedTime);
                rbDrag = Mathf.Lerp(rbDrag, jointData.rbDrag, currentBlendNormalizedTime);
                rbAngularDrag = Mathf.Lerp(rbAngularDrag, jointData.rbAngularDrag, currentBlendNormalizedTime);
                
                angularXDrive.positionSpring = Mathf.Lerp(angularXDrive.positionSpring ,jointData.angularXDrive.positionSpring, currentBlendNormalizedTime);
                angularXDrive.positionDamper = Mathf.Lerp(angularXDrive.positionDamper, jointData.angularXDrive.positionDamper, currentBlendNormalizedTime);
                angularXDrive.maximumForce = Mathf.Lerp(angularXDrive.maximumForce, jointData.angularXDrive.maximumForce, currentBlendNormalizedTime);

                angularYZDrive.positionSpring = Mathf.Lerp(angularYZDrive.positionSpring, jointData.angularYZDrive.positionSpring, currentBlendNormalizedTime);
                angularYZDrive.positionDamper = Mathf.Lerp(angularYZDrive.positionDamper, jointData.angularYZDrive.positionDamper, currentBlendNormalizedTime);
                angularYZDrive.maximumForce = Mathf.Lerp(angularYZDrive.maximumForce, jointData.angularYZDrive.maximumForce, currentBlendNormalizedTime);
            }
        }

        [Tooltip("The time it will take to blend to this profile in seconds.")]
        [Range(0.0f, 1.0f)]
        public float blendTime;

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
