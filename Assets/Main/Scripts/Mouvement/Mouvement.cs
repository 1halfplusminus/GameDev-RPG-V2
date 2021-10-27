
using Unity.Entities;
using Unity.Physics;

namespace RPG.Mouvement
{
    [GenerateAuthoringComponent]
    public struct Mouvement : IComponentData
    {
        public Velocity Velocity;
        public float Weight;
    }
}
