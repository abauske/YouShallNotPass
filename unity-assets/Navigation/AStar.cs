
using System;
using System.Collections.Generic;
using System.Linq;
using Map;
using UnityEngine;

public class AStar
{

    private static KeyValuePair<K, V> MinBy<K, V>(Dictionary<K, V> dict, Func<KeyValuePair<K, V>, float> comp)
    {
        float minVal = Single.MaxValue;
        KeyValuePair<K, V> min = default;
        foreach (var tuple in dict)
        {
            var val = comp(tuple);
            if (val < minVal)
            {
                minVal = val;
                min = tuple;
            }
        }
        return min;
    }

    public static NavigationPath FindPath(MapStud map, Vector3 start, Vector3 end)
    {
        var (startRoad, _, startPrg) = map.GetClosestRoad(start);
        var (endRoad, _, endPrg) = map.GetClosestRoad(end);
        Dictionary<MappedRoad, (MappedRoad, float)> ClosedList = new Dictionary<MappedRoad, (MappedRoad, float)>();
        // Value: previousRoad, distToHere, minDistToTarget, startProgress
        Dictionary<MappedRoad, (MappedRoad, float, float, float)> OpenList =
            new Dictionary<MappedRoad, (MappedRoad, float, float, float)>();
        
        // add start node to Open List
        OpenList[startRoad] = (null, 0, 0, startPrg);
        OpenList[startRoad.oppositeDirection] = (null, 0, 0, startRoad.Length - startPrg);

        MappedRoad minEndRoad = null;
        float minEndDist = Single.PositiveInfinity;

        MappedRoad current;
        while(OpenList.Count != 0)
        {
            var minKV = MinBy(OpenList, x => x.Value.Item3);
            var currentMinDist = minKV.Value.Item3;
            if (currentMinDist >= minEndDist)
            {
                break;
            }
            OpenList.Remove(minKV.Key);
            current = minKV.Key;
            var currentDist = minKV.Value.Item2;
            ClosedList[current] = (minKV.Value.Item1, currentDist);
            bool isEnd = endRoad == current;
            bool isEndOpposite = endRoad.oppositeDirection == current;
            if (isEnd || isEndOpposite)
            {
                var distToEnd = currentDist + (isEnd ? endPrg : current.Length - endPrg);
                if (distToEnd < minEndDist)
                {
                    minEndDist = distToEnd;
                    minEndRoad = current;
                }
            }

            foreach(MappedRoad n in current.endIntersectionNextRoads)
            {
                if(ClosedList.ContainsKey(n)) continue;
                
                var distToHere = currentDist + current.Length - minKV.Value.Item4;
                if (OpenList.TryGetValue(n, out var tuple))
                {
                    if(tuple.Item2 <= distToHere) continue;
                }

                var minDist = distToHere + (n.CenterSpline.points.First() - end).magnitude;
                OpenList[n] = (current, distToHere, minDist, 0);
            }
        }
        
        // construct path, if end was not closed return null
        if(minEndDist >= Single.PositiveInfinity) return null;

        var outPath = new NavigationPath();

        current = minEndRoad;
        // if all good, return path
        while (current != null)
        {
            var (previous, dist) = ClosedList[current];
            outPath.Add(new NavigationPathSection(current));
            current = previous;
        }

        outPath[0].EndProgress = outPath[0].Road == endRoad ? endPrg : endRoad.Length - endPrg;
        outPath.Reverse();
        outPath[0].StartProgress = outPath[0].Road == startRoad ? startPrg : startRoad.Length - startPrg;

        if (outPath.Count == 1)
        {
            // start road == end road or at least opposite
            if (outPath[0].StartProgress > outPath[0].EndProgress)
            {
                outPath[0].Road = outPath[0].Road.oppositeDirection;
                outPath[0].StartProgress = outPath[0].Road.Length - outPath[0].StartProgress;
                outPath[0].EndProgress = outPath[0].Road.Length - outPath[0].EndProgress;
            }
        }

        if(outPath.Count > 1 && outPath[0].Length < 10) {
            outPath.RemoveAt(0);
        }

        if(outPath.Count > 1 && outPath.Last().Length < 10) {
            outPath.RemoveAt(outPath.Count - 1);
        }
        
        return outPath;
    }
}