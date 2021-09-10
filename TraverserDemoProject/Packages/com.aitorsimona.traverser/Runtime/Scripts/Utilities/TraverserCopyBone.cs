using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraverserCopyBone : MonoBehaviour
{
    public Transform copyBone;
    private ConfigurableJoint configJoint;
    private Rigidbody rigidbody;

    private Quaternion initialRotation;
    private Quaternion previousLocal;

    public float adaptSpeed = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
        configJoint = GetComponent<ConfigurableJoint>();
        rigidbody = GetComponent<Rigidbody>();
        initialRotation = copyBone.transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void FixedUpdate()
    {
        copyBone.localRotation = previousLocal;
        //rigidbody.MovePosition(copyBone.position);
        configJoint.targetRotation = Quaternion.Inverse(copyBone.localRotation) * initialRotation;

    }

    private void LateUpdate()
    {
        previousLocal = copyBone.localRotation;
        copyBone.rotation = Quaternion.Slerp(copyBone.rotation, transform.rotation, Time.deltaTime* adaptSpeed);

        //rigidbody.MoveRotation(copyBone.rotation);
    }
}
