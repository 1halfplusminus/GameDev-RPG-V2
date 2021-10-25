
using Unity.Entities;
using Unity.Physics;

namespace RPG.Mouvement
{
    public struct Mouvement : IComponentData
    {
        public Velocity Velocity;
    }
}
