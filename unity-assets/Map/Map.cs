
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Interpolation;
using UnityEngine;
using UnityEditor;

namespace Map {
    
#if UNITY_EDITOR
    
    [CustomEditor(typeof(MapStud))]
    public class Map : Editor
    {

        private readonly float INTERSECTION_SIZE = 6;

        private MapStud mapStud;
        SerializedProperty resolution;

        void OnEnable(){
            mapStud = target as MapStud;
            resolution = serializedObject.FindProperty("resolution");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(resolution);
            EditorGUILayout.LabelField("roads: " + mapStud.Roads.Length + " intersections: " + mapStud.Intersections.Length);
            
            // EditorGUILayout.LabelField("spline: " +
            //                            (mapStud.Spline != null ? mapStud.Spline.GetPos(1.5f).ToString() : "-1.11f"));
            
            EditorGUI.BeginChangeCheck();
            bool clicked = GUILayout.Button("Create Map");
            
            //Rename waypoints if some have been deleted
            if (clicked)
            {
                Debug.Log("Creating Map with resolution: " + mapStud.resolution);
                // mapStud.Spline = new Spline3D(new[] { 0, 1, 2.0f },
                //     new[] { new Vector3(0, 0, 0), new Vector3(1, 1, 1), new Vector3(2, 3, 4) });
                CreateMap();
                Debug.Log("Sucessfully created the Map!");
            }
            
            //Repaint the scene if values have been edited
            if (EditorGUI.EndChangeCheck()) {
                SceneView.RepaintAll();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void CreateMap()
        {
            GSDRoad[] tRoadObjs = (GSDRoad[])GameObject.FindObjectsOfType(typeof(GSDRoad));
            GSDRoadIntersection[] tIntersectionObjs = (GSDRoadIntersection[])GameObject.FindObjectsOfType(typeof(GSDRoadIntersection));

            MappedIntersection[] intersectionSegments =
                tIntersectionObjs.Select(i => new MappedIntersection(i.transform.position)).ToArray();

            List<MappedRoad> roads = new List<MappedRoad>();
            MappedRoad r1, r2;

            float maxGrade = 0;
            
            foreach (var road in tRoadObjs)
            {
                var spline = road.GSDSpline.mNodes;
                
                if (spline.Count < 2)
                {
                    continue;
                }

                float splineStartProgress = Single.PositiveInfinity;
                float splineEndProgress = Single.PositiveInfinity;
                float startProgress = Single.PositiveInfinity;
                float endProgress = Single.PositiveInfinity;
                MappedIntersection startIntersection = null;
                MappedIntersection endIntersection = null;

                for (int i = 0; i < spline.Count; i++)
                {
                    var cur = spline[i];
                    maxGrade = Math.Max(maxGrade, Math.Max(Math.Abs(cur.GradeToNextValue), Math.Abs(cur.GradeToPrevValue)));
                    var progress = cur.tDist;
                    if (cur.bSpecialEndNode)
                    {
                        continue;
                    }
                    if (float.IsPositiveInfinity(startProgress))
                    {
                        startProgress = cur.tDist + (cur.bIsIntersection ? INTERSECTION_SIZE : 0);
                        endProgress = startProgress;
                        startIntersection = cur.bIsIntersection ? FindIntersectionIndex(intersectionSegments, cur.pos) : null;
                        splineStartProgress = progress;
                    } else if (cur.bIsIntersection)
                    {
                        endProgress = progress - INTERSECTION_SIZE;
                        endIntersection = FindIntersectionIndex(intersectionSegments, cur.pos);
                        splineEndProgress = progress;
                
                        if (i != spline.Count - 1)
                        {
                            (r1, r2) = CreateMappedRoad(road, startProgress, endProgress, splineStartProgress, splineEndProgress, startIntersection, endIntersection);
                            if (r1 != null) roads.Add(r1);
                            if (r2 != null) roads.Add(r2);
                            startProgress = progress + INTERSECTION_SIZE;
                            endProgress = startProgress;
                            startIntersection = endIntersection;
                            splineStartProgress = progress;
                        }
                    }
                    else
                    {
                        endProgress = progress;
                        endIntersection = null;
                        splineEndProgress = progress;
                    }
                }

                if (endProgress - startProgress > 5)
                {
                    (r1, r2) = CreateMappedRoad(road, startProgress, endProgress, splineStartProgress, splineEndProgress, startIntersection, endIntersection);
                    if (r1 != null) roads.Add(r1);
                    if (r2 != null) roads.Add(r2);
                }
            }

            Undo.RecordObject(mapStud, "Created map");
            mapStud.Roads = roads.ToArray();
            mapStud.Intersections = intersectionSegments;
            Debug.Log("Max Grade: " + maxGrade);
        }

        private MappedIntersection FindIntersectionIndex(MappedIntersection[] intersections, Vector3 pos)
        {
            float closestDist = Single.PositiveInfinity;
            MappedIntersection closest = null;
            foreach (var i in intersections)
            {
                var dist = (i.pos - pos).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = i;
                }
            }

            return closest;
        }

        private (MappedRoad, MappedRoad) CreateMappedRoad(GSDRoad road, float laneStartProgress, float laneEndProgress, float middleStartProgress, float middleEndProgress, MappedIntersection startIntersection, MappedIntersection endIntersection)
        {
            float totalDeltaProgress = laneEndProgress - laneStartProgress;
            
            if (totalDeltaProgress < 0.1)
            {
                return (null, null);
            }
            int lanesPerDir = road.opt_Lanes / 2;
            float laneWidth = road.opt_LaneWidth;

            var (middle, middleProgress, _) = EvenlyDistribute(road, middleStartProgress, middleEndProgress);
            var (laneMiddle, laneProgress, laneForward) = EvenlyDistribute(road, laneStartProgress, laneEndProgress);
            
            List<Vector3>[] forwardLanes = new List<Vector3>[road.opt_Lanes / 2];
            List<Vector3>[] backwardLanes = new List<Vector3>[forwardLanes.Length];
            
            for (int i = 0; i < forwardLanes.Length; i++)
            {
                forwardLanes[i] = new List<Vector3>();
                backwardLanes[i] = new List<Vector3>();
            }

            var progressOffset = middleProgress[0];
            if (progressOffset != 0)
            {
                for (int i = 0; i < middleProgress.Length; i++)
                {
                    middleProgress[i] -= progressOffset;
                }
                for (int i = 0; i < laneProgress.Length; i++)
                {
                    laneProgress[i] -= progressOffset;
                }
            }

            for (int i = 0; i < laneMiddle.Count; i++)
            {
                var pos = laneMiddle[i];
                var forward = laneForward[i];
                for (int j = 0; j < forwardLanes.Length; j++)
                {
                    float offset = laneWidth / 2 + laneWidth * j;
                    forwardLanes[j].Add(GetOffsetPoint(pos, forward, offset));
                    backwardLanes[j].Add(GetOffsetPoint(pos, forward, -offset));
                }
            }

            // Reverse for back lane
            var backMiddle = new List<Vector3>(middle);
            backMiddle.Reverse();
            foreach (var backLane in backwardLanes)
            {
                backLane.Reverse();
            }

            var forwardRoad = new MappedRoad(middleProgress, middle, laneProgress, forwardLanes);
            var backwardRoad = new MappedRoad(middleProgress, backMiddle, laneProgress, backwardLanes);
            forwardRoad.oppositeDirection = backwardRoad;
            backwardRoad.oppositeDirection = forwardRoad;

            startIntersection?.AddOutgoing(forwardRoad);
            startIntersection?.AddOncoming(backwardRoad);
            endIntersection?.AddOutgoing(backwardRoad);
            endIntersection?.AddOncoming(forwardRoad);
            
            return (forwardRoad, backwardRoad);
        }

        private (List<Vector3>, float[], List<Vector3>) EvenlyDistribute(GSDRoad road, float start, float end)
        {
            float totalDeltaProgress = end - start;
            
            int count = (int)(totalDeltaProgress / mapStud.resolution) + 2;
            
            float deltaProgress = totalDeltaProgress / (count - 1);

            float[] outProgress = new float[count];
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> forwards = new List<Vector3>();
            
            for (int i = 0; i < count - 1; i++)
            {
                float progress = start + deltaProgress * i;
                var (p, fw) = GetRoadValues(road, progress);
                positions.Add(p);
                forwards.Add(fw);
                outProgress[i] = progress;
            }
            var (pos, forward) = GetRoadValues(road, end);
            positions.Add(pos);
            forwards.Add(forward);
            outProgress[count - 1] = end;
            
            return (positions, outProgress, forwards);
        }

        private (Vector3, Vector3) GetRoadValues(GSDRoad road, float progress)
        {
            float maxProgress = road.GSDSpline.mNodes.Last().tDist;
            var vec = road.GSDSpline.GetSplineValue(Math.Min(0.9999999f, progress / maxProgress));
            var forward = (road.GSDSpline.GetSplineValue(Math.Min(0.9999999f, (progress + 0.1f) / maxProgress)) -
                           road.GSDSpline.GetSplineValue(Math.Max(0, (progress - 0.1f) / maxProgress))).normalized;
            return (vec, forward);
        }

        private Vector3 GetOffsetPoint(Vector3 pos, Vector3 forward, float offset)
        { 
            return Vector3.Cross(Vector3.up, forward.normalized).normalized * offset + pos;
        }
    }
#endif
}
