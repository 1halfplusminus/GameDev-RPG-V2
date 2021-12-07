using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


using RPG.Core;
namespace RPG.Mouvement
{
    public struct Warped : IComponentData
    {

    }
    public struct IsMoving : IComponentData { }

    [GenerateAuthoringComponent]
    public struct MoveTo : IComponentData
    {

        public float SpeedPercent;
        public bool Canceled;

        public bool Stopped;

        public float3 Position;

        public float StoppingDistance;

        public float Distance;

        public bool IsAtStoppingDistance { get => Distance <= StoppingDistance; }



        public MoveTo(float3 position) : this(position, 1f)
        {

        }
        public MoveTo(float3 position, float stoppingDistance)
        {
            Position = position;
            StoppingDistance = stoppingDistance;
            Distance = math.INFINITY;
            Canceled = false;
            Stopped = false;
            SpeedPercent = 1f;
        }

        public float CalculeSpeed(in Mouvement mouvement)
        {
            return SpeedPercent * mouvement.Speed;
        }
        public bool IsAtPosition(float3 position)
        {
            return IsAtStoppingDistance;
        }
    }
}

