using System;
using System.Collections.Generic;
using System.Linq;
using Interpolation;
using TestMySpline;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    
    [Serializable]
    public class MappedRoad
    {
        public float Length => CenterSpline.Length;
        [SerializeField] public Spline3D CenterSpline;
        [SerializeField] public CubicSpline sightDistance;
        [SerializeField] public CubicSpline curvature;
        [SerializeField] public Spline3D maxSightPoint;
        [SerializeField] public List<Spline3D> lanes = new List<Spline3D>();
        [SerializeField] public SightReason sightDistanceReasons;
        [SerializeReference] public List<MappedRoad> startIntersectionNextRoads = new List<MappedRoad>();
        [SerializeReference] public List<MappedRoad> endIntersectionNextRoads = new List<MappedRoad>();
        [SerializeReference] public MappedIntersection startIntersection;
        [SerializeReference] public MappedIntersection endIntersection;
        [SerializeReference] public MappedRoad oppositeDirection;
        [SerializeReference] public Vector3 StartPos;
        [SerializeReference] public Vector3 EndPos;
        [NonSerialized] public List<AutoSteer> cars = new();

        public MappedRoad() {}

        public MappedRoad(float[] middleProgress, List<Vector3> middle, float[] laneProgress, List<Vector3>[] lanes)
        {
            if (middleProgress.Length < 2 || middleProgress.Length != middle.Count)
            {
                throw new ArgumentException("At least two points required and progress must be same length as middle");
            }

            if (middleProgress[0] != 0)
            {
                throw new ArgumentException("middleProgress must start at 0");
            }
            CenterSpline = FixProgress(new Spline3D(middleProgress, middle.ToArray()));
            foreach (var lane in lanes)
            {
                if (lane.Count != laneProgress.Length)
                {
                    throw new ArgumentException("Forward lane length must be same as progress length");
                }
                this.lanes.Add(FixProgress(new Spline3D(laneProgress, lane.ToArray())));
            }

            CalcMaxSight();
            CalcCurvature();

            StartPos = middle.First();
            EndPos = middle.Last();
        }

        private void CalcCurvature()
        {
            var progs = CenterSpline.progress;
            var curv = new float[progs.Length];
            for (int i = 0; i < progs.Length; i++)
            {
                var before = CenterSpline.GetDirection(progs[Math.Max(0, i - 1)]);
                var after = CenterSpline.GetDirection(progs[Math.Min(progs.Length - 1, i + 1)]);
                curv[i] = Vector3.Angle(before, after);
            }
            curvature = new CubicSpline(progs, curv);
        }

        private Spline3D FixProgress(Spline3D input, float resolution = 0.1f)
        {
            var oldPrg = input.progress;
            float[] newPrg = new float[oldPrg.Length];
            newPrg[0] = oldPrg[0];
            for (int i = 1; i < oldPrg.Length; i++)
            {
                newPrg[i] = newPrg[i-1] + input.EvenlyDistribute(resolution, oldPrg[i - 1], oldPrg[i]).SelectWithPrevious((p0, p1) => (p0 - p1).magnitude).Sum();
            }

            return new Spline3D(newPrg, input.points);
        }

        private void CalcMaxSight()
        {
            var progs = CenterSpline.progress;
            var positions = CenterSpline.points;
            var forwardSight = new float[progs.Length];
            var forwardSightPoint = new Vector3[progs.Length];
            var forwardSightReason = new SightLimitReason[progs.Length];

            var layerMask = ~(1 << 2) & ~(1 << LayerMask.NameToLayer("AutonomousVehicle"));

            for (int i = 0; i < progs.Length; i++)
            {
                var rawPos = positions[i];
                var pos = new Vector3(rawPos.x, rawPos.y + 1.0f, rawPos.z);
                
                forwardSight[i] = 0;
                forwardSightPoint[i] = rawPos;
                forwardSightReason[i] = SightLimitReason.SameAsStart;
                int jStep = 1;
                for (int j = i + jStep; j < progs.Length; j+=jStep)
                {
                    var rawDst = positions[j];
                    var dir = new Vector3(rawDst.x, rawDst.y + 1.0f, rawDst.z) - pos;
                    if (Physics.Raycast(pos, dir, out var hit, dir.magnitude, layerMask))
                    {
                        var hitname = hit.collider.name;
                        if (hitname.Contains("Road"))
                        {
                            forwardSightReason[i] = SightLimitReason.Bump;
                        }
                        else
                        {
                            forwardSightReason[i] = SightLimitReason.Corner;
                        }
                        forwardSight[i] = progs[j - jStep] - progs[i];
                        forwardSightPoint[i] = positions[j - jStep];
                        break;
                    }
                    forwardSight[i] = progs[progs.Length - 1] - progs[i];
                    forwardSightPoint[i] = positions[positions.Length - 1];
                    forwardSightReason[i] = SightLimitReason.EndOfRoad;
                }
                // if(i % 10 == 0) Debug.DrawLine(pos, forwardSightPoint[i], Color.magenta, 10);
            }

            sightDistance = new CubicSpline(progs, forwardSight);
            maxSightPoint = new Spline3D(progs, forwardSightPoint);
            sightDistanceReasons = new SightReason(progs, forwardSightReason);

            // for (int i = 0; i < progs.Length; i+=10)
            // {
            //     var prog = progs[i];
            //     var pos = CenterSpline.GetPos(prog);
            //     var to = CenterSpline.GetPos(prog + sightDistance.Eval(prog));
            //     to.y = pos.y = to.x > pos.x ? 40 : 50;
            //     Debug.DrawLine(pos, to, to.x > pos.x ? Color.red : Color.yellow, 10);
            // }

            // var regionsAbove = sightDistance.GetRegionsAbove(300);
            // Debug.Log("new road " + regionsAbove.Count);
            //
            // var color = Color.green;
            // if (regionsAbove.Count > 0)
            // {
            //     color = CenterSpline.GetPos(regionsAbove[0].Item1).z < 500 ? Color.yellow : Color.green;
            // }
            //
            // for (int i = 0; i < regionsAbove.Count; i++)
            // {
            //     var pos = CenterSpline.GetPos(regionsAbove[i].Item1);
            //     var to = CenterSpline.GetPos(regionsAbove[i].Item2);
            //     to.y = pos.y = color == Color.green ? 10 : 20;
            //     Debug.DrawLine(pos, to, color, 10);
            //     Debug.Log(pos + " " + to + " " + (color == Color.green ? "green" : "yellow"));
            // }
        }

        public (Vector3, float) GetRoadPositionFromPosition(Vector3 referencePos)
        {
            var progress = GetProgressFromPosition(referencePos);
            return (CenterSpline.GetPos(progress), progress);
        }

        public float GetProgressFromPosition(Vector3 pos, float knownCloseProgress, float startResolution = 10, float sigma = 1e-3f) {
            return CenterSpline.GetProgressFromPosition(pos, knownCloseProgress, startResolution, sigma);
        }

        public float GetProgressFromPosition(Vector3 pos, float startResolution = 1000, float minProgress = 0, float maxProgress = Single.PositiveInfinity, float sigma = 1e-3f, float lastClosestDist = Single.PositiveInfinity)
        {
            return CenterSpline.GetProgressFromPosition(pos, startResolution, minProgress, maxProgress, sigma, lastClosestDist);
        }

        public Vector3 Pos(float progress)
        {
            return CenterSpline.GetPos(progress);
        }
    }
}