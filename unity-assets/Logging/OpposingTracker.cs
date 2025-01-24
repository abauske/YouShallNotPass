using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OpposingTracker : MonoBehaviour
{
    public float maxTrackingDistance = 500;
    
    private CarSpawner _carsProvider;
    private MapStud _map;
    private float _time;
    private LoggingSubject l = new LoggingSubject();
    private AutoSteer opposingCar;
    private NavigationPath _navpath;

    // Start is called before the first frame update
    void Start()
    {
        _carsProvider = FindObjectOfType<CarSpawner>();
        _map = FindObjectOfType<MapStud>();
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "OpposingTracker");
        FindObjectOfType<Navigation>().AddChangeListener(path => _navpath = path);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (opposingCar == null)
        {
            return;
        }
        Handles.SphereHandleCap(3, opposingCar.transform.position, Quaternion.identity, 8, EventType.Repaint);
    }
#endif

    // Update is called once per frame
    void Update()
    {
        var myPos = transform.position;
        
        _time += Time.deltaTime;
        if (_time >= 1 && _navpath != null && _navpath.Count >= 1)
        {
            _time -= 1;

            var myRoad = _navpath[0].Road;
            var myRoadPrg = myRoad.GetProgressFromPosition(myPos);
            
            opposingCar = null;
            float closestDist = float.PositiveInfinity;
            foreach (var car in _carsProvider.AllCars)
            {
                var carPos = car.transform.position;
                var delta = carPos - myPos;
                var dist = delta.sqrMagnitude;
                if (dist > maxTrackingDistance * maxTrackingDistance || dist > closestDist || dist < 0.5)
                {
                    continue;
                }
            
                var (road, _, roadPrg) = _map.GetClosestRoad(carPos);

                if (road == myRoad.oppositeDirection)
                {
                    road = myRoad;
                    roadPrg = myRoad.Length - roadPrg;
                }

                if (road != myRoad || roadPrg < myRoadPrg)
                {
                    continue;
                }

                var roadForward = road.CenterSpline.GetDirection(roadPrg);
                
                if (Vector3.Angle(car.transform.forward, roadForward) < 90)
                {
                    continue;
                }

                opposingCar = car;
                closestDist = dist;
            }
            
            l.opposingLength = opposingCar != null ? RecursiveGetBounds(opposingCar.gameObject).size.x : Single.NaN;
        }
        
        if (opposingCar == null)
        {
            l.opposingDist = Single.NaN;
            return;
        }

        var oppPos = opposingCar.transform.position;
        l.opposingPos = oppPos;
        l.opposingDist = (myPos - oppPos).magnitude;
        l.opposingSpeed = opposingCar.Rb.velocity.magnitude;
    }
    
    private Bounds RecursiveGetBounds(GameObject start)
    {
        Bounds b = new Bounds();
        foreach (Transform child in start.transform)
        {
            b.Encapsulate(RecursiveGetBounds(child.gameObject));
        }
        var coll = start.GetComponent<MeshFilter>();
        if (coll != null)
        {
            b.Encapsulate(coll.mesh.bounds);
        }

        return b;
    }

    private class LoggingSubject
    {
        public Vector3 opposingPos;
        public float opposingDist;
        public float opposingSpeed;
        public float opposingLength;
    }
}
