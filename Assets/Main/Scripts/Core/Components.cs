
using Unity.Entities;
namespace RPG.Core
{
    public struct Playing : IComponentData
    { }
    public struct Play : IComponentData
    { }
    public struct Played : IComponentData
    { }
    public struct Target : IComponentData
    {
        public Entity Entity;
    }
    public struct Follow : IComponentData
    {
        public Entity Entity;

    }

    public struct LookAt : IComponentData
    {
        public Entity Entity;

    }
}
