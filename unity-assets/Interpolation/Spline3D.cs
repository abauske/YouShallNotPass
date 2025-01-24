using System;
using System.Collections.Generic;
using System.Linq;
using TestMySpline;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interpolation
{
    [Serializable]
    public class Spline3D
    {
        [SerializeField] private CubicSpline x;
        [SerializeField] private CubicSpline y;
        [SerializeField] private CubicSpline z;
        public float Length => EndProgress - StartProgress;
        public float StartProgress => progress.FirstOrDefault();
        public float EndProgress => progress.LastOrDefault();
        [SerializeField] public float[] progress;
        [SerializeField] public Vector3[] points;
        
        public Spline3D(float[] progress, Vector3[] points)
        {
            x = new CubicSpline(progress, points.Select(p => p.x).ToArray());
            y = new CubicSpline(progress, points.Select(p => p.y).ToArray());
            z = new CubicSpline(progress, points.Select(p => p.z).ToArray());
            this.progress = progress;
            this.points = points;
        }

        public Vector3 GetPos(float progress, bool limit = false)
        {
            if (limit)
            {
                progress = Mathf.Max(this.progress[0], Mathf.Min(this.progress.Last(), progress));
            }
            return new Vector3(x.Eval(progress), y.Eval(progress), z.Eval(progress));
        }

        public Vector3 GetDirection(float progress, bool limit = false)
        {
            if (limit)
            {
                progress = Mathf.Max(this.progress[0], Mathf.Min(this.progress.Last(), progress));
            }
            return new Vector3(x.EvalSlope(progress), y.EvalSlope(progress), z.EvalSlope(progress));
        }

        public List<Vector3> EvenlyDistribute(float resolution, float start = 0, float end = Single.PositiveInfinity)
        {
            return EvenlyDistribute(resolution, out var _, start, end);
        }

        public List<Vector3> EvenlyDistribute(int count, float start = 0, float end = Single.PositiveInfinity)
        {
            return EvenlyDistribute(count, out var _, start, end);
        }

        public List<Vector3> EvenlyDistribute(float resolution, out List<float> progressOut, float start = 0, float end = Single.PositiveInfinity)
        {
            end = Math.Min(EndProgress, end);
            start = Math.Max(StartProgress, start);
            int count = (int)((end - start) / resolution) + 2;
            var result = EvenlyDistribute(count, out var p, start, end);
            progressOut = p;
            return result;
        }
        
        public List<Vector3> EvenlyDistribute(int count, out List<float> progressOut, float start = 0, float end = Single.PositiveInfinity)
        {
            end = Math.Min(EndProgress, end);
            start = Math.Max(StartProgress, start);
            float totalDeltaProgress = end - start;
            
            float deltaProgress = totalDeltaProgress / (count - 1);

            var positions = new List<Vector3>();
            progressOut = new List<float>();
            
            for (int i = 0; i < count - 1; i++)
            {
                float progress = start + deltaProgress * i;
                progressOut.Add(progress);
                positions.Add(GetPos(progress));
            }
            positions.Add(GetPos(end));
            progressOut.Add(end);
            
            return positions;
        }

        public float GetProgressByCloseProgress(Vector3 pos, float knownCloseProgress, float startResolution = 10, float sigma = 1e-2f)
        {
            if (startResolution <= sigma) return knownCloseProgress;
            var closePrgInBounds = Math.Max(StartProgress, Math.Min(EndProgress, knownCloseProgress));
            var closePos = GetPos(closePrgInBounds);
            var nextProgressToTest = closePrgInBounds + startResolution;
            var closestDist = (pos - closePos).sqrMagnitude;
            var closestPrg = closePrgInBounds;
            while(nextProgressToTest <= EndProgress) {
                var testPos = GetPos(nextProgressToTest);
                var testDist = (testPos - pos).sqrMagnitude;
                if(testDist >= closestDist) {
                    break;
                }
                closestPrg = nextProgressToTest;
                closestDist = testDist;
                nextProgressToTest += startResolution;
            }
            nextProgressToTest = closePrgInBounds - startResolution;
            while(nextProgressToTest >= StartProgress) {
                var testPos = GetPos(nextProgressToTest);
                var testDist = (testPos - pos).sqrMagnitude;
                if(testDist >= closestDist) {
                    break;
                }
                closestPrg = nextProgressToTest;
                closestDist = testDist;
                nextProgressToTest -= startResolution;
            }
            return GetProgressByCloseProgress(pos, closestPrg, startResolution / 2, sigma);
        }

        public float GetProgressFromPosition(Vector3 pos, float startResolution = 1000, float minProgress = 0, float maxProgress = Single.PositiveInfinity, float sigma = 1e-2f, float lastClosestDist = Single.PositiveInfinity)
        {
            minProgress = Math.Max(StartProgress, minProgress);
            maxProgress = Math.Min(EndProgress, maxProgress);
            if (maxProgress - minProgress < 10)
            {
                return GetProgressByCloseProgress(pos, (minProgress + maxProgress) / 2, sigma: sigma);
            }

            startResolution = Math.Min(startResolution, (maxProgress - minProgress) / 10);
            
            var middle = EvenlyDistribute(startResolution, out var progress, minProgress, maxProgress);
            var dp = progress.Count > 1 ? progress[1] - progress[0] : startResolution / 2;

            int closestId = -1;
            float closestDist = Single.PositiveInfinity;
            var closeIdList = new List<int>();
            var closeDistList = new List<float>();
            for (int j = 0; j < middle.Count; j++)
            {
                var dist = (middle[j] - pos).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestId = j;
                }
                if(dist > startResolution * startResolution / 2 / 2) continue;
                
                closeIdList.Add(j);
                closeDistList.Add(dist);
            }

            if (closeIdList.Count == 1 && lastClosestDist > closestDist && startResolution < sigma)
            {
                return progress[closestId];
            }

            if (closeIdList.Count <= 0)
            {
                if (startResolution < sigma)
                {
                    return progress[closestId];
                }
                if (closestId == 0)
                {
                    return GetProgressFromPosition(pos, startResolution / 10, progress[0], progress[1], sigma,
                        closestDist);
                } 
                if (closestId == middle.Count - 1)
                {
                    return GetProgressFromPosition(pos, startResolution / 10, progress[middle.Count - 2], progress[middle.Count - 1], sigma,
                        closestDist);
                }

                return GetProgressFromPosition(pos, startResolution / 5, progress[closestId - 1], progress[closestId + 1], sigma,
                    closestDist);
            }

            int rangeStart = -1;
            int rangeEnd = -1;
            float rangeClosestDist = Single.PositiveInfinity;
            closestDist = Single.PositiveInfinity;
            float closestProgress = -1;
            for (int i = 0; i < closeIdList.Count; i++)
            {
                if (rangeStart < 0)
                {
                    rangeStart = closeIdList[i];
                    rangeEnd = rangeStart;
                    rangeClosestDist = closeDistList[i];
                }
                else
                {
                    rangeEnd = closeIdList[i];
                    rangeClosestDist = Math.Min(rangeClosestDist, closeDistList[i]);
                }

                if (i == closeIdList.Count - 1 || closeIdList[i] + 1 != closeIdList[i + 1])
                {
                    var startProgress = Math.Max(minProgress, progress[rangeStart] - startResolution / 2);
                    var endProgress = Math.Min(maxProgress, progress[rangeEnd] + startResolution / 2);
                    var prog = GetProgressFromPosition(pos, startResolution / 10, startProgress,
                        endProgress, sigma, rangeClosestDist);
                    var newPos = GetPos(prog);
                    var dist = (newPos - pos).sqrMagnitude;
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestProgress = prog;
                    }

                    rangeStart = -1;
                    rangeEnd = -1;
                    rangeClosestDist = Single.PositiveInfinity;
                }
            }

            return closestProgress;
        }
    }
}