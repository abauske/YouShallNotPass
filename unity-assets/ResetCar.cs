using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Map;

public class ResetCar : MonoBehaviour
{
    public MapStud map;
    public Navigation navigator;

    private NavigationPath navpath;
    private Rigidbody rb;
    private LoggingSubject l = new LoggingSubject();

    void Start() {
        navigator.AddChangeListener(PlanChange);
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "ResetCar");
        rb = GetComponent<Rigidbody>();
    }
 
    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown("joystick button 1") || Input.GetKeyDown(KeyCode.R))
        {
            l.byButton++;
            DoReset();
        }
    }

    public void DoReset() {
        MappedRoad road;
        float prg;
        if(navpath != null && navpath.Count >= 1) {
            prg = navpath[0].StartProgress;
            road = navpath[0].Road;
        } else {
            var (r, pos, p) = map.GetClosestRoad(transform.position);
            road = r;
            prg = p;
        }
        
        var lane = road.lanes[0];
        transform.position = lane.GetPos(prg, true) + Vector3.up;
        transform.forward = lane.GetDirection(prg, true);
        l.count++;

        rb.angularVelocity = Vector3.zero;
        rb.velocity = Vector3.zero;
    }

    void PlanChange(NavigationPath path) {
        navpath = path;
    }

    private class LoggingSubject {
        public int count = 0;
        public int byButton = 0;
    }
}
