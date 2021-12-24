using System;
using Unity.Entities;

namespace RPG.Stats
{
    [GenerateAuthoringComponent]
    [Serializable]
    public struct ExperiencePoint : IComponentData
    {
        public float Value;
    }
}