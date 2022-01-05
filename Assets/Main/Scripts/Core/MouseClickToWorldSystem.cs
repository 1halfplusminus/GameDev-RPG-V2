using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using ExtensionMethods;
using Unity.Physics;
using RPG.Mouvement;

namespace RPG.Core
{
    public struct WorldClick : IComponentData
    {
        public float3 WorldPosition;
        public int Frame;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateAfter(typeof(RaycastSystem))]
    public class ClickOnTerrainSystem : SystemBase
    {


        EntityCommandBufferSystem commandBufferSystem;
        EntityQuery mouseClickQuery;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        }
        protected override void OnUpdate()
        {
            var cb = commandBufferSystem.CreateCommandBuffer();
            var cbp = cb.AsParallelWriter();
            Entities
            .ForEach((Entity e, int entityInQueryIndex, in DynamicBuffer<HittedByRaycastEvent> rayHits) =>
            {
                foreach (var rayHit in rayHits)
                {
                    if (HasComponent<Navigable>(rayHit.Hitted))
                    {
                        cbp.AddComponent(entityInQueryIndex, e, new WorldClick() { WorldPosition = rayHit.Hit.Position, Frame = 0 });
                        return;
                    }

                }
            }).ScheduleParallel();
            Entities
            .ForEach((Entity e, int entityInQueryIndex, ref WorldClick worldClick, in MouseClick click) =>
            {
                if (worldClick.Frame > 3)
                {
                    cbp.RemoveComponent<WorldClick>(entityInQueryIndex, e);
                }
                worldClick.Frame += 1;
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(Dependency);
        }


    }

    // [UpdateAfter(typeof(ClickOnTerrainSystem))]
    // [UpdateInGroup(typeof(CoreSystemGroup))]
    // public class EndSimulationWorldClickSystem : SystemBase
    // {
    //     EntityCommandBufferSystem commandBufferSystem;
    //     protected override void OnCreate()
    //     {
    //         base.OnCreate();
    //         commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    //     }
    //     protected override void OnUpdate()
    //     {
    //         var commandBuffer = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
    //         // Destroy worldClick at end of simulation
    //         Entities.ForEach((Entity e, int entityInQueryIndex, ref MouseClick click) =>
    //         {
    //             commandBuffer.RemoveComponent<WorldClick>(entityInQueryIndex, e);
    //         }).ScheduleParallel();

    //         commandBufferSystem.AddJobHandleForProducer(Dependency);
    //     }
    // }

}
