using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class NavigationLine : MonoBehaviour
{
    public Navigation navigator;
    // public NavigationPathPlanner planner;
    private LineRenderer _lineRenderer;
    // public bool showRaw = false;
    void Start() {
        _lineRenderer = GetComponent<LineRenderer>();
        navigator.AddChangeListener(PlanChange);
        // planner.AddChangeListener(PlanChange);
    }

    // void RawChange(NavMeshPath path) {
    //     if (!showRaw) return;
    //     SetPoints(path.corners);
    // }

    void PlanChange(NavigationPath path) {
        // if (showRaw) return;
        var roadPoints = path.Select(x => x.Road.CenterSpline.EvenlyDistribute(30f, x.StartProgress, x.EndProgress)).ToList();
        for (int i = 0; i < roadPoints.Count - 1; i++)
        {
            roadPoints[i].RemoveAt(roadPoints[i].Count - 1);
        }
        SetPoints(roadPoints.SelectMany(x => x).ToArray());
    }

    private void SetPoints(Vector3[] points)
    {
        points = points.Select(x => new Vector3(x.x, transform.position.y, x.z)).ToArray();
        _lineRenderer.positionCount = points.Length;
        _lineRenderer.SetPositions(points);
    }
}
