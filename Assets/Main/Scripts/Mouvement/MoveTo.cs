using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace RPG.Mouvement
{

    [GenerateAuthoringComponent]
    public struct MoveTo : IComponentData
    {
        public float3 Position;

        public float StoppingDistance;
    }
}

