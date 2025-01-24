using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Map;
using UnityEngine;
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

public class CarSpawner : MonoBehaviour
{
    public GameObject carPrototypesHolder;
    [Range(0.0f, 1.0f)]
    public float trafficDensity = 0.2f;
    public float carCountRadius = 400f;
    public float carLength = 7f;
    public GameObject egoCar;
    public Navigation navigator;
    public MapStud map;
    public float minCarDist = 15f;
    public int forcePlaceXCarsInFront = 3;
    public float oppositeSideProbability = 0.6f;
    
    private List<AutoSteer> _carPrototypes = new();
    [NonSerialized] public List<AutoSteer> AllCars = new();
    private float _elapsed = 1.9f;
    private NavigationPath _navPath;
    private Random _rand;
    private bool _initialSpreadDone = false;
    
    // Start is called before the first frame update
    void Start()
    {
        _rand = new Random((uint) FindObjectOfType<StudyParameters>().Mod + 2);
        
        _carPrototypes = carPrototypesHolder.GetComponentsInChildren<AutoSteer>().ToList();
        _carPrototypes.ForEach(c => {
            c.carId = AllCars.Count;
            AllCars.Add(c);
        });
        navigator.AddChangeListener(path => _navPath = path);
        
        AllCars.ForEach(c => PlaceCarRandomlyOnRoads(c.gameObject, map.Roads.Select(r => new PathSection(r)).ToList()));
        CheckCarCount();
    }

    private void CheckCarCount()
    {
        int carCount = (int)(carCountRadius * 2 * 2 / carLength / 2 * trafficDensity);
        // carCount = 1;

        while (AllCars.Count > carCount && AllCars.Count > _carPrototypes.Count)
        {
            var last = AllCars.Last();
            AllCars.RemoveAt(AllCars.Count - 1);
            Destroy(last.gameObject);
        }

        if (_carPrototypes.Count <= 0)
        {
            Debug.LogWarning("No car prototypes found");
            return;
        }

        while (AllCars.Count < carCount)
        {
            var o = Instantiate(_carPrototypes[_rand.NextInt(0, _carPrototypes.Count)].gameObject, carPrototypesHolder.transform);
            var ai = o.GetComponent<AutoSteer>();
            ai.carId = AllCars.Count;
            AllCars.Add(ai);

            // PlaceCarRandomlyOnRoads(o, map.Roads.Select(r => new PathSection(r)).ToList());
        }
    }

    public void UpdateTrafficDensity(float newDensity)
    {
        trafficDensity = newDensity;
        CheckCarCount();
    }

    // Update is called once per frame
    void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed < 2.0f)
        {
            return;
        }
        _elapsed -= 2.0f;

        if (_navPath == null)
        {
            return;
        }

        int carsPlacedInFront = 0;
        foreach (var car in AllCars)
        {
            float dist = (car.transform.position - egoCar.transform.position).sqrMagnitude;
            if(dist < carCountRadius * carCountRadius) continue;

            float maxPrg = 50 + carsPlacedInFront * 100;
            if (!_initialSpreadDone && carsPlacedInFront <= forcePlaceXCarsInFront && _navPath.CalcLength() >= maxPrg)
            {
                PlaceCarRandomlyOnRoads(car.gameObject, _navPath, 20 + carsPlacedInFront * 100, maxPrg, 0);
                carsPlacedInFront++;
            }
            else
            {
                PlaceCarRandomlyOnAdjacentRoads(car.gameObject, _navPath[0].Road, _navPath[0].StartProgress, _initialSpreadDone ? carCountRadius / 2 : 20,
                    carCountRadius * 1.2f, oppositeSideProbability);
            }
            
            // PlaceCarRandomlyOnRoads(car.gameObject, _navPath, 90, 100, 1);
        }

        _initialSpreadDone = true;
    }

    private void PlaceCarRandomlyOnAdjacentRoads(GameObject car, MappedRoad road, float searchStartProgress, float minProgress = 0, float maxProgress = Single.PositiveInfinity, float oppositeProbability = 0.75f) {
        minProgress = Math.Max(0, minProgress);
        var opposite = _rand.NextFloat(1) < oppositeProbability;
        float progress = _rand.NextFloat(minProgress, maxProgress);

        List<(MappedRoad, float)> placeOptions = new List<(MappedRoad, float)>();
        List<(MappedRoad, float)> toFollow = new List<(MappedRoad, float)>();
        var startMissingLength = searchStartProgress + progress - road.Length;
        if(startMissingLength <= 0) {
            placeOptions.Add((road, searchStartProgress + progress));
        } else {
            toFollow.AddRange(road.endIntersectionNextRoads.Select(r => (r, startMissingLength)));
        }
        var oppMissingLength = searchStartProgress - progress;
        if(oppMissingLength >= 0) {
            placeOptions.Add((road, oppMissingLength));
        } else {
            toFollow.AddRange(road.startIntersectionNextRoads.Select(r => (r, oppMissingLength)));
        }

        while(toFollow.Count > 0) {
            var cur = toFollow.Last();
            toFollow.RemoveAt(toFollow.Count - 1);
            if(cur.Item2 >= 0) {
                // forward
                var forwardMissingLength = cur.Item2 - cur.Item1.Length;
                if(forwardMissingLength <= 0) {
                    placeOptions.Add((cur.Item1, cur.Item2));
                } else {
                    toFollow.AddRange(cur.Item1.endIntersectionNextRoads.Select(r => (r, forwardMissingLength)));
                }
            } else {
                // opposite direction
                oppMissingLength = cur.Item1.Length + cur.Item2;
                if(oppMissingLength >= 0) {
                    placeOptions.Add((cur.Item1, oppMissingLength));
                } else {
                    toFollow.AddRange(cur.Item1.startIntersectionNextRoads.Select(r => (r, oppMissingLength)));
                }
            }
        }

        while(placeOptions.Count > 0) {
            var index = _rand.NextInt(placeOptions.Count);
            var (r, prg) = placeOptions[index];
            placeOptions.RemoveAt(index);
            if(opposite) {
                prg = r.Length - prg;
                r = r.oppositeDirection;
            }
            if(PlaceCarRandomlyOnRoad(car, r, prg)) {
                break;
            }
        }
    }

    private void PlaceCarRandomlyOnRoads<T>(GameObject car, List<T> roads, float minProgress = 0, float maxProgress = Single.PositiveInfinity, float oppositeProbability = 0.75f) where T: PathSection
    {
        float totalLength = roads.Aggregate(0f, (agg, r) => agg + r.Length);
        maxProgress = Math.Min(totalLength, maxProgress);
        minProgress = Math.Max(0, minProgress);
        var opposite = _rand.NextFloat(1) < oppositeProbability;
        float progress = _rand.NextFloat(minProgress, maxProgress);

        int i = 0;
        for (; i < roads.Count - 1 && progress - roads[i].Length > 0; i++)
        {
            progress -= roads[i].Length;
        }

        var section = roads[i];
        var road = section.Road;
        var startProgress = section.StartProgress;
        if (opposite && road.oppositeDirection != null)
        {
            road = road.oppositeDirection;
            startProgress = road.Length - startProgress - 2 * progress;
        }

        PlaceCarRandomlyOnRoad(car, road, startProgress + progress);
    }

    private bool PlaceCarRandomlyOnRoad(GameObject car, MappedRoad road, float progress)
    {
        var laneIndex = _rand.NextInt(0, road.lanes.Count);
        var lane = road.lanes[laneIndex];
        progress = Math.Max(Math.Min(lane.EndProgress, progress), lane.StartProgress);
        var pos = lane.GetPos(progress);
        if(AllCars.Any(c => (c.transform.position - pos).sqrMagnitude < minCarDist * minCarDist)) return false;
        var looking = lane.GetDirection(progress);

        pos.y += 0.8f;
        car.transform.position = pos;
        car.transform.LookAt(pos + looking);
        var rb = car.GetComponent<Rigidbody>();
        rb.velocity = looking * (75 / 3.6f);
        rb.angularVelocity = Vector3.zero;
        car.GetComponent<AutoSteer>().SetRoadToFollow(road, laneIndex);
        return true;
    }

    public void Respread()
    {
        _initialSpreadDone = false;
    }
}
