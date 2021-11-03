using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


using RPG.Core;
namespace RPG.Mouvement
{

    [GenerateAuthoringComponent]
    public struct MoveTo : IComponentData
    {

        public bool Canceled;

        public bool Stopped;

        public float3 Position;

        public float StoppingDistance;

        public float Distance;

        public bool IsAtStoppingDistance { get => Distance <= StoppingDistance; }

        public float SpeedPercent;

        public float MaxSpeed;
        public MoveTo(float3 position) : this(position, 1f, 0.0f)
        {

        }
        public MoveTo(float3 position, float stoppingDistance, float maxSpeed)
        {
            Position = position;
            StoppingDistance = stoppingDistance;
            Distance = math.INFINITY;
            Canceled = false;
            Stopped = false;
            SpeedPercent = 1f;
            MaxSpeed = maxSpeed;
        }

        public bool IsAtPosition(float3 position)
        {
            return IsAtStoppingDistance;
        }
    }
}

