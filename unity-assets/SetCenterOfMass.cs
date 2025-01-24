using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetCenterOfMass : MonoBehaviour
{
    [Tooltip("Sets the center of mass relative to the transform's position and rotation, but will not reflect the transform's scale!")]
    public Vector3 centerOfMassOffset;
    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMassOffset;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
