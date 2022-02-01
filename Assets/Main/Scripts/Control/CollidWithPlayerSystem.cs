using Unity.Entities;
using UnityEngine;
using RPG.Core;

namespace RPG.Control
{
    public struct CollidWithPlayer : IComponentData
    {
        public Entity Entity;
        public EventOverlapState State;
    }
    [UpdateInGroup(typeof(ControlSystemGroup))]

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
            var commandBufferP = entityCommandBufferSystem
            .CreateCommandBuffer()
            .AsParallelWriter();
            Entities.ForEach((int entityInQueryIndex, Entity e, DynamicBuffer<StatefulTriggerEvent> triggerEvents) =>
            {
                foreach (var triggerEvent in triggerEvents)
                {
                    var otherEntity = triggerEvent.GetOtherEntity(e);
                    if (HasComponent<PlayerControlled>(otherEntity) && HasComponent<DisabledControl>(otherEntity) == false)
                    {
                        // Debug.Log($" {e.Index} Collid with player {otherEntity.Index}");
                        commandBufferP.AddComponent(entityInQueryIndex, e, new CollidWithPlayer { Entity = otherEntity, State = triggerEvent.State });
                        break;
                    }
                }
            }).ScheduleParallel();

            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}
