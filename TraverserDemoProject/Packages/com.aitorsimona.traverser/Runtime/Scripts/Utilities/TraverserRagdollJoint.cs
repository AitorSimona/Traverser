using UnityEngine;

namespace Traverser
{

    public class TraverserRagdollJoint : MonoBehaviour
    {
        // --- Attributes ---

        [Range(0.0f, 1.0f)]
        [Tooltip("The amount of influence the ragdoll has on the final pose. Overriden by ragdoll profiles.")]
        public float weight = 0.0f;

        [Tooltip("The bone this ragdoll joint is going to mimick and modify.")]
        public Transform copyBone;

        // --------------------------------

        // --- Private Variables ---

        private ConfigurableJoint configJoint;
        private Rigidbody rb;

        private Quaternion initialRotation;
        private Quaternion previousLocal;

        private JointDrive angularXDrive;
        private JointDrive angularYZDrive;

        // --------------------------------

        // --- Basic Methods ---

        // Start is called before the first frame update
        void Start()
        {
            configJoint = GetComponent<ConfigurableJoint>();
            rb = GetComponent<Rigidbody>();
            initialRotation = copyBone.transform.localRotation;
            transform.localRotation = copyBone.localRotation;
            angularXDrive = new JointDrive();
            angularYZDrive = new JointDrive();
        }

        public void UpdateJoint()
        {
            copyBone.localRotation = previousLocal;
        }

        public void FixedUpdateJoint()
        {
            copyBone.localRotation = previousLocal;
            configJoint.targetRotation = Quaternion.Inverse(copyBone.localRotation) * initialRotation;
        }

        public void LateUpdateJoint()
        {
            previousLocal = copyBone.localRotation;
            copyBone.localRotation = Quaternion.Slerp(copyBone.localRotation, transform.localRotation, weight);
        }

        // --------------------------------

        public void MoveRBPosition(Vector3 position)
        {
            rb.MovePosition(position);
        }

        public void SetRagdollJoint(ref TraverserRagdollProfile.TraverserJointData jointData)
        {
            weight = jointData.weight;
            rb.drag = jointData.rbDrag;
            rb.angularDrag = jointData.rbAngularDrag;
            angularXDrive.positionSpring = jointData.angularXDrive.positionSpring;
            angularXDrive.positionDamper = jointData.angularXDrive.positionDamper;
            angularXDrive.maximumForce = jointData.angularXDrive.maximumForce;

            angularYZDrive.positionSpring = jointData.angularYZDrive.positionSpring;
            angularYZDrive.positionDamper = jointData.angularYZDrive.positionDamper;
            angularYZDrive.maximumForce = jointData.angularYZDrive.maximumForce;

            configJoint.angularXDrive = angularXDrive;
            configJoint.angularYZDrive = angularYZDrive;
        }
    }
}