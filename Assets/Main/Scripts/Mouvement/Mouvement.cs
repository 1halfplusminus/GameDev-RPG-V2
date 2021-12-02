
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace RPG.Mouvement
{
    [GenerateAuthoringComponent]
    public struct Mouvement : IComponentData
    {
        public Velocity Velocity;
        public float Weight;

        public float Speed;
    }
    [Serializable]
    public struct WarpTo : IComponentData
    {
        public float3 Destination;
    }
}
