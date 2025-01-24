using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Map;
using UnityEngine;

public class PathSection
{
    public MappedRoad Road;
    public float StartProgress;
    public float EndProgress;
    public float Length => EndProgress - StartProgress;

    public PathSection(MappedRoad road, float startProgress = 0, float endProgress = Single.PositiveInfinity)
    {
        Road = road;
        StartProgress = Math.Max(0, startProgress);
        EndProgress = Math.Min(road.Length, endProgress);
    }
}

public class Path<T> : List<T> where T: PathSection 
{
    public Path() {}
    public Path(IEnumerable<T> vals) : base(vals) {}
    
    public float CalcLength()
    {
        return this.Aggregate(0f, (i, section) => i + section.Length);
    }
}