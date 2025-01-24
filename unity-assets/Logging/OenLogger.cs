using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OenLogger : MonoBehaviour
{
    public int backlogCount = 4;
    
    private OvertakingAnalyzer _overtaking;

    private List<(AutoSteer, OenLogObject)> loggers = new();
    private OenHandler l = new();
    private Transform _userCar;

    // Start is called before the first frame update
    void Start()
    {
        _overtaking = FindObjectOfType<OvertakingAnalyzer>();

        var logger = FindObjectOfType<CarLogger>();
        logger.RegisterLoggedObject(l, "OenHandler");
        for (int i = 0; i < backlogCount; i++)
        {
            var log = new OenLogObject();
            loggers.Add((null, log));
            logger.RegisterLoggedObject(log, "OenLogger" + i);
        }
        _userCar = FindObjectOfType<RCC_CarControllerV3>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        var cars = _overtaking.carsToTake;
        if (cars.Count > 0)
        {
            if (cars[0] != loggers[l.currentIndex].Item1)
            {
                l.currentIndex = (l.currentIndex + 1) % backlogCount;
            }
        
            for (int i = 0; i < cars.Count && i < loggers.Count; i++)
            {
                var index = (l.currentIndex + i) % backlogCount;
                var logObject = loggers[index];
                if (logObject.Item1 == cars[i]) continue;
                logObject.Item1 = cars[i];
                logObject.Item2.length = RecursiveGetBounds(cars[i].gameObject).size.x;
                logObject.Item2.id = cars[i].carId;
                loggers[index] = logObject;
            }
        }

        foreach (var tuple in loggers)
        {
            var oenCar = tuple.Item1;
            var logObject = tuple.Item2;
            if (oenCar == null)
            {
                logObject.dist = Single.NaN;
                logObject.speed = Single.NaN;
                logObject.id = -1;
            }
            else
            {
                var oenCarPos = oenCar.position;
                var oenCarFwd = oenCar.transform.forward;
                logObject.pos = oenCarPos;
                var oenToUser = _userCar.position - oenCarPos;
                logObject.dist = Mathf.Cos(Mathf.Deg2Rad * Vector3.SignedAngle(oenToUser, oenCarFwd, _userCar.transform.up)) *
                                       oenToUser.magnitude;
                logObject.speed = oenCar.velocity.magnitude;
                logObject.turn = oenCar.nextRoadTurn;
            }
        }
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

    private class OenLogObject
    {
        public Vector3 pos;
        public float dist;
        public float length;
        public float speed;
        public AutoSteer.TurnDirection turn;
        public int id;
    }

    private class OenHandler
    {
        public int currentIndex = 0;
    }
}
