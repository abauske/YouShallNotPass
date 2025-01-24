using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetSeatedPosition : MonoBehaviour
{
    [Tooltip("Desired head position of player when seated")]
    public GameObject desiredHeadPosition;
    public GameObject camera;

    private bool centered = false;
    private LoggingSubject l = new LoggingSubject();

    public float firstResetDelay = 1;

    private DateTime firstResetTime;

    void Start() {
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "ResetView");
    }
 
    // Update is called once per frame
    void Update () {
        if(firstResetTime == default) {
            firstResetTime = DateTime.Now.AddSeconds(firstResetDelay);
        }
        if (Input.GetKeyDown("joystick button 0") || Input.GetKeyDown(KeyCode.Space) || (!centered && DateTime.Now > firstResetTime))
        {
            centered = true;
            ResetSeatedPos(desiredHeadPosition.transform);
        }
    }
 
    private void ResetSeatedPos(Transform desiredHeadPos){
        Quaternion rotationDifference = desiredHeadPosition.transform.rotation * Quaternion.Inverse( camera.transform.rotation );
        transform.rotation = rotationDifference * transform.rotation;
        Vector3 positionDifference = desiredHeadPosition.transform.position - camera.transform.position;
        transform.position += positionDifference;
        l.count++;
        Debug.Log("Seted Position Resetted");
    }

    private class LoggingSubject {
        public int count = 0;
    }
}
