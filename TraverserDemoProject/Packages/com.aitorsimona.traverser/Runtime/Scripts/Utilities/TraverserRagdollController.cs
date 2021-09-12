using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Traverser
{
    public class TraverserRagdollController : MonoBehaviour
    {
        // --- Attributes ---

        [Tooltip("When set to false, ragdoll weight will be set to 0.")]
        public bool active = true;

        [Header("Ragdoll Joints")]
        public TraverserRagdollJoint hipsRGJoint;
        public TraverserRagdollJoint spineRGJoint;
        public TraverserRagdollJoint headRGJoint;
        public TraverserRagdollJoint leftArmRGJoint;
        public TraverserRagdollJoint leftForeArmRGJoint;
        public TraverserRagdollJoint leftHandRGJoint;
        public TraverserRagdollJoint rightArmRGJoint;
        public TraverserRagdollJoint rightForeArmRGJoint;
        public TraverserRagdollJoint rightHandRGJoint;
        public TraverserRagdollJoint leftUpperLegRGJoint;
        public TraverserRagdollJoint leftLowerLegRGJoint;
        public TraverserRagdollJoint rightUpperLegRGJoint;
        public TraverserRagdollJoint rightLowerLegRGJoint;

        [Header("Ragdoll Profiles")]
        [Tooltip("This profile will be loaded at startup if ragdoll controller is enabled.")]
        public TraverserRagdollProfile defaultProfile;
        [Tooltip("This profile will be overriden on calls to SetProfile.")]
        public TraverserRagdollProfile currentProfile;

        // --------------------------------

        // --- Private Variables ---

        private Animator animator;

        // --------------------------------

        // --- Basic Methods ---

        void Start()
        {
            animator = GetComponent<Animator>();
            SetRagdollProfile(ref defaultProfile);
        }

        void Update()
        {
            if (active)
                UpdateRagdoll(ref currentProfile);
            else
                DeactivateRagdoll();

            hipsRGJoint.UpdateJoint();
            spineRGJoint.UpdateJoint();
            headRGJoint.UpdateJoint();
            leftArmRGJoint.UpdateJoint();
            leftForeArmRGJoint.UpdateJoint();
            leftHandRGJoint.UpdateJoint();
            rightArmRGJoint.UpdateJoint();
            rightForeArmRGJoint.UpdateJoint();
            rightHandRGJoint.UpdateJoint();
            leftUpperLegRGJoint.UpdateJoint();
            leftLowerLegRGJoint.UpdateJoint();
            rightUpperLegRGJoint.UpdateJoint();
            rightLowerLegRGJoint.UpdateJoint();
        }

        private void FixedUpdate()
        {
            hipsRGJoint.FixedUpdateJoint();
            spineRGJoint.FixedUpdateJoint();
            headRGJoint.FixedUpdateJoint();
            leftArmRGJoint.FixedUpdateJoint();
            leftForeArmRGJoint.FixedUpdateJoint();
            leftHandRGJoint.FixedUpdateJoint();
            rightArmRGJoint.FixedUpdateJoint();
            rightForeArmRGJoint.FixedUpdateJoint();
            rightHandRGJoint.FixedUpdateJoint();
            leftUpperLegRGJoint.FixedUpdateJoint();
            leftLowerLegRGJoint.FixedUpdateJoint();
            rightUpperLegRGJoint.FixedUpdateJoint();
            rightLowerLegRGJoint.FixedUpdateJoint();
        }

        private void LateUpdate()
        {
            // --- Hips / Spine ---
            AdjustRagdollComponent(ref hipsRGJoint, HumanBodyBones.Hips);
            AdjustRagdollComponent(ref spineRGJoint, HumanBodyBones.Spine);

            // --- Head ---
            AdjustRagdollComponent(ref headRGJoint, HumanBodyBones.Head);

            // --- Left arm ---
            AdjustRagdollComponent(ref leftArmRGJoint, HumanBodyBones.LeftUpperArm);
            AdjustRagdollComponent(ref leftForeArmRGJoint, HumanBodyBones.LeftLowerArm);
            AdjustRagdollComponent(ref leftHandRGJoint, HumanBodyBones.LeftHand);

            // --- Right arm ---
            AdjustRagdollComponent(ref rightArmRGJoint, HumanBodyBones.RightUpperArm);
            AdjustRagdollComponent(ref rightForeArmRGJoint, HumanBodyBones.RightLowerArm);
            AdjustRagdollComponent(ref rightHandRGJoint, HumanBodyBones.RightHand);

            // --- Left leg ---
            AdjustRagdollComponent(ref leftUpperLegRGJoint, HumanBodyBones.LeftUpperLeg);
            AdjustRagdollComponent(ref leftLowerLegRGJoint, HumanBodyBones.LeftLowerLeg);

            // --- Right leg
            AdjustRagdollComponent(ref rightUpperLegRGJoint, HumanBodyBones.RightUpperLeg);
            AdjustRagdollComponent(ref rightLowerLegRGJoint, HumanBodyBones.RightLowerLeg);

            hipsRGJoint.LateUpdateJoint();
            spineRGJoint.LateUpdateJoint();
            headRGJoint.LateUpdateJoint();
            leftArmRGJoint.LateUpdateJoint();
            leftForeArmRGJoint.LateUpdateJoint();
            leftHandRGJoint.LateUpdateJoint();
            rightArmRGJoint.LateUpdateJoint();
            rightForeArmRGJoint.LateUpdateJoint();
            rightHandRGJoint.LateUpdateJoint();
            leftUpperLegRGJoint.LateUpdateJoint();
            leftLowerLegRGJoint.LateUpdateJoint();
            rightUpperLegRGJoint.LateUpdateJoint();
            rightLowerLegRGJoint.LateUpdateJoint();
        }

        // --------------------------------

        // --- Utility Methods ---

        private void AdjustRagdollComponent(ref TraverserRagdollJoint ragdollJoint, HumanBodyBones bone)
        {
            ragdollJoint.MoveRBPosition(animator.GetBoneTransform(bone).position);
        }

        public void SetRagdollProfile(ref TraverserRagdollProfile ragdollProfile)
        {
            // --- Hips / Spine ---
            currentProfile.hipsRGJoint.SetJointData(ref ragdollProfile.hipsRGJoint);
            currentProfile.spineRGJoint.SetJointData(ref ragdollProfile.spineRGJoint);

            // --- Head ---
            currentProfile.headRGJoint.SetJointData(ref ragdollProfile.headRGJoint);

            // --- Left arm ---
            currentProfile.leftArmRGJoint.SetJointData(ref ragdollProfile.leftArmRGJoint);
            currentProfile.leftForeArmRGJoint.SetJointData(ref ragdollProfile.leftForeArmRGJoint);
            currentProfile.leftHandRGJoint.SetJointData(ref ragdollProfile.leftHandRGJoint);

            // --- Right arm ---
            currentProfile.rightArmRGJoint.SetJointData(ref ragdollProfile.rightArmRGJoint);
            currentProfile.rightForeArmRGJoint.SetJointData(ref ragdollProfile.rightForeArmRGJoint);
            currentProfile.rightHandRGJoint.SetJointData(ref ragdollProfile.rightHandRGJoint);

            // --- Left leg ---
            currentProfile.leftUpperLegRGJoint.SetJointData(ref ragdollProfile.leftUpperLegRGJoint);
            currentProfile.leftLowerLegRGJoint.SetJointData(ref ragdollProfile.leftLowerLegRGJoint);

            // --- Right leg
            currentProfile.rightUpperLegRGJoint.SetJointData(ref ragdollProfile.rightUpperLegRGJoint);
            currentProfile.rightLowerLegRGJoint.SetJointData(ref ragdollProfile.rightLowerLegRGJoint);
        }

        public void SetDefaultRagdollProfile()
        {
            SetRagdollProfile(ref defaultProfile);
        }

        private void UpdateRagdoll(ref TraverserRagdollProfile ragdollProfile)
        {
            // --- Hips / Spine ---
            hipsRGJoint.SetRagdollJoint(ref ragdollProfile.hipsRGJoint);
            spineRGJoint.SetRagdollJoint(ref ragdollProfile.spineRGJoint);

            // --- Head ---
            headRGJoint.SetRagdollJoint(ref ragdollProfile.headRGJoint);

            // --- Left arm ---
            leftArmRGJoint.SetRagdollJoint(ref ragdollProfile.leftArmRGJoint);
            leftForeArmRGJoint.SetRagdollJoint(ref ragdollProfile.leftForeArmRGJoint);
            leftHandRGJoint.SetRagdollJoint(ref ragdollProfile.leftHandRGJoint);

            // --- Right arm ---
            rightArmRGJoint.SetRagdollJoint(ref ragdollProfile.rightArmRGJoint);
            rightForeArmRGJoint.SetRagdollJoint(ref ragdollProfile.rightForeArmRGJoint);
            rightHandRGJoint.SetRagdollJoint(ref ragdollProfile.rightHandRGJoint);

            // --- Left leg ---
            leftUpperLegRGJoint.SetRagdollJoint(ref ragdollProfile.leftUpperLegRGJoint);
            leftLowerLegRGJoint.SetRagdollJoint(ref ragdollProfile.leftLowerLegRGJoint);

            // --- Right leg
            rightUpperLegRGJoint.SetRagdollJoint(ref ragdollProfile.rightUpperLegRGJoint);
            rightLowerLegRGJoint.SetRagdollJoint(ref ragdollProfile.rightLowerLegRGJoint);
        }

        private void DeactivateRagdoll()
        {
            // --- Hips / Spine ---
            hipsRGJoint.weight = 0.0f;
            spineRGJoint.weight = 0.0f;

            // --- Head ---
            headRGJoint.weight = 0.0f;

            // --- Left arm ---
            leftArmRGJoint.weight = 0.0f;
            leftForeArmRGJoint.weight = 0.0f;
            leftHandRGJoint.weight = 0.0f;

            // --- Right arm ---
            rightArmRGJoint.weight = 0.0f;
            rightForeArmRGJoint.weight = 0.0f;
            rightHandRGJoint.weight = 0.0f;

            // --- Left leg ---
            leftUpperLegRGJoint.weight = 0.0f;
            leftLowerLegRGJoint.weight = 0.0f;

            // --- Right leg
            rightUpperLegRGJoint.weight = 0.0f;
            rightLowerLegRGJoint.weight = 0.0f;
        }

        // --------------------------------
    }
}
