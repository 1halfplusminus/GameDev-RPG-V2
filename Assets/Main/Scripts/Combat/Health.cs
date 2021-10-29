using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct Health : IComponentData
{
    [Min(0.0f)]
    public float Value;
}