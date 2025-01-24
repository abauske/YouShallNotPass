using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Map
{
    [Serializable]
    public class MappedIntersection
    {
        [SerializeReference] public List<MappedRoad> roads = new List<MappedRoad>();
        [SerializeReference] public List<MappedRoad> oncomingRoads = new List<MappedRoad>();
        [SerializeReference] public List<MappedRoad> outgoingRoads = new List<MappedRoad>();

        [SerializeField] public Vector3 pos;

        public MappedIntersection(Vector3 pos)
        {
            this.pos = pos;
        }

        public void AddOncoming(MappedRoad road)
        {
            oncomingRoads.Add(road);
            roads.Add(road);
            foreach (var o in outgoingRoads)
            {
                if (o.oppositeDirection == road)
                {
                    continue;
                }
                road.endIntersectionNextRoads.Add(o);
                o.startIntersectionNextRoads.Add(road);
            }
            road.endIntersection = this;
        }

        public void AddOutgoing(MappedRoad road)
        {
            outgoingRoads.Add(road);
            roads.Add(road);
            foreach (var o in oncomingRoads)
            {
                if (o.oppositeDirection == road)
                {
                    continue;
                }
                road.startIntersectionNextRoads.Add(o);
                o.endIntersectionNextRoads.Add(road);
            }
            road.startIntersection = this;
        }
    }
}