using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public struct Health : IComponentData
{
    [Min(0.0f)]
    public float Value;
}