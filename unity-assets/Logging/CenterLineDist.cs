using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CenterLineDist : MonoBehaviour
{
    private class CenterLineDistSubject
    {
        public float distToCenter;
        public float RoadPrg;
        public float RoadLength;
    }
    
    private class SightSubject
    {
        public float availSight;
    }
    
    public Navigation pathPlanner;

    public float DistToCenterLine => log.distToCenter;

    private CenterLineDistSubject log = new CenterLineDistSubject();
    private SightSubject sightLog = new SightSubject();
    private NavigationPath navPath = null;
    private float pathUpdateTime = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        FindObjectOfType<CarLogger>().RegisterLoggedObject(log, "leftFrontWheel");
        FindObjectOfType<CarLogger>().RegisterLoggedObject(sightLog, "Sight");
        pathPlanner.AddChangeListener(Pathchange);
    }

    // Update is called once per frame
    void Update()
    {
        if (navPath != null && navPath.Count > 0)
        {
            var sec = navPath[0];
            var objectPos = transform.position;
            var prg = sec.Road.GetProgressFromPosition(objectPos, 1, sec.StartProgress - 10, sec.StartProgress + 10 + 60 * (Time.time - pathUpdateTime));
            log.RoadPrg = prg;
            log.RoadLength = sec.Road.Length;
            var forward = sec.Road.CenterSpline.GetDirection(prg);
            var pos = sec.Road.Pos(prg);
            var a = pos + forward - pos;
            var b = objectPos - pos;
            b.y = 0;
            a.y = 0;
            bool isLeft = -a.x * b.z + a.z * b.x < 0;
            log.distToCenter = b.magnitude;
            if (isLeft)
            {
                log.distToCenter = -log.distToCenter;
            }

            sightLog.availSight = sec.Road.sightDistance.Eval(prg);
        }
        else
        {
            log.distToCenter = Single.NaN;
            sightLog.availSight = Single.NaN;
        }
    }

    void Pathchange(NavigationPath path)
    {
        navPath = path;
        pathUpdateTime = Time.time;
    }
}
