using Unity.Entities;

namespace RPG.Core
{
    [GenerateAuthoringComponent]
    public struct MaxLifeTime : IComponentData
    {
        public float Value;
    }
}