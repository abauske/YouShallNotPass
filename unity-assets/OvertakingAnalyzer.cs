using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Map;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;


public class OvertakingStartSection : PathSection
{
    public OvertakingStartSection(MappedRoad road, float startProgress = 0, float endProgress = Single.PositiveInfinity) : base(road, startProgress, endProgress)
    {
    }
}

public class OvertakingPath : Path<OvertakingStartSection>
{
    public OvertakingPath() {}
    public OvertakingPath(IEnumerable<OvertakingStartSection> vals) : base(vals) {}
}

public static class OvertakingPathExtension
{
    public static OvertakingPath ToOvertakingPath<T>(this IEnumerable<T> source) where T : OvertakingStartSection
    {
        return source != null ? new OvertakingPath(source) : throw new ArgumentNullException(nameof(source));
    }
}

public enum OvertakingStatus
{
    NoCarToTake,
    StayInLane,
    OvertakingMightBePossible
}

public enum OvertakingWarnExplanation
{
    NoWarn,
    Bump,
    Corner,
    RoadTooShort,
    NoSpaceToGoBackToLane
}

public class OvertakingAnalyzer : MonoBehaviour
{
    private LoggingSubject l = new LoggingSubject();
    
    private const float dt = 0.1f;
    private RequiredDist _rdConst;
    private RequiredDistLDM _rdLDM;
    private RequiredDistDyn _rdDyn;
    private List<(RequiredDist, RequiredDistLogger, string)> _rds = new List<(RequiredDist, RequiredDistLogger, string)>();
    public Rigidbody userCar;
    private Bounds userCarBounds;
    
    private CarSpawner _carsProvider;
    
    public Navigation pathPlanner;
    public float maxOvertakingDistance = 100;
    public float maxRotationDegree = 30;

    public float tes = 1f;
    public float ts = 1f;
    public float tsopp = 1.5f;
    public bool onlyConsiderOneCar = false;

    public float minOvertakingLength = 50;
    
    private List<Action<OvertakingStartSection>> _listeners = new List<Action<OvertakingStartSection>>();

    public OvertakingStatus Status => l.Status;
    public OvertakingWarnExplanation WarnExplanation => l.WarnExplanation;
    private AutoSteer tooCloseCar;
    private float tooCloseTimer = 0;
    public float DistToNextOvertaking { get; private set; } = Single.PositiveInfinity;
    public bool isSureOvertakingSection = false;

    public TextMeshPro debug;

    // Start is called before the first frame update
    void Start()
    {
        _rdConst = new RequiredDistConst();
        _rdLDM = new RequiredDistLDM(dt);
        _rdDyn = new RequiredDistDyn(dt);
        _rds.Add((_rdConst, new RequiredDistLogger(), "const"));
        _rds.Add((_rdLDM, new RequiredDistLogger(), "ldm.d" + dt));
        _rds.Add((_rdDyn, new RequiredDistLogger(), "dyn.d" + dt));

        _carsProvider = FindObjectOfType<CarSpawner>();

        pathPlanner.AddChangeListener(Pathchange);
        
        userCarBounds = RecursiveGetBounds(userCar.gameObject);

        var logger = FindObjectOfType<CarLogger>();
        logger.RegisterLoggedObject(l, "overtakingAnalyzer");
        foreach (var (rd, rdl, s) in _rds)
        {
            logger.RegisterLoggedObject(rdl, s);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (WarnExplanation == OvertakingWarnExplanation.NoSpaceToGoBackToLane)
        {
            tooCloseTimer += Time.deltaTime;
            bool isSureSectionClose = isSureOvertakingSection && DistToNextOvertaking < 2000;
            if (tooCloseTimer > 10 && !isSureSectionClose)
            {
                var queue = FindCarsToOvertake(tooCloseCar.transform, 50, 3);
                for (int i = 0; i < queue.Count; i++)
                {
                    queue[i].GetComponent<AutoSteer>().parkForSecs = 10 * (i + 1);
                }
                tooCloseCar.GetComponent<AutoSteer>().parkForSecs = 10;
                tooCloseTimer = 0;
            }
        }
        else
        {
            tooCloseTimer = 0;
        }
    }

    private bool IsInFrontOf(Vector3 toCheckPos, Vector3 referencePos, Vector3 frontDirection, float inFrontDegree = 30)
    {
        var delta = toCheckPos - referencePos;
        return Vector3.Angle(frontDirection, delta.normalized) < inFrontDegree;
    }

    private List<AutoSteer> FindCarsToOvertake(Transform searchPoint, float maxDist, int maxCount = Int32.MaxValue)
    {
        if (maxCount <= 0)
        {
            return new List<AutoSteer>();
        }
        
        AutoSteer closest = null;
        float closestDist = float.PositiveInfinity;
        foreach (var car in _carsProvider.AllCars)
        {
            var delta = car.transform.position - searchPoint.position;
            var dist = delta.sqrMagnitude;
            if (dist > maxDist * maxDist || dist > closestDist || dist < 0.5 || car.parkForSecs > 1)
            {
                continue;
            }

            if (Vector3.Angle(car.transform.forward, searchPoint.forward) > maxRotationDegree)
            {
                continue;
            }

            if (!IsInFrontOf(car.position, searchPoint.position, searchPoint.forward, maxRotationDegree))
            {
                continue;
            }

            closest = car;
            closestDist = dist;
        }

        if (closest == null)
        {
            return new List<AutoSteer>();
        }

        var other = FindCarsToOvertake(closest.transform, maxDist, maxCount - 1);
        other.Insert(0, closest);
        return other;
    }

    public List<AutoSteer> carsToTake = new List<AutoSteer>();
    private Bounds bounds = new Bounds();
    
    
    void OnDrawGizmos()
    {
        foreach (var car in carsToTake)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(car.position, 3);
        }
        Gizmos.DrawCube(bounds.center, bounds.size);
    }

    Bounds RecursiveGetBounds(GameObject start)
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

    void Pathchange(NavigationPath path)
    {
        debug.text = "";
        l.Status = OvertakingStatus.NoCarToTake;
        l.WarnExplanation = OvertakingWarnExplanation.NoWarn;
        DistToNextOvertaking = Single.PositiveInfinity;
        isSureOvertakingSection = false;
        if (path.CalcLength() < 10)
        {
            publish(null);
            return;
        }

        l.laneCount = path[0].Road.lanes.Count;
        
        carsToTake = FindCarsToOvertake(userCar.transform, maxOvertakingDistance, onlyConsiderOneCar ? 1 : 2);

        if (carsToTake.Count <= 0)
        {
            publish(null);
            return;
        }
        l.Status = OvertakingStatus.StayInLane;

        var (startRoadPos, startProgress) = path[0].Road.GetRoadPositionFromPosition(userCar.position);
        var startDist = (userCar.position - startRoadPos).magnitude;

        var firstBounds = RecursiveGetBounds(carsToTake.First().gameObject);
        l.Loen = firstBounds.size.x;
        // l.Loen = 0;
        // var lastBounds = RecursiveGetBounds(carsToTake.Last().gameObject);
        //
        // l.Loen += firstBounds.size.x / 2;
        // l.Loen += lastBounds.size.x / 2;
        //
        // for (int i = 0; i < carsToTake.Count - 1; i++)
        // {
        //     l.Loen += (carsToTake[i].position - carsToTake[i + 1].position).magnitude;
        // }

        l.Loing = userCarBounds.size.x;

        l.voen = carsToTake[0].velocity.magnitude;

        l.L1 = (userCar.position - carsToTake[0].position).magnitude - firstBounds.size.x / 2 -
               userCarBounds.size.x / 2;

        var userForward = userCar.transform.forward;
        l.G = (float)Math.Tan(Vector3.Angle(userForward, new Vector3(userForward.x, 0, userForward.z)) * Mathf.Deg2Rad);

        l.Lemin = Math.Max(l.Loing + 2 * tes * l.voen, 5 * l.Loing);
        l.Le = carsToTake.Count > 1
            ? (carsToTake[0].position - carsToTake[1].position).magnitude - firstBounds.size.x / 2 - RecursiveGetBounds(carsToTake[1].gameObject).size.x / 2
            : Single.PositiveInfinity;

        if (l.Le < l.Lemin)
        {
            l.WarnExplanation = OvertakingWarnExplanation.NoSpaceToGoBackToLane;
            var multiLaneSections = FindOvertakingSections(path, Single.PositiveInfinity, 1);
            SetOvertakingPossibilities(path, multiLaneSections, startProgress, updateStatus: true);
            var closeCar = carsToTake[1];
            if (tooCloseCar != closeCar)
            {
                tooCloseCar = closeCar;
                tooCloseTimer = 0;
            }
            return;
        }
        
        l.L2 = Math.Min(ts * l.voen, (l.Le - l.Loing) / 2);

        l.Lges = l.L1 + l.L2 + l.Loen + l.Loing;
        l.LtoCrit = l.L1 + l.Loen / 2 + l.Loing / 2;
        float Lrest = l.Lges - l.LtoCrit;

        l.L3 = l.L2;
        l.Leges = l.L1 + l.Loen + l.Loing + l.Le - l.L3;

        float vlim = 100;
        float vlimopp = vlim;
        float vlimoing = vlim;

        l.vmaxopp = (0.72514286f * vlimopp + 51.80060566f) / 3.6f;

        bool statusSet = false;

        foreach (var (rd, rdl, s) in _rds)
        {
            (double ddist, double dtges, double dvoingend, double teq, double startupDist) = rd.getRequiredDist(userCar.velocity.magnitude, l.LtoCrit, Lrest, l.voen, l.G, 0, vlimoing, l.Leges);
            rdl.dist = (float)ddist;
            rdl.tfinish = (float)dtges;
            rdl.voingend = (float)dvoingend;
            rdl.teq = (float)teq;
            rdl.startupDist = (float)startupDist;

            if (path.CalcLength() < rdl.dist)
            {
                continue;
            }

            rdl.dopp = (rdl.tfinish - rdl.teq) * l.vmaxopp;
            rdl.Lsm = tsopp * (l.vmaxopp + rdl.voingend);

            rdl.dsmin = rdl.dist + rdl.Lsm + rdl.dopp;

            var overtakingStartSections = FindOvertakingSections(path, rdl.dsmin, startOffset: rdl.startupDist);
            overtakingStartSections.ForEach(se => se.StartProgress = Mathf.Max(0, se.StartProgress - rdl.startupDist));
            // publish(overtakingStartSections);
            // foreach (var startSection in overtakingStartSections)
            // {
            //     Debug.DrawLine(startSection.Road.CenterSpline.GetPos(startSection.StartProgress), startSection.Road.CenterSpline.GetPos(startSection.EndProgress), Color.white, 10);
            // }

            statusSet |= SetOvertakingPossibilities(path, overtakingStartSections, startProgress, rdl, !statusSet, rd != _rdConst);
        }

        if (Status == OvertakingStatus.StayInLane)
        {
            var reason = path[0].Road.sightDistanceReasons.Eval(startProgress);
            if (reason == SightLimitReason.Bump)
            {
                l.WarnExplanation = OvertakingWarnExplanation.Bump;
            }
            else if(reason == SightLimitReason.Corner)
            {
                l.WarnExplanation = OvertakingWarnExplanation.Corner;
            }
            else if(reason == SightLimitReason.EndOfRoad)
            {
                l.WarnExplanation = OvertakingWarnExplanation.RoadTooShort;
            }
        }
    }

    private bool SetOvertakingPossibilities(NavigationPath path, List<OvertakingStartSection> overtakingStartSections,
        float startProgress, RequiredDistLogger rdl = null, bool updateStatus = false, bool logonly = false)
    {
        if (overtakingStartSections.Count <= 0)
        {
            if(!logonly) {
                publish(null);
            }
            return false;
        }

        if (rdl == null)
        {
            rdl = new RequiredDistLogger();
        }

        var firstSection = overtakingStartSections[0];
        if(!logonly) {
            publish(firstSection);
        }
        float DistToNextPossibility = 0;
        for (int i = 0; i < path.Count - 1 && path[i].Road != firstSection.Road; i++)
        {
            DistToNextPossibility += path[i].Length;
        }

        rdl.amOnFirstSectionRoad = firstSection.Road == path[0].Road;
        DistToNextPossibility += firstSection.StartProgress - (rdl.amOnFirstSectionRoad ? startProgress : 0);
        rdl.DistToNextOvertaking = DistToNextPossibility;
        if(!logonly) {
            DistToNextOvertaking = DistToNextPossibility;
            isSureOvertakingSection = firstSection.Road.lanes.Count > 1;
        }

        if (rdl.amOnFirstSectionRoad && startProgress + 10 > firstSection.StartProgress && !logonly)
        {
            if (updateStatus)
            {
                l.Status = OvertakingStatus.OvertakingMightBePossible;
                l.WarnExplanation = OvertakingWarnExplanation.NoWarn;
            }

            return true;
        }

        return false;
    }

    private List<OvertakingStartSection> FindOvertakingSections(NavigationPath navPath, float requiredDist, int maxCount = 10, float startOffset = 0)
    {
        if (navPath.Count > 0)
        {
            String txt = "\nReq: {0}m\nAv: {1}m";
            var avail = navPath[0].Road.sightDistance.Eval(navPath[0].StartProgress);
            if(avail > requiredDist)
            {
                txt += "\nNow Possible";
            }
            debug.text += string.Format(txt, requiredDist, avail);
        }
        else
        {
            debug.text += "\nno navpath";
        }
        List<OvertakingStartSection> output = new List<OvertakingStartSection>();
        for (int i = 0; i < navPath.Count && output.Count < maxCount; i++)
        {
            var section = navPath[i];
            if (section.Road.lanes.Count > 1)
            {
                output.Add(new OvertakingStartSection(section.Road, section.StartProgress, section.EndProgress));
                continue;
            }
            if(section.Length < requiredDist || section.Road.sightDistance.MaxY <= requiredDist) continue;
            var startPrg = i > 0 ? section.StartProgress : Math.Max(section.StartProgress - minOvertakingLength, 0);
            var possibilities = section.Road.sightDistance.GetRegionsAbove(requiredDist, startPrg, section.EndProgress)
                .Select(s => new OvertakingStartSection(section.Road, s.Item1, s.Item2)).ToList();
            if (possibilities.Count > 0 && i == 0 && startOffset > 0)
            {
                var first = possibilities.First();
                if (first.EndProgress < startOffset + section.StartProgress)
                {
                    possibilities.RemoveAt(0);
                }
            }
            output.AddRange(possibilities.Where(s => s.EndProgress - s.StartProgress > minOvertakingLength));
        }

        return output;
    }

    private void publish(OvertakingStartSection path)
    {
        foreach (var listener in _listeners)
        {
            listener(path);
        }
    } 

    public void AddPathListener(Action<OvertakingStartSection> listener)
    {
        _listeners.Add(listener);
    }

    private class LoggingSubject
    {
        public float Loen;
        public float Loing;
        public float voen;
        public float L1;
        public float Lemin;
        public float Le;
        public float L2;
        public float Lges;
        public float L3;
        public float Leges;
        public OvertakingStatus Status;
        public float G;
        public float vmaxopp;
        public float LtoCrit;
        public OvertakingWarnExplanation WarnExplanation;
        public int laneCount;
    }
    
    private class RequiredDistLogger
    {
        public float dist;
        public float tfinish;
        public float voingend;
        public float teq;
        public float dopp;
        public float Lsm;
        public float dsmin;
        public bool amOnFirstSectionRoad;
        public float DistToNextOvertaking;
        public float startupDist;
    }
}
