using RPG.Core;
using Unity.Entities;
using RPG.Mouvement;

namespace RPG.Control
{
    [GenerateAuthoringComponent]
    public struct PlayerControlled : IComponentData { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ConvertToEntitySystem))]
    public class PlayerControlledInitialisationSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities
            .WithAll<PlayerControlled>()
            .ForEach((Entity e) =>
            {
                EntityManager.AddComponent<MouseClick>(e);
            });
        }
    }
}
