using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Map;
using Valve.VR.InteractionSystem;
using Random = Unity.Mathematics.Random;

public class AutoSteer : MonoBehaviour
{

    public float lookahead = 10;
    public float steeringFactor = 1.2f;
    public float futureSteeringFactor = 20f;
    public float speedFactor = 100;
    public float speedFactorI = 0.1f;
    public float brakeFactor = 200;
    public float speedSteerFactor = 0.1f;
    public float desiredVelKmh = 75;
    public float maxStraigthAngle = 30;
    public float nextRoadAsessmentDist = 10;
    public float leftTurnContDist = 1.9f;
    public float parkOffset = 3;
    public float intersectionDecelFactor = 4;
    public float desiredVel => desiredVelKmh / 3.6f;
    
    [HideInInspector] public float parkForSecs = 0;
    private float parkedForSecs = 0;

    private Rigidbody _rb;
    public Rigidbody Rb => _rb;
    public Vector3 position
    {
        get => transform.position;
        set => transform.position = value;
    }

    public Vector3 velocity => _rb.velocity;
    private MappedRoad _currentRoad;
    private MappedRoad _nextRoad;
    private TurnDirection _nextRoadTurn = TurnDirection.INVALID;
    public TurnDirection nextRoadTurn => _nextRoadTurn;
    private IntersectionType _nextIntType = IntersectionType.INVALID;
    private int _currentLane;
    private MapStud _map;
    private float _roadPrg;
    private WheelCollider[] _wheels;
    private WheelCollider[] _frontwheels;
    private Random _rand;
    private Rigidbody _userCar;
    private float _waitedAtIntersection = 0;
    private float _speedI = 0;
    private float modifiedSpeedSetpoint = 0;
    private float modifiedSpeedRemaining = 0;
    private CenterLineDist _centerLineDist;

    [HideInInspector] public int carId;

    // Start is called before the first frame update
    void Start()
    {
        _rand = new Random((uint) FindObjectOfType<StudyParameters>().Mod + 100);
        _rb = GetComponent<Rigidbody>();
        _map = FindObjectOfType<MapStud>();
        _wheels = GetComponentsInChildren<WheelCollider>();
        _frontwheels = _wheels.Where(w => w.gameObject.name.Contains("Front")).ToArray();
        _userCar = FindObjectOfType<RCC_CarControllerV3>().GetComponent<Rigidbody>();
        _centerLineDist = FindObjectOfType<CenterLineDist>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var pos = transform.position;
        if(_currentRoad == null) {
            SetRoadToFollow(_map.GetClosestRoad(pos).Item1);
        }
        
        if (parkForSecs < -10 && _rb.velocity.sqrMagnitude < 4 * 4 && _waitedAtIntersection < -10)
        {
            transform.position += new Vector3(10000, 0, 0);
            return;
        }
        
        var lane = _currentRoad.lanes[_currentLane];
        _roadPrg = lane.GetProgressByCloseProgress(pos, _roadPrg);

        var lanePos = lane.GetPos(_roadPrg);
        lanePos.y += 2;
        var laneDir = lane.GetDirection(_roadPrg);
        Debug.DrawLine(lanePos, lanePos + new Vector3(laneDir.z, laneDir.y, -laneDir.x) * 5, Color.magenta);
        
        Vector3 lookoutPoint = Vector3.zero;
        if (_roadPrg > lane.EndProgress - 100 && _nextRoad == null)
        {
            var optionsList = _currentRoad.endIntersectionNextRoads;
            if (optionsList.Count < 1)
            {
                optionsList = new List<MappedRoad> { _currentRoad.oppositeDirection };
            }

            _nextRoad = optionsList[_rand.NextInt(optionsList.Count)];
            _nextRoadTurn = GetTurnType(_currentRoad, _nextRoad);
            _nextIntType = GetIntType(_currentRoad);
        }
        
        if (_roadPrg > lane.EndProgress - nextRoadAsessmentDist && _nextRoad != null)
        {
            var angleToNext = Vector3.SignedAngle(transform.forward,
                _nextRoad.CenterSpline.GetDirection(_nextRoad.CenterSpline.StartProgress), transform.up);
            bool continueToNextRoad = true;
            if (angleToNext < -maxStraigthAngle)
            {
                // left turn - turn late
                lookoutPoint = lane.GetPos(lane.EndProgress + leftTurnContDist);
                if ((pos - lookoutPoint).magnitude > 2)
                {
                    continueToNextRoad = false;
                }
            }

            if (continueToNextRoad)
            {
                var nextLaneIndex = _nextRoad.lanes.Count - 1;
                if (_waitedAtIntersection < -0.1f)
                {
                    SetRoadToFollow(_nextRoad, nextLaneIndex);
                    FixedUpdate();
                    return;
                }

                lookoutPoint = _nextRoad.lanes[nextLaneIndex].GetPos(0, true);
            }
        }
        else if(parkForSecs <= 0)
        {
            lookoutPoint = lane.GetPos(Mathf.Min(lane.EndProgress, _roadPrg + lookahead));
            parkForSecs -= Time.deltaTime;
            parkedForSecs = 0;
        } else if((_centerLineDist.DistToCenterLine < -0.5f && parkedForSecs <= 2.2f) || lane.EndProgress - _roadPrg < 150) {
            lookoutPoint = lane.GetPos(Mathf.Min(lane.EndProgress, _roadPrg + lookahead));
            parkForSecs = 0;
            parkedForSecs = 0;
        }
        else
        {
            lookoutPoint = lane.GetPos(Mathf.Min(lane.EndProgress, _roadPrg + lookahead));
            var forward = lane.GetDirection(Mathf.Min(lane.EndProgress, _roadPrg + lookahead));
            lookoutPoint = Vector3.Cross(Vector3.up, forward.normalized).normalized * parkOffset + lookoutPoint;
            parkForSecs -= Time.deltaTime;
            parkedForSecs += Time.deltaTime;
        }

        var lookDelta = lookoutPoint - pos;
        var angle = Vector3.SignedAngle(transform.forward, lookDelta, transform.up);
        var steerAngle = Mathf.Min(60, Mathf.Max(-60, angle + steeringFactor));

        // velocity
        var vel = _rb.velocity.magnitude;
        var err = GetVelocitySetpoint(steerAngle) - vel;
        var brakeP = err < 0 ? -err * brakeFactor : 0;
        var accP = err > 0 ? err * speedFactor : err * brakeFactor;
        _speedI += err * speedFactorI;
        // Debug.DrawLine(transform.position, transform.position + transform.forward * 5, Color.magenta);
        
        _wheels.ForEach(x => x.motorTorque = accP + _speedI);
        _wheels.ForEach(x => x.brakeTorque = Mathf.Max(0, brakeP - _speedI));
        _frontwheels.ForEach(x => x.steerAngle = steerAngle);
    }

    private TurnDirection GetTurnType(MappedRoad cur, MappedRoad next)
    {
        var curDir = cur.CenterSpline.GetDirection(cur.Length);
        var nextDir = next.CenterSpline.GetDirection(10);
        var turnAngle = Vector3.SignedAngle(curDir, nextDir, Vector3.up);
        return turnAngle > 45
            ? TurnDirection.RIGHT
            : (turnAngle < -45 ? TurnDirection.LEFT : TurnDirection.STRAIGHT);
    }

    private IntersectionType GetIntType(MappedRoad currentRoad)
    {
        var next = currentRoad.endIntersectionNextRoads;
        if(next.Count != 2) return IntersectionType.INVALID;

        foreach (var r in next)
        {
            if (GetTurnType(currentRoad, r) == TurnDirection.STRAIGHT) return IntersectionType.STRAIGHT;
        }

        return IntersectionType.END;
    }

    private float GetVelocitySetpoint(float steerAngle)
    {
        GameObject closest = null;
        float closestDist = Single.PositiveInfinity;
        foreach (var car in _currentRoad.cars)
        {
            if(car == this || car.parkedForSecs > 1) continue;
            UpdateClosestCar(car.gameObject, ref closestDist, ref closest);
        }
        
        var lane = _currentRoad.lanes[_currentLane];
        if (_roadPrg > lane.EndProgress - nextRoadAsessmentDist || _roadPrg < lane.StartProgress + nextRoadAsessmentDist)
        {
            foreach (var r in _currentRoad.endIntersectionNextRoads)
            {
                foreach (var car in r.cars)
                {
                    UpdateClosestCar(car.gameObject, ref closestDist, ref closest);
                }
            }
        }

        UpdateClosestCar(_userCar.gameObject, ref closestDist, ref closest);

        float intersectionSpeed = Mathf.Max((lane.EndProgress - _roadPrg) /
            (_nextRoadTurn == TurnDirection.STRAIGHT ? 1 : intersectionDecelFactor) + 8, 8);
        _waitedAtIntersection -= Time.deltaTime;
        if (NeedsWaitingAtIntersection())
        {
            intersectionSpeed = (lane.EndProgress - _roadPrg) / 1.5f - 8;
            _waitedAtIntersection = 0;
        }
        
        float parkSpeed = parkedForSecs > 2 ? 0 : Single.PositiveInfinity;
        float targetV = desiredVel;
        if (modifiedSpeedRemaining > 0)
        {
            targetV = modifiedSpeedSetpoint;
            modifiedSpeedRemaining -= Time.deltaTime;
        }

        var curSteerSpeed = targetV - Mathf.Abs(steerAngle) * speedSteerFactor;
        var futureSteerSpeed = targetV / Mathf.Max(0.01f, _currentRoad.curvature.Eval(Mathf.Min(_roadPrg + 20, _currentRoad.Length))) * futureSteeringFactor;
        return Mathf.Min(curSteerSpeed, futureSteerSpeed, (closestDist - 10) * 2 / 3.6f, intersectionSpeed, parkSpeed);
    }

    private bool NeedsWaitingAtIntersection()
    {
        if (_nextRoadTurn == TurnDirection.STRAIGHT) return false;
        if (_nextIntType != IntersectionType.END && _nextRoadTurn == TurnDirection.RIGHT) return false;
        
        var userCarT = _userCar.transform;
        var userCarToInt = _currentRoad.EndPos - userCarT.position;
        var myCarToIntDistance2 = (_currentRoad.EndPos - transform.position).sqrMagnitude;
        var userCarIntDist2 = userCarToInt.sqrMagnitude;
        if (myCarToIntDistance2 > 30 * 30 || userCarIntDist2 > 150 * 150) return false;
        if (myCarToIntDistance2 < 8 * 8) return false;
        var userCarIntAngle = Vector3.Angle(userCarT.forward, userCarToInt);
        if (userCarIntAngle > 90) return false;
        var carCompAngle = Vector3.SignedAngle(userCarT.forward, transform.forward, Vector3.up);
        if (Mathf.Abs(carCompAngle) < 35) return false;
        if (_nextIntType == IntersectionType.END) return _nextRoadTurn == TurnDirection.LEFT || carCompAngle < 50;
        // wants to turn left at straight intersection
        return Mathf.Abs(carCompAngle) > 125;
    }

    private bool UpdateClosestCar(GameObject car, ref float currentClosestDist, ref GameObject currentClosestCar)
    {
        var myTrans = transform;
        var myPos = myTrans.position;
        var myVel = _rb.velocity.magnitude;
        var t = car.transform;
        var delta = t.position - myPos;
        var dist = delta.magnitude;
        if (dist >= currentClosestDist || (dist > myVel * 3.6f / 2 && dist > 15))
        {
            return false;
        }

        var angleToCar = Vector3.Angle(delta, myTrans.forward);
        if ((Vector3.Angle(myTrans.forward, t.forward) > 60 && angleToCar > 15) || angleToCar > 60)
        {
            return false;
        }

        // if (dist < 20 && (_userCar.transform.position - transform.position).sqrMagnitude > 150 * 150)
        // {
        //     car.transform.position += new Vector3(10000, 0, 0);
        //     return false;
        // }

        currentClosestDist = dist;
        currentClosestCar = car;
        return true;
    }

    public void SetRoadToFollow(MappedRoad road, int lane = 0) {
        if(_currentRoad != null) {
            _currentRoad.cars.Remove(this);
        }
        _currentRoad = road;
        _currentRoad.cars.Add(this);
        _currentLane = Mathf.Min(_currentRoad.lanes.Count, Mathf.Max(0, lane));
        _roadPrg = road.lanes[_currentLane].GetProgressFromPosition(transform.position, sigma: 5);
        _nextRoad = null;
        _nextRoadTurn = TurnDirection.INVALID;
        _nextIntType = IntersectionType.INVALID;

        parkForSecs = 0;
        parkedForSecs = 0;
        _waitedAtIntersection = 0;
        _speedI = 0;
    }

    public void ModifySpeed(float newSpeed, float duration)
    {
        modifiedSpeedSetpoint = newSpeed;
        modifiedSpeedRemaining = duration;
    }

    public enum TurnDirection
    {
        RIGHT,
        LEFT,
        STRAIGHT,
        INVALID
    }
    
    private enum IntersectionType
    {
        STRAIGHT,
        END,
        INVALID
    }
}
