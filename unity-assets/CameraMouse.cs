using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMouse : MonoBehaviour
{
    public float sensitivity;

    private void Start()
    {
        Application.targetFrameRate = 300;
    }

    void FixedUpdate ()
    {
        float rotateHorizontal = Input.GetAxis ("Mouse X");
        float rotateVertical = Input.GetAxis ("Mouse Y");
        Transform tr = transform;
        tr.Rotate(tr.up * (rotateHorizontal * sensitivity));
        tr.Rotate(tr.forward * (rotateVertical * sensitivity));
        
        // transform.position += new Vector3(-rotateHorizontal * sensitivity, rotateVertical * sensitivity, 0);
    }
}
