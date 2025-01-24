using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public class MapViz : MonoBehaviour
{
    public MapStud map;
    public bool visualizeSightDistance = false;
    public float booleanSightDistance = -1;
    public bool drawRoadNumbers = false;
    public int drawSightDistOfRoad = -1;
    public bool drawSightNumbers = false;
    public bool drawSightLines = false;
    public bool drawSightReason = false;
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // foreach (var road in map.Roads)
        // {
        //     var sight = road.sightDistance;
        //     for (int i = 0; i < sight.XOrig.Length; i++)
        //     {
        //         var pos = road.CenterSpline.GetPos(sight.XOrig[i]);
        //         Handles.Label(pos, sight.YOrig[i].ToString());
        //     }
        // }


        if(drawSightDistOfRoad >= 0 && drawSightDistOfRoad < map.Roads.Length) {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.black;
            var road = map.Roads[drawSightDistOfRoad];
            var sight = road.sightDistance;
            for (int i = 0; i < sight.XOrig.Length; i++)
            {
                var pos = road.Pos(sight.XOrig[i]);
                if (drawSightNumbers)
                {
                    Handles.Label(pos, "Sight: " + sight.YOrig[i] + "\nPrg^-1: " + (road.Length - sight.XOrig[i]),
                        style: style);
                }

                if (drawSightLines)
                {
                    Handles.color = Color.green;
                    Handles.DrawLine(pos, road.Pos(sight.XOrig[i] + sight.YOrig[i]));
                }

                if (drawSightReason)
                {
                    var reason = road.sightDistanceReasons.YOrig[i];
                    var imgPos = pos + new Vector3(0, 2, 0);
                    if (reason == SightLimitReason.Bump)
                    {
                        Gizmos.DrawIcon(imgPos, "Bump.png", false);
                    } else if (reason == SightLimitReason.Corner)
                    {
                        Gizmos.DrawIcon(imgPos, "Corner.png", false);
                    } else if (reason == SightLimitReason.EndOfRoad)
                    {
                        Gizmos.DrawIcon(imgPos, "Crossing.png", false);
                    }
                }
            }
        }

        if (drawRoadNumbers)
        {
            for (int i = 0; i < map.Roads.Length; i++)
            {
                var road = map.Roads[i];
                var centerProgress = road.Length / 2;
                var center = road.lanes.First().GetPos(centerProgress);
                var forward = road.CenterSpline.GetDirection(centerProgress).normalized;
                var scale = 5;
                var right = new Vector3(forward.z * scale, forward.y + 2, -forward.x * scale);
                // var tmpCol = Handles.color;
                Handles.Label(center + right, i.ToString());
                // Handles.color = Color.yellow;
                // Handles.ArrowHandleCap(0, center, Quaternion.LookRotation(forward), scale, EventType.Repaint);
                // Handles.color = Color.green;
                // Handles.ArrowHandleCap(0, center, Quaternion.LookRotation(right), scale, EventType.Repaint);
                // Handles.color = tmpCol;
            }
        }

        if (visualizeSightDistance)
        {
            float maxSight = 0;
            MappedRoad maxSightRoad;
            int maxSightRoadIndex = 0;
            for (int rI = 0; rI < map.Roads.Length; rI++)
            {
                var r = map.Roads[rI];
                if (maxSight < r.sightDistance.MaxY)
                {
                    maxSight = r.sightDistance.MaxY;
                    maxSightRoad = r;
                    maxSightRoadIndex = rI;
                }
            }

            maxSight = 1000;
            
            Handles.Label(new Vector3(-100, 0, -100), maxSight.ToString() + " R: "+ maxSightRoadIndex.ToString());
            
            var gradient = new Gradient();

            // Blend color from red at 0% to blue at 100%
            var colors = new GradientColorKey[3];
            colors[0] = new GradientColorKey(Color.red, 0.0f);
            colors[1] = new GradientColorKey(Color.yellow, 0.5f);
            colors[2] = new GradientColorKey(Color.green, 1);

            // Blend alpha from opaque at 0% to transparent at 100%
            var alphas = new GradientAlphaKey[2];
            alphas[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphas[1] = new GradientAlphaKey(1.0f, 1);

            gradient.SetKeys(colors, alphas);

            Color oldColor = Gizmos.color;
            foreach (var road in map.Roads)
            {
                var sight = road.sightDistance;
                for (int i = 0; i < sight.XOrig.Length; i++)
                {
                    var pos = road.Pos(sight.XOrig[i]);
                    pos.y += 2;
                    Gizmos.color = gradient.Evaluate(sight.YOrig[i] / maxSight);
                    Gizmos.DrawSphere(pos, 6);
                }
            }
            Gizmos.color = oldColor;
        }

        if (booleanSightDistance > 0)
        {
            Color oldColor = Gizmos.color;
            foreach (var road in map.Roads)
            {
                var sight = road.sightDistance;
                // for (int i = 0; i < sight.XOrig.Length; i++)
                // {
                //     var pos = road.Pos(sight.XOrig[i]);
                //     pos.y += 2;
                //     Gizmos.color = sight.YOrig[i] > booleanSightDistance ? Color.green : Color.red;
                //     Gizmos.DrawSphere(pos, 6);
                // }
                var step = 10;
                for (int i = 0; i <= road.Length; i+=step)
                {
                    var pos = road.Pos(i);
                    pos.y += 2;
                    Gizmos.color = sight.Eval(i) > booleanSightDistance ? Color.green : Color.red;
                    Gizmos.DrawSphere(pos, 6);
                }
            }
            Gizmos.color = oldColor;
        }
    }
#endif
}
