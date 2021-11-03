using Unity.Entities;

namespace RPG.Animation
{
    [GenerateAuthoringComponent]
    public struct GuardAnimation : IComponentData
    {
        public float NervouslyLookingAround;
    }
}