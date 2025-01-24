using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Interpolation;
using Map;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class MapStud : MonoBehaviour
{
    public float resolution = 10.0f;

    [SerializeReference] public MappedRoad[] Roads = Array.Empty<MappedRoad>();

    [SerializeReference] public MappedIntersection[] Intersections = Array.Empty<MappedIntersection>();


    /**
     * returns (road, position on road, progress)
     */
    public (MappedRoad, Vector3, float) GetClosestRoad(Vector3 searchPoint)
    {
        if (Roads.Length <= 0)
        {
            return (null, Vector3.zero, -1);
        }

        List<(MappedRoad, float, float, float)> options = Roads.Select(r => (r, 0f, Single.PositiveInfinity, Single.PositiveInfinity)).ToList();

        // remove all opposite direction roads
        for (int i = 0; i < options.Count; i++)
        {
            options.RemoveAll(p => p.Item1.oppositeDirection == options[i].Item1);
        }
        
        float res = 400;
        float minDist = Single.PositiveInfinity;
        MappedRoad minRoad = null;
        float minRoadMinPrg = 0;
        float minRoadMaxPrg = 0;
        while (options.Count > 1 && minDist > 3)
        {
            for (int i = options.Count - 1; i >= 0; i--)
            {
                var val = options[i];
                var prg = val.Item1.GetProgressFromPosition(searchPoint, res, val.Item2, val.Item3, res / 2, val.Item4);
                var pos = val.Item1.Pos(prg);
                var dist = (searchPoint - pos).magnitude;
                if (dist - res / 2 > minDist)
                {
                    options.RemoveAt(i);
                    continue;
                }

                val.Item2 = Math.Max(val.Item2, prg - res / 2);
                val.Item3 = Math.Min(val.Item3, prg + res / 2);
                options[i] = val;
                
                if (dist < minDist)
                {
                    minDist = dist;
                    minRoad = val.Item1;
                    minRoadMinPrg = val.Item2;
                    minRoadMaxPrg = val.Item3;
                }
            }

            res /= 10;
        }

        if (options.Count <= 0 || minRoad == null)
        {
            throw new Exception("This should not happen. options got cleared");
        }

        var road = minRoad;
        var closestPrg = road.GetProgressFromPosition(searchPoint, minProgress: minRoadMinPrg, maxProgress:minRoadMaxPrg);
        var closestPos = road.Pos(closestPrg);
        return (road, closestPos, closestPrg);
    }
}
