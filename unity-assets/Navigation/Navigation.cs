using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Map;
using UnityEngine;
using UnityEngine.AI;

public class NavigationPathSection : PathSection
{
    public NavigationPathSection(MappedRoad road, float startProgress = 0, float endProgress = Single.PositiveInfinity) : base(road, startProgress, endProgress)
    {
    }
}

public class NavigationPath : Path<NavigationPathSection> { }

public class Navigation : MonoBehaviour
{
    public List<Transform> targets = new List<Transform>();
    private NavigationPath path;
    private float elapsed = 0.0f;
    private List<Action<NavigationPath>> listeners = new List<Action<NavigationPath>>();
    public MapStud map;

    void Update()
    {
        // Update the way to the goal every second.
        elapsed += Time.deltaTime;
        if (elapsed > 1.0f)
        {
            elapsed -= 1.0f;
            CalcAndPublishPath();
        }
        // for (int i = 0; i < path.corners.Length - 1; i++)
        //     Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
    }

    private void CalcAndPublishPath()
    {
        path = new NavigationPath();
        if (targets.Count > 0)
        {
            var carPos = transform.position;
            path = AStar.FindPath(map, carPos, targets[0].position);
        }

        for (int i = 0; i < targets.Count-1; i++)
        {
            path.AddRange(AStar.FindPath(map, targets[i].position, targets[i+1].position));
        }

        // Notify Listeners
        foreach (var listener in listeners)
        {
            listener(path);
        }
    }

    public void AddChangeListener(Action<NavigationPath> listener)
    {
        listeners.Add(listener);
        if (path != null)
        {
            listener(path);
        }
    }

    public void ForceCalcPathNow()
    {
        CalcAndPublishPath();
    }
}
