using Unity.Entities;

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