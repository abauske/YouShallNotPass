using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public class SectionHandler : MonoBehaviour
{
    [System.Serializable]
    public class Section
    {
        public List<Transform> landmarks = new List<Transform> {null, null};
        public bool enabled = true;
        [Range(0.0f, 1.0f)]
        public float trafficDensity = 0.2f;
        [Range(0.0f, 1.0f)]
        public float trafficPercentageOppositeSide = 0.6f;
        public float Length;
    }
    
    public float maxReachedDist = 30;
    public int currentActiveSection = 0;
    public int startSection = -1;
    public bool randomize = true;
    public Transform userCar;
    public float fadeoutTime = 1;
    public List<Section> Sections = new List<Section>();
    public int visualizeSection = -1;
    
    public bool Paused
    {
        get { return l.paused; }
        private set {
            l.paused = value;
            Time.timeScale = l.paused ? 0 : 1;
        }
    }
    
    private List<int> unhandledSections = new List<int>();
    public int unhandledSectionCount => unhandledSections.Count;
    private SortedSet<int> handledSections = new SortedSet<int>();
    public int handledSectionsCount => handledSections.Count;
    private Random _rand;
    private float endReachtedTime = Single.PositiveInfinity;
    private Navigation navigator;
    private LoggingSubject l = new LoggingSubject();
    private CarSpawner _carSpawner;
    private int currentSectionLandmarkReached = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        navigator = FindObjectOfType<Navigation>();
        _carSpawner = FindObjectOfType<CarSpawner>();
        _rand = new Random((uint) UnityEngine.Random.Range(1, Int32.MaxValue));
        unhandledSections.Clear();
        for (int i = 0; i < Sections.Count; i++)
        {
            if(!Sections[i].enabled) continue;
            if (Sections[i].landmarks.Count < 2 || Sections[i].landmarks.Any(s => s == null))
            {
                throw new Exception("Section must at least have a start and an end");
            }
            unhandledSections.Add(i);
        }
        FindObjectOfType<CarLogger>().RegisterLoggedObject(l, "Section");

        if (startSection > 0 && startSection < Sections.Count && Sections[startSection].enabled)
        {
            ActivateSection(startSection);
        }
        else
        {
            ActivateNextSection();
        }
    }

    // Update is called once per frame
    void Update()
    {
        l.current = currentActiveSection;

        if (currentActiveSection < 0 || currentActiveSection >= Sections.Count)
        {
            return;
        }
        
        // only allow continue if not finished
        if (Input.GetKeyDown("joystick button 2") || Input.GetKeyDown(KeyCode.P))
        {
            Paused = !Paused;
        }

        var curSection = Sections[currentActiveSection];
        if ((userCar.position - curSection.landmarks[currentSectionLandmarkReached + 1].position).sqrMagnitude <=
            maxReachedDist * maxReachedDist)
        {
            if (currentSectionLandmarkReached + 1 >= curSection.landmarks.Count - 1)
            {
                // last landmark reached
                endReachtedTime = Mathf.Min(Time.time, endReachtedTime);
                handledSections.Add(currentActiveSection);
            }
            else
            {
                // more landmarks to go
                currentSectionLandmarkReached++;
                UpdateTargetLandmarks();
            }
        }

        float timeSinceEndReach = Time.time - endReachtedTime;
        if (timeSinceEndReach >= 0)
        {
            Time.timeScale = 1 / (timeSinceEndReach + 1);

            if (timeSinceEndReach > fadeoutTime)
            {
                if (unhandledSections.Count > 0)
                {
                    ActivateNextSection();
                }
                else
                {
                    Paused = true;
                    endReachtedTime = Single.PositiveInfinity;
                    currentActiveSection = Sections.Count;
                }
            }
        }
    }

    private void UpdateTargetLandmarks()
    {
        var curSection = Sections[currentActiveSection];
        var curLandmarks = curSection.landmarks;
        navigator.targets = curLandmarks.GetRange(currentSectionLandmarkReached + 1,
            Math.Min(curLandmarks.Count - currentSectionLandmarkReached - 1, 2));
    }

    private void ActivateNextSection()
    {
        ActivateSection(GetNextSectionIndex());
    }

    private void ActivateSection(int index)
    {
        if (index < 0 || index >= Sections.Count)
        {
            throw new Exception("cannot activate section " + index + " as there are only " + Sections.Count +
                                " sections");
        }
        if (!Sections[index].enabled)
        {
            throw new Exception("cannot activate section " + index + " as it is not enabled");
        }
        endReachtedTime = Single.PositiveInfinity;
        unhandledSections.Remove(index);
        currentActiveSection = index;
        currentSectionLandmarkReached = 0;
        var newSection = Sections[currentActiveSection];
        var start = newSection.landmarks.First();
        userCar.position = start.position;
        userCar.rotation = start.rotation;
        userCar.GetComponent<Rigidbody>().velocity = Vector3.zero;
        Paused = true;
        UpdateTargetLandmarks();
        navigator.ForceCalcPathNow();
        _carSpawner.UpdateTrafficDensity(newSection.trafficDensity);
        _carSpawner.oppositeSideProbability = newSection.trafficPercentageOppositeSide;
        _carSpawner.Respread();
    }

    private int GetNextSectionIndex()
    {
        if (!randomize) return unhandledSections[0];
        return unhandledSections[_rand.NextInt(unhandledSections.Count)];
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (visualizeSection < 0 || visualizeSection >= Sections.Count)
        {
            return;
        }

        var s = Sections[visualizeSection];
        s.Length = 0;
        for (int i = 0; i < s.landmarks.Count - 1; i++)
        {
            if(s.landmarks[i+1] == null || s.landmarks[i] == null) continue;
            var path = AStar.FindPath(FindObjectOfType<MapStud>(), s.landmarks[i].position, s.landmarks[i+1].position);
            foreach (var pathSection in path)
            {
                var points = pathSection.Road.CenterSpline.EvenlyDistribute(60f, pathSection.StartProgress, pathSection.EndProgress).Select(v => new Vector3(v.x, v.y + 20, v.z)).ToArray();
                Handles.color = Color.blue;
                for (int j = 0; j < points.Length - 1; j++) {
                    Handles.ArrowHandleCap(0, points[j], Quaternion.LookRotation(points[j+1] - points[j]), 55, EventType.Repaint);
                }
                // Handles.DrawAAPolyLine(15, points);
            }

            s.Length += path.CalcLength();
        }
    }
#endif


    private class LoggingSubject
    {
        public int current;
        public bool paused;
    }
}
