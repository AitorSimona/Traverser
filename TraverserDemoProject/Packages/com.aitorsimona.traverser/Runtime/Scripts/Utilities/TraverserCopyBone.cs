using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TraverserCopyBone : MonoBehaviour
{
    public Transform copyBone;
    private ConfigurableJoint configJoint;

    private Quaternion initialRotation;

    // Start is called before the first frame update
    void Start()
    {
        configJoint = GetComponent<ConfigurableJoint>();
        initialRotation = copyBone.transform.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void FixedUpdate()
    {

    }

    private void LateUpdate()
    {
        configJoint.targetRotation = Quaternion.Inverse(copyBone.localRotation) * initialRotation;

    }
}
