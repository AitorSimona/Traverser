using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FerrisWheel : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees.")]
    public float rotationSpeed = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(rotationSpeed, transform.forward) * transform.rotation, Time.deltaTime);
    }
}
