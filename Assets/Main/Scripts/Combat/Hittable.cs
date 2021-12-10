using Unity.Entities;

namespace RPG.Combat
{
    [GenerateAuthoringComponent]
    public struct Hittable : IComponentData
    {

    }

    public struct HitPoint : IComponentData
    {
        public Entity Entity;
    }
}
