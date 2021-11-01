
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace RPG.Control
{
    public struct AIControlled : IComponentData { }

    public struct ChasePlayer : IComponentData
    {
        public float ChaseDistance;
    }

    public struct GuardOriginalLocationTag : IComponentData { }
    public struct GuardLocation : IComponentData
    {
        public float3 Value;
    }

}