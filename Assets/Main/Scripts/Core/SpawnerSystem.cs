using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

namespace RPG.Core
{
    public struct HasHybridComponent : IComponentData
    {

    }
    public struct Spawn : IComponentData
    {
        public Entity Prefab;
    }

    public struct Spawned : IComponentData
    {

    }
    public struct HasSpawn : IComponentData
    {
        public Entity Entity;
    }
    [UpdateInGroup(typeof(CoreSystemGroup))]
    public class SpawnSystem : SystemBase
    {
        EntityCommandBufferSystem entityCommandBufferSystem;
        protected override void OnCreate()
        {
            base.OnCreate();
            entityCommandBufferSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            var commandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            var commandBufferP = commandBuffer.AsParallelWriter();
            Entities.WithNone<HasHybridComponent>().ForEach((int entityInQueryIndex, Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
               {
                   var instance = commandBufferP.Instantiate(entityInQueryIndex, toSpawn.Prefab);
                   commandBufferP.AddComponent(entityInQueryIndex, instance, new Translation { Value = localToWorld.Position });
                   commandBufferP.AddComponent(entityInQueryIndex, instance, new Rotation { Value = localToWorld.Rotation });
                   commandBufferP.AddComponent<Spawned>(entityInQueryIndex, instance);

                   commandBufferP.AddComponent(entityInQueryIndex, e, new HasSpawn { Entity = instance });
                   commandBufferP.RemoveComponent<Spawn>(entityInQueryIndex, e);
               }
            ).ScheduleParallel();
            var em = EntityManager;
            Entities.WithAny<HasHybridComponent>().WithStructuralChanges().ForEach((Entity e, in Spawn toSpawn, in LocalToWorld localToWorld) =>
                {
                    var instance = em.Instantiate(toSpawn.Prefab);
                    commandBuffer.AddComponent<Translation>(instance, new Translation { Value = localToWorld.Position });
                    commandBuffer.AddComponent<Rotation>(instance, new Rotation { Value = localToWorld.Rotation });
                    commandBuffer.AddComponent<Spawned>(instance);
                    commandBuffer.RemoveComponent<Spawn>(e);
                }
            ).Run();

            Entities.WithAny<Spawned>().ForEach((int entityInQueryIndex, Entity e) =>
                {
                    commandBufferP.RemoveComponent<Spawned>(entityInQueryIndex, e);
                }
            ).ScheduleParallel();
            entityCommandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }


}
