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
        [Tooltip("Layer used to prevent collisions between controller and ragdoll components. Set all skeleton parts to this layer")]
        public string ragdollLayer;

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
        [Tooltip("This is the active profile.")]
        public TraverserRagdollProfile currentProfile;
        [Tooltip("This profile will be overriden on calls to SetProfile and used to blend from previous to current.")]
        public TraverserRagdollProfile blendProfile;

        // --------------------------------

        // --- Private Variables ---

        private Animator animator;
        private float blendTime = 0.25f;
        private float currentBlendTime = 0.0f;
        private bool isBlending = false;

        // --------------------------------

        // --- Basic Methods ---

        private void Awake()
        {
            // --- Prevent collisions between ragdoll and controller ---
            Physics.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer(ragdollLayer), true);
        }

        void Start()
        {
            animator = GetComponent<Animator>();
            SetRagdollProfile(ref defaultProfile);
        }

        void Update()
        {
            if (isBlending)
                BlendProfile(Time.deltaTime);

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

        private void BlendProfile(float deltaTime)
        {
            // --- Update blending ---
            currentBlendTime += deltaTime;
            float normalizedBlendTime;

            if (Mathf.Approximately(blendTime, 0.0f))
                normalizedBlendTime = 1.0f;
            else
                normalizedBlendTime = currentBlendTime / blendTime;

            if (normalizedBlendTime >= 1.0f)
                isBlending = false;

            // --- Hips / Spine ---
            currentProfile.hipsRGJoint.BlendJointData(ref blendProfile.hipsRGJoint, normalizedBlendTime);
            currentProfile.spineRGJoint.BlendJointData(ref blendProfile.spineRGJoint, normalizedBlendTime);

            // --- Head ---
            currentProfile.headRGJoint.BlendJointData(ref blendProfile.headRGJoint, normalizedBlendTime);

            // --- Left arm ---
            currentProfile.leftArmRGJoint.BlendJointData(ref blendProfile.leftArmRGJoint, normalizedBlendTime);
            currentProfile.leftForeArmRGJoint.BlendJointData(ref blendProfile.leftForeArmRGJoint, normalizedBlendTime);
            currentProfile.leftHandRGJoint.BlendJointData(ref blendProfile.leftHandRGJoint, normalizedBlendTime);

            // --- Right arm ---
            currentProfile.rightArmRGJoint.BlendJointData(ref blendProfile.rightArmRGJoint, normalizedBlendTime);
            currentProfile.rightForeArmRGJoint.BlendJointData(ref blendProfile.rightForeArmRGJoint, normalizedBlendTime);
            currentProfile.rightHandRGJoint.BlendJointData(ref blendProfile.rightHandRGJoint, normalizedBlendTime);

            // --- Left leg ---
            currentProfile.leftUpperLegRGJoint.BlendJointData(ref blendProfile.leftUpperLegRGJoint, normalizedBlendTime);
            currentProfile.leftLowerLegRGJoint.BlendJointData(ref blendProfile.leftLowerLegRGJoint, normalizedBlendTime);

            // --- Right leg
            currentProfile.rightUpperLegRGJoint.BlendJointData(ref blendProfile.rightUpperLegRGJoint, normalizedBlendTime);
            currentProfile.rightLowerLegRGJoint.BlendJointData(ref blendProfile.rightLowerLegRGJoint, normalizedBlendTime);
        }

        private void AdjustRagdollComponent(ref TraverserRagdollJoint ragdollJoint, HumanBodyBones bone)
        {
            ragdollJoint.MoveRBPosition(animator.GetBoneTransform(bone).position);
        }

        public void SetRagdollProfile(ref TraverserRagdollProfile ragdollProfile)
        {
            // --- Update control variables ---
            isBlending = true;
            currentBlendTime = 0.0f;
            blendTime = ragdollProfile.blendTime;

            // --- Hips / Spine ---
            blendProfile.hipsRGJoint.SetJointData(ref ragdollProfile.hipsRGJoint);
            blendProfile.spineRGJoint.SetJointData(ref ragdollProfile.spineRGJoint);

            // --- Head ---
            blendProfile.headRGJoint.SetJointData(ref ragdollProfile.headRGJoint);

            // --- Left arm ---
            blendProfile.leftArmRGJoint.SetJointData(ref ragdollProfile.leftArmRGJoint);
            blendProfile.leftForeArmRGJoint.SetJointData(ref ragdollProfile.leftForeArmRGJoint);
            blendProfile.leftHandRGJoint.SetJointData(ref ragdollProfile.leftHandRGJoint);

            // --- Right arm ---
            blendProfile.rightArmRGJoint.SetJointData(ref ragdollProfile.rightArmRGJoint);
            blendProfile.rightForeArmRGJoint.SetJointData(ref ragdollProfile.rightForeArmRGJoint);
            blendProfile.rightHandRGJoint.SetJointData(ref ragdollProfile.rightHandRGJoint);

            // --- Left leg ---
            blendProfile.leftUpperLegRGJoint.SetJointData(ref ragdollProfile.leftUpperLegRGJoint);
            blendProfile.leftLowerLegRGJoint.SetJointData(ref ragdollProfile.leftLowerLegRGJoint);

            // --- Right leg
            blendProfile.rightUpperLegRGJoint.SetJointData(ref ragdollProfile.rightUpperLegRGJoint);
            blendProfile.rightLowerLegRGJoint.SetJointData(ref ragdollProfile.rightLowerLegRGJoint);


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
