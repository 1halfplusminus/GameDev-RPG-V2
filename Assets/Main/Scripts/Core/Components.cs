
using Unity.Entities;

namespace RPG.Core
{
    public struct Follow : IComponentData
    {
        public Entity Entity;
    }

    public struct LookAt : IComponentData
    {
        public Entity Entity;
    }

    public struct ActiveCamera : IComponentData
    {

    }
}
