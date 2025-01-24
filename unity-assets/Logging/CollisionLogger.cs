using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionLogger : MonoBehaviour
{
    public float collisionResetMagnitude = 100;

    private LoggingSubject l = new LoggingSubject();
    private CrashText text;
    private float lastCrashTime = -100;
    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "CollisionLogger");
        text = FindObjectOfType<CrashText>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            l.manualCounter++;
            text.SetCrashed();
        }
    }

    void OnCollisionEnter(Collision collision) {
        var intensity = collision.impulse.magnitude;
        if(intensity < 10) {
            return;
        }
        l.collisionCounter++;
        l.collidedObjectPos = collision.transform.position;
        l.impulse = collision.impulse;
        l.type = GetType(collision);
        // Debug.Log("Collisionmag: " + intensity + " Collisiontype: " + l.type);
        if (intensity > collisionResetMagnitude && Time.time > lastCrashTime + 5)
        {
            lastCrashTime = Time.time;
            l.tooStrongCounter++;
            text.SetCrashed();
        }
    }

    private CollisionType GetType(Collision col) {
        var go = col.gameObject;
        if(go.name.Contains("Terrain")) {
            return CollisionType.Terrain;
        }

        for(int i = 0; i < 2; i++, go = go.transform.parent.gameObject) {
            if(go.name.Contains("Car")) {
                return CollisionType.Car;
            }
        }
        return CollisionType.Unknown;
    }

    private class LoggingSubject {
        public int collisionCounter = 0;
        public int tooStrongCounter = 0;
        public int manualCounter = 0;
        public Vector3 collidedObjectPos;
        public Vector3 impulse;
        public CollisionType type;
    }

    private enum CollisionType {
        Car,
        Terrain,
        Unknown
    }
}
