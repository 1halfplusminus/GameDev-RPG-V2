using Unity.Entities;
using RPG.Core;
using RPG.Control;

namespace RPG.Gameplay
{

    [UpdateInGroup(typeof(GameplaySystemGroup))]

    public class CollidWithPlayerSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBufferP = entityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var players = GetComponentDataFromEntity<PlayerControlled>(true);
            Entities.ForEach((int entityInQueryIndex, Entity e, DynamicBuffer<StatefulTriggerEvent> triggerEvents) =>
            {
                foreach (var triggerEvent in triggerEvents)
                {
                    var otherEntity = triggerEvent.GetOtherEntity(e);
                    if (players.HasComponent(otherEntity))
                    {
                        commandBufferP.AddComponent(entityInQueryIndex, e, new CollidWithPlayer { Entity = otherEntity });
                        break;
                    }
                }
            }).WithReadOnly(players).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
