using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[GenerateAuthoringComponent]
public struct MoveTo : IComponentData {
    public float3 Position;

    public float StoppingDistance;
}

