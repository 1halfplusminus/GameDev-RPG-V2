using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

using ExtensionMethods;
using Unity.Physics;

namespace RPG.Core
{
    public struct WorldClick : IComponentData
    {
        public float3 WorldPosition;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateAfter(typeof(RaycastSystem))]
    public class ClickOnTerrainSystem : SystemBase
    {
        const float MAX_DISTANCE = 10000000f;
        EntityQuery queryClicks;
        EntityQuery queryTerrains;
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var navigables = GetComponentDataFromEntity<Navigable>(true);
            Entities
            .WithReadOnly(navigables)
            .ForEach((Entity e, int entityInQueryIndex, in DynamicBuffer<HittedByRaycast> rayHits) =>
            {
                foreach (var rayHit in rayHits)
                {
                    if (navigables.HasComponent(rayHit.Hitted))
                    {
                        commandBuffer.AddComponent(entityInQueryIndex, e, new WorldClick() { WorldPosition = rayHit.Hit.Position });
                        return;
                    }
                }
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }

    [UpdateAfter(typeof(ClickOnTerrainSystem))]
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class EndSimulationWorldClickSystem : SystemBase
    {
        EntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            // Destroy worldClick at end of simulation
            Entities.ForEach((Entity e, int entityInQueryIndex, ref MouseClick click) =>
            {
                commandBuffer.RemoveComponent<WorldClick>(entityInQueryIndex, e);
            }).ScheduleParallel();

            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }

}
