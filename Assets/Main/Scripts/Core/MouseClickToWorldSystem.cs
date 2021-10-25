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
        EndSimulationEntityCommandBufferSystem commandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
                        UnityEngine.Debug.Log("Here");
                        commandBuffer.AddComponent(entityInQueryIndex, e, new WorldClick() { WorldPosition = rayHit.Hit.Position });
                        return;
                    }
                }
            }).ScheduleParallel();
            commandBufferSystem.AddJobHandleForProducer(this.Dependency);
            /* queryClicks = GetEntityQuery(new ComponentType[] {
            ComponentType.ReadOnly<MouseClick>()
        });
            queryTerrains = GetEntityQuery(new ComponentType[] {
            ComponentType.ReadOnly<TerrainCollider>()
        });
            var clicks = queryClicks.ToComponentDataArray<MouseClick>(Allocator.Temp);
            var terrains = queryTerrains.ToComponentArray<TerrainCollider>();
            var terrainEntities = queryTerrains.ToEntityArray(Allocator.Temp);
            foreach (var click in clicks)
            {
                if (click.CapturedThisFrame == true)
                {
                    for (int i = 0; i < terrains.Length; i++)
                    {
                        RaycastHit hit;
                        terrains[i].Raycast(click.Ray.ToEngineRay(), out hit, MAX_DISTANCE);
                        if (hit.collider)
                        {
                            var worldClick = new WorldClick { WorldPosition = hit.point };
                            if (EntityManager.HasComponent<WorldClick>(terrainEntities[i]))
                            {
                                EntityManager.SetComponentData(terrainEntities[i], worldClick);
                            }
                            else
                            {
                                EntityManager.AddComponentData(terrainEntities[i], worldClick);
                            }

                            Debug.Log("Clicked on " + hit.collider.name);
                        }
                    }
                }

            }
            clicks.Dispose();
            terrainEntities.Dispose(); */
        }
    }

}
