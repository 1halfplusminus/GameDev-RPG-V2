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

    public partial class CollidWithPlayerSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        EntityQuery collidWithPlayerQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            collidWithPlayerQuery = EntityManager.CreateEntityQuery(typeof(CollidWithPlayer));
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem
            .CreateCommandBuffer();
            var commandBufferP = commandBuffer
            .AsParallelWriter();

            commandBuffer.RemoveComponentForEntityQuery<CollidWithPlayer>(collidWithPlayerQuery);

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
