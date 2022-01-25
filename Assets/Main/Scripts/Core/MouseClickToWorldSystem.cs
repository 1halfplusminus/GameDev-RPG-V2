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
        public Entity Hitted;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    [UpdateAfter(typeof(RaycastSystem))]
    public class ClickOnTerrainSystem : SystemBase
    {

        EntityCommandBufferSystem commandBufferSystem;

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
                        cbp.AddComponent(entityInQueryIndex, e, new WorldClick() { WorldPosition = rayHit.Position, Frame = 0, Hitted = rayHit.Hitted });
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

}
